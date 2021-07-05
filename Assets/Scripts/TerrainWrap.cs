using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainBrush {

    public class TerrainWrap : MonoBehaviour {

        [SerializeField] private float size=100f;
        [SerializeField] private int resolution=128;
        [SerializeField] private Material material;
        [SerializeField] private TerrainBrushVolume volume;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

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
                encapsulatedBounds = encapsulatedBounds.EncapsulateTransformedBounds(c.bounds);
            }
            Debug.DrawLine(encapsulatedBounds.min, encapsulatedBounds.max, Color.blue, 1f);
            transform.position=encapsulatedBounds.min;
            transform.rotation=Quaternion.identity;
            size=Mathf.Max(encapsulatedBounds.size.x, encapsulatedBounds.size.z);

            if (meshFilter==null) gameObject.AddComponent<MeshFilter>();
            if (meshRenderer==null) gameObject.AddComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            for (int indexY=0; indexY<resolution; indexY++) {
                for (int indexX=0; indexX<resolution; indexX++) {
                    RaycastHit hit;
                    Vector3 point = new Vector3(indexX * size/resolution, 0f, indexY * size/resolution);
                    if (Physics.Raycast(transform.TransformPoint(point+Vector3.up*100f), -Vector3.up, out hit, 200f, LayerMask.GetMask("TerrainBrushes"))) {
                        float y = transform.InverseTransformPoint(hit.point).y;
                        //y = Mathf.Max(0f, y);
                        vertices.Add(new Vector3(point.x, y, point.z));
                    } else {
                        vertices.Add(point-Vector3.up*100f);
                    }
                }
            }
            List<int> tris = new List<int>();
            for (int indexY=0; indexY<resolution-1; indexY++) {
                for (int indexX=0;indexX<resolution-1;indexX++) {
                    if (Vector3.Distance(vertices[indexX+indexY*resolution], vertices[indexX+1+(indexY+1)*resolution])>Vector3.Distance(vertices[indexX+1+indexY*resolution], vertices[indexX+(indexY+1)*resolution])) {
                        if (vertices[indexX+indexY*resolution].y>-100f && vertices[indexX+(indexY+1)*resolution].y>-100f && vertices[indexX+1+indexY*resolution].y>-100f) {
                            tris.Add(indexX+indexY*resolution);
                            tris.Add(indexX+(indexY+1)*resolution);
                            tris.Add(indexX+1+indexY*resolution);
                        }
                        if (vertices[indexX+(indexY+1)*resolution].y>-100f && vertices[indexX+1+(indexY+1)*resolution].y>-100f && vertices[indexX+1+indexY*resolution].y>-100f) {
                            tris.Add(indexX+(indexY+1)*resolution);
                            tris.Add(indexX+1+(indexY+1)*resolution);
                            tris.Add(indexX+1+indexY*resolution);
                        }
                    } else {
                        if (vertices[indexX+indexY*resolution].y>-100f && vertices[indexX+1+(indexY+1)*resolution].y>-100f && vertices[indexX+1+indexY*resolution].y>-100f) {
                            tris.Add(indexX+indexY*resolution);
                            tris.Add(indexX+1+(indexY+1)*resolution);
                            tris.Add(indexX+1+indexY*resolution);
                        }
                        if (vertices[indexX+indexY*resolution].y>-100f && vertices[indexX+(indexY+1)*resolution].y>-100f && vertices[indexX+1+(indexY+1)*resolution].y>-100f) {
                            tris.Add(indexX+indexY*resolution);
                            tris.Add(indexX+(indexY+1)*resolution);
                            tris.Add(indexX+1+(indexY+1)*resolution);
                        }
                    }
                }
            }
            List<Vector3> normals = new List<Vector3>();
            for (int indexY=0; indexY<resolution; indexY++) {
                for (int indexX=0; indexX<resolution; indexX++) {
                    normals.Add(Vector3.up);
                }
            }
            List<Vector2> uvs = new List<Vector2>();
            for (int indexY=0; indexY<resolution; indexY++) {
                for (int indexX=0; indexX<resolution; indexX++) {
                    Vector3 texPoint = volume.worldToTexture.MultiplyPoint(transform.TransformPoint(vertices[indexY*resolution+indexX]));
                    uvs.Add(new Vector2(texPoint.x, texPoint.y));
                }
            }
            // CULL UNUSED VERTS
            List<int> vertCull = new List<int>();
            for (int i=0;i<vertices.Count;i++) vertCull.Add(i);
            int cullIndex=0;
            while (cullIndex<vertices.Count) {
                if (vertices[cullIndex].y<=-100f) {
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
            for (int i=0;i<resolution*resolution;i++) vertLookup.Add(0);
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
            meshRenderer.sharedMaterial=material;
        }

    }

}
