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
            // BLOOD SACRIFICES WERE MADE FOR THIS
            // NEVER TOUCH IT AGAIN
            // THIS MAGIC HAS BEEN LOST TO TIME
            if (gameObject.GetComponent<MeshFilter>()==null) meshFilter=gameObject.AddComponent<MeshFilter>();
            if (gameObject.GetComponent<MeshRenderer>()==null) meshRenderer=gameObject.AddComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            List<Vector3> surfaceLattice = new List<Vector3>();
            for (int indexY=-2; indexY<resolution+3; indexY++) {
                for (int indexX=-2; indexX<resolution+3; indexX++) {
                    RaycastHit hit;
                    float chunkX=(chunkID%chunks)*size/chunks;
                    float chunkY=Mathf.Floor(chunkID/chunks)*size/chunks;
                    Vector3 point = new Vector3(indexX * (size/chunks)/resolution + chunkX, 0, indexY * (size/chunks)/resolution + chunkY);
                    if (Physics.Raycast(transform.TransformPoint(point)+Vector3.up*encapsulatedBounds.size.y, -Vector3.up, out hit, encapsulatedBounds.size.y, TerrainBrushOverseer.instance.meshBrushTargetLayers)) {
                        float y = transform.InverseTransformPoint(hit.point).y;
                        surfaceLattice.Add(new Vector3(point.x, y, point.z));
                    } else {
                        surfaceLattice.Add(point-Vector3.up*encapsulatedBounds.extents.y);
                    }
                }
            }
            List<Vector3> surfaceLatticeSmooth = new List<Vector3>(surfaceLattice);
            for (int indexY=0; indexY<resolution+3; indexY++) {
                for (int indexX=0; indexX<resolution+3; indexX++) {
                    Vector3 smoothedPoint=surfaceLattice[(indexX+0)+(indexY+1)*(resolution+5)];
                    smoothedPoint+=surfaceLattice[(indexX+2)+(indexY+1)*(resolution+5)];
                    smoothedPoint+=surfaceLattice[(indexX+1)+(indexY+0)*(resolution+5)];
                    smoothedPoint+=surfaceLattice[(indexX+1)+(indexY+2)*(resolution+5)];
                    smoothedPoint+=surfaceLattice[(indexX+0)+(indexY+0)*(resolution+5)];
                    smoothedPoint+=surfaceLattice[(indexX+2)+(indexY+0)*(resolution+5)];
                    smoothedPoint+=surfaceLattice[(indexX+0)+(indexY+2)*(resolution+5)];
                    smoothedPoint+=surfaceLattice[(indexX+2)+(indexY+2)*(resolution+5)];
                    smoothedPoint/=8f;
                    surfaceLatticeSmooth[(indexX+1)+(indexY+1)*(resolution+5)]=smoothedPoint;
                }
            }
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int indexY=0; indexY<resolution+1; indexY++) {
                for (int indexX=0; indexX<resolution+1; indexX++) {
                    int myIndex=(indexX+2)+(indexY+2)*(resolution+5);
                    vertices.Add(surfaceLatticeSmooth[myIndex]);
                    Vector3 myNormal=Vector3.zero;
                    myNormal+=Vector3.Cross((surfaceLatticeSmooth[myIndex-(resolution+5)]-surfaceLatticeSmooth[myIndex]).normalized, (surfaceLatticeSmooth[myIndex-1]-surfaceLatticeSmooth[myIndex]).normalized).normalized;
                    myNormal+=Vector3.Cross((surfaceLatticeSmooth[myIndex-1]-surfaceLatticeSmooth[myIndex]).normalized, (surfaceLatticeSmooth[myIndex+(resolution+5)]-surfaceLatticeSmooth[myIndex]).normalized).normalized;
                    myNormal+=Vector3.Cross((surfaceLatticeSmooth[myIndex+(resolution+5)]-surfaceLatticeSmooth[myIndex]).normalized, (surfaceLatticeSmooth[myIndex+1]-surfaceLatticeSmooth[myIndex]).normalized).normalized;
                    myNormal+=Vector3.Cross((surfaceLatticeSmooth[myIndex+1]-surfaceLatticeSmooth[myIndex]).normalized, (surfaceLatticeSmooth[myIndex-(resolution+5)]-surfaceLatticeSmooth[myIndex]).normalized).normalized;
                    normals.Add(myNormal.normalized);
                }
            }
            List<Vector2> uv = new List<Vector2>();
            List<Vector2> uv2 = new List<Vector2>();
            for (int indexY=0; indexY<resolution+1; indexY++) {
                for (int indexX=0; indexX<resolution+1; indexX++) {
                    Vector3 texPoint = TerrainBrushOverseer.instance.volume.worldToTexture.MultiplyPoint(transform.TransformPoint(vertices[indexY*(resolution+1)+indexX]));
                    uv.Add(new Vector2(texPoint.x, texPoint.y));
                    uv2.Add(new Vector2(indexX/resolution, indexY/resolution));
                }
            }
            List<int> triangles = new List<int>();
            float lowerBound = 0f;
            for (int indexY=0; indexY<resolution; indexY++) {
                for (int indexX=0;indexX<resolution;indexX++) {
                    if (Vector3.Distance(vertices[indexX+indexY*(resolution+1)], vertices[indexX+1+(indexY+1)*(resolution+1)])>Vector3.Distance(vertices[indexX+1+indexY*(resolution+1)], vertices[indexX+(indexY+1)*(resolution+1)])) {
                        if (vertices[indexX+indexY*(resolution+1)].y>lowerBound && vertices[indexX+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+indexY*(resolution+1)].y>lowerBound) {
                            triangles.Add(indexX+indexY*(resolution+1));
                            triangles.Add(indexX+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+indexY*(resolution+1));
                        }
                        if (vertices[indexX+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+indexY*(resolution+1)].y>lowerBound) {
                            triangles.Add(indexX+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+indexY*(resolution+1));
                        }
                    } else {
                        if (vertices[indexX+indexY*(resolution+1)].y>lowerBound && vertices[indexX+1+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+indexY*(resolution+1)].y>lowerBound) {
                            triangles.Add(indexX+indexY*(resolution+1));
                            triangles.Add(indexX+1+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+indexY*(resolution+1));
                        }
                        if (vertices[indexX+indexY*(resolution+1)].y>lowerBound && vertices[indexX+(indexY+1)*(resolution+1)].y>lowerBound && vertices[indexX+1+(indexY+1)*(resolution+1)].y>lowerBound) {
                            triangles.Add(indexX+indexY*(resolution+1));
                            triangles.Add(indexX+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+(indexY+1)*(resolution+1));
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
                    uv.RemoveAt(cullIndex);
                    uv2.RemoveAt(cullIndex);
                    vertCull.RemoveAt(cullIndex);
                } else {
                    cullIndex++;
                }
            }
            // ORGANIZE TRI LIST FOR CULLED VERTS
            List<int> vertLookup = new List<int>();
            for (int i=0;i<(resolution+1)*(resolution+1);i++) vertLookup.Add(0);
            for (int i=0;i<vertCull.Count;i++) vertLookup[vertCull[i]]=i;
            for (int i=0;i<triangles.Count;i++) {
                triangles[i]=vertLookup[triangles[i]];
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();
            mesh.uv2 = uv2.ToArray();
            //mesh.RecalculateNormals();
            meshFilter.mesh=mesh;
        }

#endif
    }

}
