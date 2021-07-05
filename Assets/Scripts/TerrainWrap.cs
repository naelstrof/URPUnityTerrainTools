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
            if (meshFilter==null) gameObject.AddComponent<MeshFilter>();
            if (meshRenderer==null) gameObject.AddComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            for (int indexY=0; indexY<100; indexY++) {
                for (int indexX=0; indexX<100; indexX++) {
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
            for (int indexY=0; indexY<99; indexY++) {
                for (int indexX=0;indexX<99;indexX++) {
                    if (Vector3.Distance(vertices[indexX+indexY*100], vertices[indexX+1+(indexY+1)*100])>Vector3.Distance(vertices[indexX+1+indexY*100], vertices[indexX+(indexY+1)*100])) {
                        if (vertices[indexX+indexY*100].y>-100f && vertices[indexX+(indexY+1)*100].y>-100f && vertices[indexX+1+indexY*100].y>-100f) {
                            tris.Add(indexX+indexY*100);
                            tris.Add(indexX+(indexY+1)*100);
                            tris.Add(indexX+1+indexY*100);
                        }
                        if (vertices[indexX+(indexY+1)*100].y>-100f && vertices[indexX+1+(indexY+1)*100].y>-100f && vertices[indexX+1+indexY*100].y>-100f) {
                            tris.Add(indexX+(indexY+1)*100);
                            tris.Add(indexX+1+(indexY+1)*100);
                            tris.Add(indexX+1+indexY*100);
                        }
                    } else {
                        if (vertices[indexX+indexY*100].y>-100f && vertices[indexX+1+(indexY+1)*100].y>-100f && vertices[indexX+1+indexY*100].y>-100f) {
                            tris.Add(indexX+indexY*100);
                            tris.Add(indexX+1+(indexY+1)*100);
                            tris.Add(indexX+1+indexY*100);
                        }
                        if (vertices[indexX+indexY*100].y>-100f && vertices[indexX+(indexY+1)*100].y>-100f && vertices[indexX+1+(indexY+1)*100].y>-100f) {
                            tris.Add(indexX+indexY*100);
                            tris.Add(indexX+(indexY+1)*100);
                            tris.Add(indexX+1+(indexY+1)*100);
                        }
                    }
                }
            }
            List<Vector3> normals = new List<Vector3>();
            for (int indexY=0; indexY<100; indexY++) {
                for (int indexX=0; indexX<100; indexX++) {
                    normals.Add(Vector3.up);
                }
            }
            List<Vector2> uvs = new List<Vector2>();
            for (int indexY=0; indexY<100; indexY++) {
                for (int indexX=0; indexX<100; indexX++) {
                    Vector3 texPoint = volume.worldToTexture.MultiplyPoint(transform.TransformPoint(vertices[indexY*100+indexX]));
                    Debug.Log(texPoint);
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
