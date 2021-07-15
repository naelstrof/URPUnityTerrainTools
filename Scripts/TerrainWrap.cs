using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TerrainBrush {

    [ExecuteAlways]
    [RequireComponent(typeof(LODGroup))]
    public class TerrainWrap : MonoBehaviour {
        [HideInInspector]
        public int chunkID=0;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        [SerializeField, HideInInspector]
        private LODGroup group;
        void Update() {
            Camera cam = Camera.main;
            if (Application.isEditor && !Application.isPlaying) {
                group.ForceLOD(0);
                return;
            }
            if (cam == null) {
                cam = Camera.current;
            }
            if (meshRenderer == null) {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            if (cam == null) {
                group.ForceLOD(1);
                return;
            }
            if (Vector3.Distance(cam.transform.position, meshRenderer.bounds.center)-meshRenderer.bounds.extents.magnitude > TerrainBrushOverseer.instance.foliageFadeDistance) {
                group.ForceLOD(1);
            } else {
                group.ForceLOD(0);
            }
        }
        public void Generate(int chunkID, Bounds encapsulatedBounds, int resolution, int chunks, float smooth ) {
            this.chunkID = chunkID;
            transform.position=encapsulatedBounds.min;
            transform.rotation=Quaternion.identity;
            float size=Mathf.Max(encapsulatedBounds.size.x, encapsulatedBounds.size.z);
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

        private GameObject GetFoliageSubMesh(int index) {
            GameObject foliage;
            Transform foliageT = transform.Find("Foliage"+index);
            if (foliageT!=null) {
                foliage=foliageT.gameObject;
            } else {
                foliage=new GameObject("Foliage"+index, new System.Type[]{typeof(MeshFilter), typeof(MeshRenderer)});
                foliage.transform.parent=transform;
            }
            if (TerrainBrushOverseer.instance.locked) {
                foliage.hideFlags = HideFlags.HideAndDontSave;
            } else {
                foliage.hideFlags = HideFlags.HideInHierarchy;
            }
            foliage.transform.localPosition=Vector3.zero;
            foliage.transform.localRotation=Quaternion.identity;
            return foliage;
        }

        public void GenerateFoliage(float foliagePerlinScale, float density, int recurseCount, FoliageData[] foliageData, Matrix4x4 worldToTexture, Texture2D maskTexture) {
            if (meshFilter == null) {
                meshFilter = GetComponent<MeshFilter>();
            }
            if (meshFilter == null || meshFilter.sharedMesh == null) {
                Debug.LogError("Failed to generate foliage, there was no terrain to generate it on!");
                return;
            }
            Vector3[] verticesTerrain = meshFilter.sharedMesh.vertices;
            Vector3[] normalsTerrain = meshFilter.sharedMesh.normals;
            int[] trianglesTerrain = meshFilter.sharedMesh.triangles;
            Vector2[] uvTerrain = meshFilter.sharedMesh.uv;

            List<Renderer> renderers = new List<Renderer>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            List<Vector3> uv2 = new List<Vector3>();
            List<Vector3> uv3 = new List<Vector3>();
            int foliageIndex = 0;
            for (int i=0;i<trianglesTerrain.Length;i+=3) {
                AddFoliageAtTriangle(
                    foliagePerlinScale,
                    density,
                    foliageData,
                    worldToTexture,
                    maskTexture,
                    ref vertices,
                    ref uv,
                    ref uv2,
                    ref uv3,
                    ref triangles,
                    ref colors,
                    verticesTerrain[trianglesTerrain[i]],
                    verticesTerrain[trianglesTerrain[i+1]],
                    verticesTerrain[trianglesTerrain[i+2]],
                    Vector3.Cross((verticesTerrain[trianglesTerrain[i+1]]-verticesTerrain[trianglesTerrain[i]]).normalized, (verticesTerrain[trianglesTerrain[i+2]]-verticesTerrain[trianglesTerrain[i]]).normalized),
                    recurseCount);
                if (vertices.Count > 50000) { // Create a submesh! We hit the vertex limit
                    GameObject target = GetFoliageSubMesh(foliageIndex++);
                    MeshFilter meshFilterFoliage = target.GetComponent<MeshFilter>();
                    MeshRenderer meshRendererFoliage = target.GetComponent<MeshRenderer>();
                    renderers.Add(meshRendererFoliage);
                    meshFilterFoliage.sharedMesh=new Mesh();
                    meshFilterFoliage.sharedMesh.name="Foliage";
                    meshFilterFoliage.sharedMesh.vertices=vertices.ToArray();
                    meshFilterFoliage.sharedMesh.uv=uv.ToArray();
                    meshFilterFoliage.sharedMesh.SetUVs(1,uv2);
                    meshFilterFoliage.sharedMesh.SetUVs(2,uv3);
                    meshFilterFoliage.sharedMesh.triangles=triangles.ToArray();
                    meshFilterFoliage.sharedMesh.colors=colors.ToArray();
                    meshFilterFoliage.sharedMesh.RecalculateNormals();
                    meshFilterFoliage.sharedMesh.RecalculateTangents();
                    meshFilterFoliage.sharedMesh.RecalculateBounds();
                    meshRendererFoliage.sharedMaterial=foliageData[0].foliageMaterial;
                    vertices = new List<Vector3>();
                    uv = new List<Vector2>();
                    triangles = new List<int>();
                    colors = new List<Color>();
                    uv2 = new List<Vector3>();
                    uv3 = new List<Vector3>();
                }
            }
            // Write out whatever we have left to an additional submesh.
            GameObject ftarget = GetFoliageSubMesh(foliageIndex++);
            MeshFilter fmeshFilterFoliage = ftarget.GetComponent<MeshFilter>();
            MeshRenderer fmeshRendererFoliage = ftarget.GetComponent<MeshRenderer>();
            renderers.Add(fmeshRendererFoliage);
            fmeshFilterFoliage.sharedMesh=new Mesh();
            fmeshFilterFoliage.sharedMesh.name="Foliage";
            fmeshFilterFoliage.sharedMesh.vertices=vertices.ToArray();
            fmeshFilterFoliage.sharedMesh.uv=uv.ToArray();
            fmeshFilterFoliage.sharedMesh.SetUVs(1,uv2);
            fmeshFilterFoliage.sharedMesh.SetUVs(2,uv3);
            fmeshFilterFoliage.sharedMesh.triangles=triangles.ToArray();
            fmeshFilterFoliage.sharedMesh.colors=colors.ToArray();
            fmeshFilterFoliage.sharedMesh.RecalculateNormals();
            fmeshFilterFoliage.sharedMesh.RecalculateTangents();
            fmeshFilterFoliage.sharedMesh.RecalculateBounds();
            fmeshRendererFoliage.sharedMaterial=foliageData[0].foliageMaterial;

            LOD newLod = new LOD();
            newLod.renderers = renderers.ToArray();
            newLod.screenRelativeTransitionHeight = 0.25f;
            if (group == null) {
                group = GetComponent<LODGroup>();
            }
            if (group == null) {
                group = gameObject.AddComponent<LODGroup>();
            }
            group.SetLODs(new LOD[]{newLod, new LOD()});

            if (transform.Find("Foliage")) {
                DestroyImmediate(transform.Find("Foliage").gameObject);
            }
            // Finally delete left-over submeshes.
            int childCount = transform.childCount;
            for (int i=foliageIndex;i<childCount;i++) {
                if (transform.Find("Foliage"+i) != null) {
                    DestroyImmediate(transform.Find("Foliage"+i).gameObject);
                }
            }
        }

        private Mesh ChooseRandom(FoliageData[] foliageData, float perlinSample, float perlinShift, FoliageData.FoliageAspect aspect) {
            int count = FoliageData.GetFoliageCount(foliageData, aspect);
            // Select random mesh
            int select = Mathf.RoundToInt(perlinSample*(float)(count-1));
            // Shift by up to length of array
            select = (select + Mathf.RoundToInt(perlinShift*(count-1))) % count;
            return FoliageData.GetFoliage(foliageData, aspect, select).foliageMesh;
        }

        private Mesh ChooseFoliage(FoliageData[] foliageData, float foliagePerlinScale, float density, float x, float y) {
            float random01 = Random.Range(0f,1f);
            // Low density biases towards grass. Thrillers don't care about density.
            bool grassSpillGauss = random01 * density <= 0.68f;
            bool fillerGauss = random01 * density > 0.68f && random01 * density < 0.95f;
            // We want thrillers to always show up around 5% of the time, so we don't take density into account.
            bool thrillerGauss = random01 >= 0.95f;


            // Now we know what we're doing, we try to group things up a little-- so similar plants kinda show up near eachother.
            float perlinScale = 10f*foliagePerlinScale;
            float perlinSample = Mathf.Clamp01(Mathf.PerlinNoise(x*perlinScale,y*perlinScale));
            float perlinShiftSample= Mathf.Clamp01(Mathf.PerlinNoise((x+perlinScale)*perlinScale,(y+perlinScale)*perlinScale));

            if (thrillerGauss && FoliageData.GetFoliageCount(foliageData, FoliageData.FoliageAspect.Thriller) > 0) {
                return ChooseRandom(foliageData, perlinSample, perlinShiftSample, FoliageData.FoliageAspect.Thriller);
            }

            if (grassSpillGauss) {
                // Choose spillers over grass if the density is low.
                bool spillerCheck = Random.Range(0f,1f)*density < 0.2f;
                if (spillerCheck && FoliageData.GetFoliageCount(foliageData, FoliageData.FoliageAspect.Spiller) > 0) {
                    return ChooseRandom(foliageData, perlinSample, perlinShiftSample, FoliageData.FoliageAspect.Spiller);
                } else if (FoliageData.GetFoliageCount(foliageData, FoliageData.FoliageAspect.Grass) > 0) {
                    return ChooseRandom(foliageData, perlinSample, perlinShiftSample, FoliageData.FoliageAspect.Grass);
                }
            }

            if (fillerGauss && FoliageData.GetFoliageCount(foliageData, FoliageData.FoliageAspect.Filler) > 0) {
                return ChooseRandom(foliageData, perlinSample, perlinShiftSample, FoliageData.FoliageAspect.Filler);
            }
            return null;
        }

        private void AddFoliageAtTriangle(float foliagePerlinScale, float density, FoliageData[] foliageData, Matrix4x4 worldToTexture, Texture2D maskTexture, ref List<Vector3> vertices, ref List<Vector2> uv, ref List<Vector3> uv2, ref List<Vector3> uv3, ref List<int> triangles, ref List<Color> colors, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, int recurse) {
            if (recurse>0) {
                Vector3 pm1 = (p1+p2)/2f;
                Vector3 pm2 = (p2+p3)/2f;
                Vector3 pm3 = (p3+p1)/2f;
                AddFoliageAtTriangle(foliagePerlinScale, density, foliageData, worldToTexture, maskTexture, ref vertices, ref uv, ref uv2, ref uv3, ref triangles, ref colors, p1, pm1, pm3, normal, recurse-1);
                AddFoliageAtTriangle(foliagePerlinScale, density, foliageData, worldToTexture, maskTexture, ref vertices, ref uv, ref uv2, ref uv3, ref triangles, ref colors, pm1, p2, pm2, normal, recurse-1);
                AddFoliageAtTriangle(foliagePerlinScale, density, foliageData, worldToTexture, maskTexture, ref vertices, ref uv, ref uv2, ref uv3, ref triangles, ref colors, pm1, pm2, pm3, normal, recurse-1);
                AddFoliageAtTriangle(foliagePerlinScale, density, foliageData, worldToTexture, maskTexture, ref vertices, ref uv, ref uv2, ref uv3, ref triangles, ref colors, pm2, p3, pm3, normal, recurse-1);
            } else {
                Vector3 triCenter = (p1+p2+p3) / 3f;
                float triSize=Mathf.Min(Vector3.Distance(p1,p2), Vector3.Distance(p2,p3));
                triSize=Mathf.Min(triSize, Vector3.Distance(p1,p3));
                triSize/=50f;
                Vector3 RandomOffset = Quaternion.LookRotation(Quaternion.AngleAxis(Random.Range(0f,360f), Vector3.up) * Vector3.forward, normal) * Vector3.forward * triSize;
                Vector3 texPoint = worldToTexture.MultiplyPoint(transform.TransformPoint(triCenter+RandomOffset));
                //Debug.Log(dataTexture.GetPixel(Mathf.RoundToInt(texPoint.x*TerrainBrushOverseer.instance.volume.texture.width), Mathf.RoundToInt(texPoint.z*TerrainBrushOverseer.instance.volume.texture.height)));
                int x = Mathf.RoundToInt(texPoint.x*maskTexture.width);
                int y = Mathf.RoundToInt(texPoint.y*maskTexture.height);
                float groundDensity = maskTexture.GetPixel(x, y).g;
                if (Random.Range(0f,1f)>1f-groundDensity*density) {
                    Mesh chosenMesh=ChooseFoliage(foliageData, foliagePerlinScale, groundDensity, texPoint.x, texPoint.y);
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
                    Vector3 foliageCenter = triCenter + RandomOffset;
                    for (int i=0;i<foliageVerts.Length;i++) {
                        Vector3 newVert = rotationFix * foliageVerts[i] * randomScale + foliageCenter;
                        vertices.Add(newVert);
                        uv.Add(foliageUv[i]);
                        float windAmount = Mathf.Clamp01(foliageVerts[i].z / chosenMesh.bounds.size.z);
                        windAmount *= chosenMesh.bounds.extents.z;
                        colors.Add(new Color(windAmount*0.01f, windAmount, windAmount, 0f));
                        uv2.Add(transform.TransformPoint(foliageCenter));
                        uv3.Add(normal.normalized);
                    }
                    for (int i=0;i<foliageTriangles.Length;i++) {
                        triangles.Add(foliageTriangles[i]+lastVert);
                    }
                }
            }
        }
    }

}
