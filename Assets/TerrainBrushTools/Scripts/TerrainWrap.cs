using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TerrainBrush {

    [ExecuteAlways]
    public class TerrainWrap : MonoBehaviour {

        [SerializeField] private float size=40f;
        [SerializeField] private int resolution=128;
        [SerializeField] private int chunks=4;
        //[HideInInspector]
        public int chunkID=0;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private int generateTimer;
        [HideInInspector]
        public bool generated=false;
        public void SetChunkID(int value) { chunkID=value; generateTimer=value; generated=false; }

#if UNITY_EDITOR
		void OnEnable() {
			SceneView.duringSceneGui -= this.OnSceneGUI;
			SceneView.duringSceneGui += this.OnSceneGUI;
			UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
		}

		void OnDisable() {
			SceneView.duringSceneGui -= this.OnSceneGUI;
		}

		void OnSceneGUI(SceneView sceneView) {
            if (!generated) {
                generateTimer-=1;
                if (generateTimer<=0) {
                    Generate();
                    generated=true;
                }
                SceneView.RepaintAll();
            }
		}

        [ContextMenu("Generate")]
        public void Generate() {
            List<Collider> colliders = new List<Collider>(Object.FindObjectsOfType<Collider>());
            if (colliders.Count == 0) {
                Debug.Log("No colliders found.");
                return;
            }
            // Generate the volume that the textures exist on.
            Bounds encapsulatedBounds = new Bounds(colliders[0].bounds.center, colliders[0].bounds.size);
            foreach(Collider c in colliders) {
                if (((1<<c.gameObject.layer)&TerrainBrushOverseer.instance.meshBrushTargetLayers) != 0) {
                    encapsulatedBounds.EncapsulateTransformedBounds(c.bounds);
                }
            }
            transform.position=encapsulatedBounds.min;
            transform.rotation=Quaternion.identity;
            size=Mathf.Max(encapsulatedBounds.size.x, encapsulatedBounds.size.z);

            if (gameObject.GetComponent<MeshFilter>()==null) meshFilter=gameObject.AddComponent<MeshFilter>();
            if (gameObject.GetComponent<MeshRenderer>()==null) meshRenderer=gameObject.AddComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int indexY=0; indexY<resolution+1; indexY++) {
                for (int indexX=0; indexX<resolution+1; indexX++) {
                    RaycastHit hit;
                    float chunkX=(chunkID%chunks)*size/chunks;
                    float chunkY=Mathf.Floor(chunkID/chunks)*size/chunks;
                    Vector3 point = new Vector3(indexX * (size/chunks)/resolution + chunkX, 0, indexY * (size/chunks)/resolution + chunkY);
                    if (Physics.Raycast(transform.TransformPoint(point)+Vector3.up*encapsulatedBounds.size.y, -Vector3.up, out hit, encapsulatedBounds.size.y, TerrainBrushOverseer.instance.meshBrushTargetLayers)) {
                        //y = Mathf.Max(0f, y);
                        float y = transform.InverseTransformPoint(hit.point).y;
                        vertices.Add(new Vector3(point.x, y, point.z));
                        normals.Add(hit.normal);
                    } else {
                        vertices.Add(point-Vector3.up*encapsulatedBounds.extents.y);
                        normals.Add(Vector3.up);
                    }
                }
            }
            List<Vector2> uvs = new List<Vector2>();
            for (int indexY=0; indexY<resolution+1; indexY++) {
                for (int indexX=0; indexX<resolution+1; indexX++) {
                    Vector3 texPoint = TerrainBrushOverseer.instance.volume.worldToTexture.MultiplyPoint(transform.TransformPoint(vertices[indexY*(resolution+1)+indexX]));
                    uvs.Add(new Vector2(texPoint.x, texPoint.y));
                }
            }
            List<int> tris = new List<int>();
            float lowerBound = 0f;
            for (int indexY=0; indexY<resolution; indexY++) {
                for (int indexX=0;indexX<resolution;indexX++) {
                    if (Vector3.Distance(vertices[indexX+indexY*(resolution+1)], vertices[indexX+1+(indexY+1)*(resolution+1)])>Vector3.Distance(vertices[indexX+1+indexY*(resolution+1)], vertices[indexX+(indexY+1)*(resolution+1)])) {
                        if (vertices[indexX+indexY*(resolution+1)].y>lowerBound && vertices[indexX+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+indexY*(resolution+1)].y>lowerBound) {
                            tris.Add(indexX+indexY*(resolution+1));
                            tris.Add(indexX+(indexY+1)*(resolution+1));
                            tris.Add(indexX+1+indexY*(resolution+1));
                        }
                        if (vertices[indexX+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+indexY*(resolution+1)].y>lowerBound) {
                            tris.Add(indexX+(indexY+1)*(resolution+1));
                            tris.Add(indexX+1+(indexY+1)*(resolution+1));
                            tris.Add(indexX+1+indexY*(resolution+1));
                        }
                    } else {
                        if (vertices[indexX+indexY*(resolution+1)].y>lowerBound && vertices[indexX+1+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+indexY*(resolution+1)].y>lowerBound) {
                            tris.Add(indexX+indexY*(resolution+1));
                            tris.Add(indexX+1+(indexY+1)*(resolution+1));
                            tris.Add(indexX+1+indexY*(resolution+1));
                        }
                        if (vertices[indexX+indexY*(resolution+1)].y>lowerBound && vertices[indexX+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+(indexY+1)*(resolution+1)].y>lowerBound) {
                            tris.Add(indexX+indexY*(resolution+1));
                            tris.Add(indexX+(indexY+1)*(resolution+1));
                            tris.Add(indexX+1+(indexY+1)*(resolution+1));
                        }
                    }
                }
            }
            // CULL UNUSED VERTS
            List<int> vertCull = new List<int>();
            for (int i=0;i<vertices.Count;i++) vertCull.Add(i);
            int cullIndex=0;
            while (cullIndex<vertices.Count) {
                if (vertices[cullIndex].y<=lowerBound) {
                    vertices.RemoveAt(cullIndex);
                    normals.RemoveAt(cullIndex);
                    uvs.RemoveAt(cullIndex);
                    vertCull.RemoveAt(cullIndex);
                } else {
                    cullIndex++;
                }
            }
            // ORGANIZE TRI LIST FOR CULLED VERTS
            List<int> vertLookup = new List<int>();
            for (int i=0;i<(resolution+1)*(resolution+1);i++) vertLookup.Add(0);
            for (int i=0;i<vertCull.Count;i++) vertLookup[vertCull[i]]=i;
            for (int i=0;i<tris.Count;i++) {
                tris[i]=vertLookup[tris[i]];
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            meshFilter.mesh=mesh;
        }

#endif
    }

}
