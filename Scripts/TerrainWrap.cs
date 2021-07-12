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
        [HideInInspector]
        public int chunkID=0;
        private float smooth=1f;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilterFoliage;
        private MeshRenderer meshRendererFoliage;
        private int generateTimer;
        private Texture2D dataTexture;
        [HideInInspector]
        public bool generated=false;
        public void SetChunkID(int value, int chunks, int resolution, float smooth) { chunkID=value; this.chunks = chunks; this.resolution = resolution; this.smooth=smooth; generateTimer=value; generated=false; }

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
            mesh.name="GeneratedSceneMesh";
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
                        surfaceLattice.Add(new Vector3(point.x, encapsulatedBounds.min.y, point.z));
                    }
                }
            }
            List<Vector3> surfaceLatticeSmooth = new List<Vector3>(surfaceLattice);
            for (int indexY=0; indexY<resolution+3; indexY++) {
                for (int indexX=0; indexX<resolution+3; indexX++) {
                    Vector3 smoothPoint;
                    Vector3 smoothedPoint=surfaceLattice[(indexX+1)+(indexY+1)*(resolution+5)];
                    int smoothPoints=1;
                    if (smoothedPoint.y>encapsulatedBounds.min.y) {
                        smoothPoint=surfaceLattice[(indexX+0)+(indexY+0)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothPoint=surfaceLattice[(indexX+1)+(indexY+0)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothPoint=surfaceLattice[(indexX+2)+(indexY+0)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothPoint=surfaceLattice[(indexX+0)+(indexY+1)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothPoint=surfaceLattice[(indexX+2)+(indexY+1)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothPoint=surfaceLattice[(indexX+0)+(indexY+2)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothPoint=surfaceLattice[(indexX+1)+(indexY+2)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothPoint=surfaceLattice[(indexX+2)+(indexY+2)*(resolution+5)];
                        if (smoothPoint.y>encapsulatedBounds.min.y) {
                            smoothedPoint+=smoothPoint;
                            smoothPoints++;
                        }
                        smoothedPoint/=smoothPoints;
                    }
                    surfaceLatticeSmooth[(indexX+1)+(indexY+1)*(resolution+5)]=Vector3.Lerp(surfaceLattice[(indexX+1)+(indexY+1)*(resolution+5)], smoothedPoint, smooth);
                }
            }
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int indexY=0; indexY<resolution+1; indexY++) {
                for (int indexX=0; indexX<resolution+1; indexX++) {
                    int myIndex=(indexX+2)+(indexY+2)*(resolution+5);
                    vertices.Add(surfaceLatticeSmooth[myIndex]);
                    Vector3 myNormal=Vector3.zero;
                    if (surfaceLatticeSmooth[myIndex-(resolution+5)].y>encapsulatedBounds.min.y && surfaceLatticeSmooth[myIndex-1].y>encapsulatedBounds.min.y)
                        myNormal+=Vector3.Cross((surfaceLatticeSmooth[myIndex-(resolution+5)]-surfaceLatticeSmooth[myIndex]).normalized, (surfaceLatticeSmooth[myIndex-1]-surfaceLatticeSmooth[myIndex]).normalized).normalized;
                    if (surfaceLatticeSmooth[myIndex-1].y>encapsulatedBounds.min.y && surfaceLatticeSmooth[myIndex+(resolution+5)].y>encapsulatedBounds.min.y)
                        myNormal+=Vector3.Cross((surfaceLatticeSmooth[myIndex-1]-surfaceLatticeSmooth[myIndex]).normalized, (surfaceLatticeSmooth[myIndex+(resolution+5)]-surfaceLatticeSmooth[myIndex]).normalized).normalized;
                    if (surfaceLatticeSmooth[myIndex+(resolution+5)].y>encapsulatedBounds.min.y && surfaceLatticeSmooth[myIndex+1].y>encapsulatedBounds.min.y)
                        myNormal+=Vector3.Cross((surfaceLatticeSmooth[myIndex+(resolution+5)]-surfaceLatticeSmooth[myIndex]).normalized, (surfaceLatticeSmooth[myIndex+1]-surfaceLatticeSmooth[myIndex]).normalized).normalized;
                    if (surfaceLatticeSmooth[myIndex+1].y>encapsulatedBounds.min.y && surfaceLatticeSmooth[myIndex-(resolution+5)].y>encapsulatedBounds.min.y)
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
                    uv2.Add(new Vector2((float)indexX/(float)resolution, (float)indexY/(float)resolution));
                }
            }
            List<int> triangles = new List<int>();
            for (int indexY=0; indexY<resolution; indexY++) {
                for (int indexX=0;indexX<resolution;indexX++) {
                    if (Vector3.Distance(vertices[indexX+indexY*(resolution+1)], vertices[indexX+1+(indexY+1)*(resolution+1)])>Vector3.Distance(vertices[indexX+1+indexY*(resolution+1)], vertices[indexX+(indexY+1)*(resolution+1)])) {
                        if (vertices[indexX+indexY*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+(indexY+1)*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+1+indexY*(resolution+1)].y>encapsulatedBounds.min.y) {
                            triangles.Add(indexX+indexY*(resolution+1));
                            triangles.Add(indexX+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+indexY*(resolution+1));
                        }
                        if (vertices[indexX+(indexY+1)*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+1+(indexY+1)*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+1+indexY*(resolution+1)].y>encapsulatedBounds.min.y) {
                            triangles.Add(indexX+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+indexY*(resolution+1));
                        }
                    } else {
                        if (vertices[indexX+indexY*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+1+(indexY+1)*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+1+indexY*(resolution+1)].y>encapsulatedBounds.min.y) {
                            triangles.Add(indexX+indexY*(resolution+1));
                            triangles.Add(indexX+1+(indexY+1)*(resolution+1));
                            triangles.Add(indexX+1+indexY*(resolution+1));
                        }
                        if (vertices[indexX+indexY*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+(indexY+1)*(resolution+1)].y>encapsulatedBounds.min.y && vertices[indexX+1+(indexY+1)*(resolution+1)].y>encapsulatedBounds.min.y) {
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
                if (vertices[cullIndex].y<=encapsulatedBounds.min.y) {
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
            // MOVE ORIGIN TO CENTER
            float averageY = 0f;
            if (vertices.Count>0) {
                for (int i=0;i<vertices.Count;i++) averageY+=vertices[i].y+transform.position.y;
                averageY/=vertices.Count;
            }
            Vector3 offset=new Vector3((chunkID%chunks+0.5f)*size/chunks + encapsulatedBounds.min.x, averageY, (Mathf.Floor(chunkID/chunks)+0.5f)*size/chunks + encapsulatedBounds.min.z)-transform.position;
            for (int i=0;i<vertices.Count;i++) vertices[i]-=offset;
            transform.position+=offset;

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();
            mesh.uv2 = uv2.ToArray();
            //mesh.RecalculateNormals();
            meshFilter.mesh=mesh;
            mesh.RecalculateBounds();

            MeshCollider meshCollider = GetComponent<MeshCollider>();
            if (meshCollider==null) meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh=mesh;
            //BuildFoliageMesh(vertices, normals, triangles, uv);
        }

        public void GenerateFoliage() {
            BuildFoliageMesh(meshFilter.sharedMesh.vertices, meshFilter.sharedMesh.normals, meshFilter.sharedMesh.triangles, meshFilter.sharedMesh.uv);
        }

        private void BuildFoliageMesh(Vector3[] verticesTerrain, Vector3[] normalsTerrain, int[] trianglesTerrain, Vector2[] uvTerrain) {
            dataTexture = new Texture2D(TerrainBrushOverseer.instance.volume.texture.width, TerrainBrushOverseer.instance.volume.texture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = TerrainBrushOverseer.instance.volume.texture;
            dataTexture.ReadPixels(new Rect(0, 0, TerrainBrushOverseer.instance.volume.texture.width, TerrainBrushOverseer.instance.volume.texture.height), 0, 0);
            dataTexture.Apply();
            GameObject foliage;
            Transform foliageT = transform.Find("Foliage");
            if (foliageT!=null) {
                foliage=foliageT.gameObject;
            } else {
                foliage=new GameObject("Foliage");
                foliage.transform.parent=transform;
            }
            foliage.transform.localPosition=Vector3.zero;
            foliage.transform.localRotation=Quaternion.identity;
            meshFilterFoliage=foliage.GetComponent<MeshFilter>();
            if (meshFilterFoliage==null) meshFilterFoliage=foliage.AddComponent<MeshFilter>();
            meshRendererFoliage=foliage.GetComponent<MeshRenderer>();
            if (meshRendererFoliage==null) meshRendererFoliage=foliage.AddComponent<MeshRenderer>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int i=0;i<trianglesTerrain.Length;i+=3) {
                AddFoliageAtTriangle(
                    ref vertices,
                    ref uv,
                    ref triangles,
                    verticesTerrain[trianglesTerrain[i]],
                    verticesTerrain[trianglesTerrain[i+1]],
                    verticesTerrain[trianglesTerrain[i+2]],
                    Vector3.Cross((verticesTerrain[trianglesTerrain[i+1]]-verticesTerrain[trianglesTerrain[i]]).normalized, (verticesTerrain[trianglesTerrain[i+2]]-verticesTerrain[trianglesTerrain[i]]).normalized),
                    2);
            }
            meshFilterFoliage.sharedMesh=new Mesh();
            meshFilterFoliage.sharedMesh.name="Foliage";
            meshFilterFoliage.sharedMesh.vertices=vertices.ToArray();
            meshFilterFoliage.sharedMesh.uv=uv.ToArray();
            meshFilterFoliage.sharedMesh.triangles=triangles.ToArray();
            meshFilterFoliage.sharedMesh.RecalculateNormals();
            meshFilterFoliage.sharedMesh.RecalculateTangents();
            meshFilterFoliage.sharedMesh.RecalculateBounds();
            meshRendererFoliage.sharedMaterial=TerrainBrushOverseer.instance.foliageMaterial;
        }

        private Mesh ChooseRandom(float perlinSample, float perlinShift, Mesh[] array) {
            // Select random mesh
            int select = Mathf.RoundToInt(perlinSample*(float)(array.Length-1));
            // Shift by up to length of array
            select = (select + Mathf.RoundToInt(perlinShift*(array.Length-1))) % array.Length;
            return array[select];
        }

        private Mesh ChooseFoliage(float density, float x, float y) {

            float random01 = Random.Range(0f,1f);
            // Low density biases towards grass. Thrillers don't care about density.
            bool grassSpillGauss = random01 * density <= 0.68f;
            bool fillerGauss = random01 * density > 0.68f && random01 * density < 0.95f;
            // We want thrillers to always show up around 5% of the time, so we don't take density into account.
            bool thrillerGauss = random01 >= 0.95f;


            // Now we know what we're doing, we try to group things up a little-- so similar plants kinda show up near eachother.
            float perlinSample = Mathf.Clamp01(Mathf.PerlinNoise(x*10f,y*10f));
            float perlinShiftSample= Mathf.Clamp01(Mathf.PerlinNoise((x+10f)*10f,(y+10f)*10f));

            if (thrillerGauss && TerrainBrushOverseer.instance.foliageMeshesThrillers.Length > 0) {
                return ChooseRandom(perlinSample, perlinShiftSample, TerrainBrushOverseer.instance.foliageMeshesThrillers);
            }

            if (grassSpillGauss) {
                // Choose spillers over grass if the density is low.
                bool spillerCheck = Random.Range(0f,1f)*density < 0.4f;
                if (spillerCheck && TerrainBrushOverseer.instance.foliageMeshesSpillers.Length > 0) {
                    return ChooseRandom(perlinSample, perlinShiftSample, TerrainBrushOverseer.instance.foliageMeshesSpillers);
                } else if (TerrainBrushOverseer.instance.foliageMeshesGrass.Length > 0) {
                    return ChooseRandom(perlinSample, perlinShiftSample, TerrainBrushOverseer.instance.foliageMeshesGrass);
                }
            }

            if (fillerGauss && TerrainBrushOverseer.instance.foliageMeshesFillers.Length > 0) {
                return ChooseRandom(perlinSample, perlinShiftSample, TerrainBrushOverseer.instance.foliageMeshesFillers);
            }
            return null;
        }

        private void AddFoliageAtTriangle(ref List<Vector3> vertices, ref List<Vector2> uv, ref List<int> triangles, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, int recurse) {
            if (recurse>0) {
                Vector3 pm1 = (p1+p2)/2f;
                Vector3 pm2 = (p2+p3)/2f;
                Vector3 pm3 = (p3+p1)/2f;
                AddFoliageAtTriangle(ref vertices, ref uv, ref triangles, p1, pm1, pm3, normal, recurse-1);
                AddFoliageAtTriangle(ref vertices, ref uv, ref triangles, pm1, p2, pm2, normal, recurse-1);
                AddFoliageAtTriangle(ref vertices, ref uv, ref triangles, pm1, pm2, pm3, normal, recurse-1);
                AddFoliageAtTriangle(ref vertices, ref uv, ref triangles, pm2, p3, pm3, normal, recurse-1);
            } else {
                Vector3 triCenter = (p1+p2+p3) / 3f;
                float triSize=Mathf.Min(Vector3.Distance(p1,p2), Vector3.Distance(p2,p3));
                triSize=Mathf.Min(triSize, Vector3.Distance(p1,p3));
                triSize/=50f;
                Vector3 RandomOffset = Quaternion.LookRotation(Quaternion.AngleAxis(Random.Range(0f,360f), Vector3.up) * Vector3.forward, normal) * Vector3.forward * triSize;
                Vector3 texPoint = TerrainBrushOverseer.instance.volume.worldToTexture.MultiplyPoint(transform.TransformPoint(triCenter+RandomOffset));
                //Debug.Log(dataTexture.GetPixel(Mathf.RoundToInt(texPoint.x*TerrainBrushOverseer.instance.volume.texture.width), Mathf.RoundToInt(texPoint.z*TerrainBrushOverseer.instance.volume.texture.height)));
                int x = Mathf.RoundToInt(texPoint.x*TerrainBrushOverseer.instance.volume.texture.width);
                int y = Mathf.RoundToInt(texPoint.y*TerrainBrushOverseer.instance.volume.texture.height);
                float foliageDensity = dataTexture.GetPixel(x, y).g;
                if (Random.Range(0f,1f)>1f-foliageDensity*0.2f) {
                    Mesh chosenMesh=ChooseFoliage(foliageDensity, texPoint.x, texPoint.y);
                    if (chosenMesh == null) {
                        return;
                    }
                    Vector3[] foliageVerts=chosenMesh.vertices;
                    Vector2[] foliageUv=chosenMesh.uv;
                    int[] foliageTriangles=chosenMesh.triangles;
                    int lastVert = vertices.Count;
                    Quaternion rotationFix=Quaternion.LookRotation(Quaternion.AngleAxis(Random.Range(0f,360f), Vector3.up) * Vector3.forward, Vector3.Lerp(normal,Vector3.up,0.5f));
                    rotationFix = rotationFix * Quaternion.Euler(-90f, 0f, 0f);
                    float randomScale = 0.2f * Random.Range(1f,2f);
                    for (int i=0;i<foliageVerts.Length;i++) {
                        Vector3 newVert = rotationFix * foliageVerts[i] * randomScale + triCenter + RandomOffset;
                        vertices.Add(newVert);
                        uv.Add(foliageUv[i]);
                    }
                    for (int i=0;i<foliageTriangles.Length;i++) {
                        triangles.Add(foliageTriangles[i]+lastVert);
                    }
                }
            }
        }
#endif
    }

}
