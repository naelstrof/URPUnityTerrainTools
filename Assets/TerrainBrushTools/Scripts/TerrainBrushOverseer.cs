using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Assertions;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace TerrainBrush {
    [ExecuteInEditMode]
    public class TerrainBrushOverseer : MonoBehaviour {
        private static TerrainBrushOverseer _instance;

        [SerializeField, HideInInspector]
        private Material normalsMaterial;
        [SerializeField, HideInInspector]
        private Material depthMaterial;

        [SerializeField]
        private Texture2D cachedMaskMap;
        [SerializeField]
        private Texture2D cachedNormals;
        [SerializeField]
        private Texture2D cachedDepth;
        
        [Tooltip("Uses serialized images from disk, otherwise we generate them when needed!")]
        public bool useBakedMaps = false;

        [Header("Mesh Generation Settings")]
        public LayerMask meshBrushTargetLayers;
        public Material terrainMaterial;
        public GameObject terrainWrapPrefab;
        [Range(2,32)]
        public int chunkCountSquared = 8;
        [Range(2,6)]
        public int resolutionPow = 4;
        [Range(0f,1f)]
        public float smoothness = 1f;

        [Header("Texture map settings")]
        public float pixelPadding = 1f;
        public int texturePowSize = 10;

        [Header("Foliage Settings")]
        public FoliageData[] foliageMeshes;
        public int seed = 8008569;
        [Range(1,4)]
        public int foliageRecursiveCount = 2;
        [Range(0f,1f)]
        public float foliageDensity = 0.2f;
        [Range(0f,10f)]
        public float foliagePerlinScale = 1f;
        [Range(0f,200f)]
        public float foliageFadeDistance = 25f;
        [Tooltip("Serializes the foliage to the scene, you should have this on while you work, though before you push commits or a build, you should turn this off...")]
        public bool foliageSaveInScene = true;

        public static TerrainBrushOverseer instance {
            get {
                if (!SceneManager.GetActiveScene().IsValid() || !SceneManager.GetActiveScene().isLoaded) {
                    _instance = null;
                    return null;
                }
                if (_instance == null){
                    _instance = UnityEngine.Object.FindObjectOfType<TerrainBrushOverseer>();
                }
                if (_instance == null) {
                    GameObject gameObject = new GameObject("TerrainBrushOverseer", new System.Type[]{typeof(TerrainBrushOverseer)});
                    _instance = gameObject.GetComponent<TerrainBrushOverseer>();
                }
                return _instance;
            }
        }
        private RenderTargetHandle temporaryDepth = new RenderTargetHandle();
        private RenderTargetHandle temporaryMask = new RenderTargetHandle();
        // Generate our projection/view matrix
        public Matrix4x4 projection {
            get {
                return Matrix4x4.Ortho(-volume.bounds.extents.x, volume.bounds.extents.x,
                                       -volume.bounds.extents.z, volume.bounds.extents.z, 0f, volume.bounds.size.y);
            }
        }
        // FIXME: I'm pretty sure the scale is inverted on the z axis due to API differences. Though I'm not sure.
        // As long as we stick to one graphics api, we don't care.
        public Matrix4x4 view {
            get {
                return Matrix4x4.Inverse(Matrix4x4.TRS(volume.bounds.center+Vector3.up*volume.bounds.extents.y, volume.rotation, new Vector3(1, 1, -1)));
            }
        }

        public int callbackOrder => throw new NotImplementedException();


        [HideInInspector]
        public TerrainBrushVolume volume = new TerrainBrushVolume();

        public void OnEnable() {
            temporaryDepth.Init("_TemporaryTerrainBrushDepth");
            temporaryMask.Init("_TemporaryTerrainBrushMask");
            if (instance != this && instance != null) {
                DestroyImmediate(gameObject);
            }
            if (volume != null) {
                Shader.SetGlobalMatrix("_WorldToTexture", volume.worldToTexture);
                Shader.SetGlobalFloat("_FoliageFadeDistance", foliageFadeDistance);
            }
        }
        public void Start() {
            if (!useBakedMaps) {
                // We do a dry run to ensure stuff is initialized correctly.
                GenerateTexture();
                TerrainBrushScheduler.instance.GenerateMesh(transform, volume.worldToTexture, meshBrushTargetLayers, terrainMaterial, terrainWrapPrefab, chunkCountSquared, resolutionPow, smoothness).OnFinish += () => {
                    GenerateTexture(GenerateDepthNormals());
                    if (!foliageSaveInScene) {
                        TerrainBrushScheduler.instance.GenerateFoliage(foliageSaveInScene, seed, foliagePerlinScale, foliageDensity, foliageRecursiveCount, volume.worldToTexture, volume.texture, foliageMeshes);
                    }
                };
                terrainMaterial.SetTexture("_TerrainBlendMap", volume.texture);
                foreach(BlendedBrush b in UnityEngine.Object.FindObjectsOfType<BlendedBrush>()) {
                    b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainBlendMap", volume.texture);
                    b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainDepth", volume.depth);
                    b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainNormals", volume.normals);
                }
            } else {
                terrainMaterial.SetTexture("_TerrainBlendMap", cachedMaskMap);
                foreach(BlendedBrush b in UnityEngine.Object.FindObjectsOfType<BlendedBrush>()) {
                    b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainBlendMap", cachedMaskMap);
                    b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainDepth", cachedDepth);
                    b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainNormals", cachedNormals);
                }
            }
            if (!foliageSaveInScene) {
                TerrainBrushScheduler.instance.GenerateFoliage(foliageSaveInScene, seed, foliagePerlinScale, foliageDensity, foliageRecursiveCount, volume.worldToTexture, useBakedMaps ? (Texture)cachedMaskMap : (Texture)volume.texture, foliageMeshes);
            }
            Shader.SetGlobalFloat("_FoliageFadeDistance", foliageFadeDistance);
        }
        private void RenderTerrainMeshWithMaterial(CommandBuffer cmd, RenderTexture outputTexture, Material withMaterial, Color clearColor) {
            cmd.SetGlobalMatrix("_WorldToTexture", volume.worldToTexture);
            cmd.GetTemporaryRT(temporaryDepth.id, outputTexture.width, outputTexture.height, 0, FilterMode.Bilinear, outputTexture.graphicsFormat, 1, false, RenderTextureMemoryless.None, false);
            cmd.SetRenderTarget(temporaryDepth.Identifier());
            cmd.ClearRenderTarget(true, true, clearColor);
            cmd.SetViewProjectionMatrices(view, projection);
            TerrainWrap[] activeTerrainWraps = UnityEngine.Object.FindObjectsOfType<TerrainWrap>();
            foreach(TerrainWrap wrap in activeTerrainWraps) {
                cmd.DrawMesh(wrap.GetComponent<MeshFilter>().sharedMesh, wrap.transform.localToWorldMatrix, withMaterial);
            }
            cmd.Blit(temporaryDepth.Identifier(), outputTexture);
            cmd.ReleaseTemporaryRT(temporaryDepth.id);
        }
        private RenderTexture GetNewTexture() {
            RenderTexture texture = new RenderTexture(1<<texturePowSize, 1<<texturePowSize, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
            return texture;
        }
        private RenderTexture GetNewNormalsTexture() {
            RenderTexture texture = new RenderTexture(1<<texturePowSize, 1<<texturePowSize, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            return texture;
        }
        private RenderTexture GetNewDepthTexture() {
            RenderTexture texture = new RenderTexture(1<<texturePowSize, 1<<texturePowSize, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat);
            return texture;
        }
        private CommandBuffer GenerateDepthNormals() {
            CommandBuffer cmd = new CommandBuffer();
            // If these are null, then we have nothing to draw, so we just return.
            if (volume == null || volume.texture == null) {
                return cmd;
            }
            if ( volume.normals == null || volume.normals.width != 1<<texturePowSize || volume.normals.height != 1<<texturePowSize) {
                volume.normals = GetNewNormalsTexture();
            }
            if ( volume.depth == null || volume.depth.width != 1<<texturePowSize || volume.depth.height != 1<<texturePowSize) {
                volume.depth = GetNewDepthTexture();
            }
            #if UNITY_EDITOR
            if (normalsMaterial == null) {
                normalsMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("e193d81be99018543abf37ccc4ba04a6"));
                depthMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("e71467d04236c064aaed2dfa2df7e7a7"));
            }
            #endif
            RenderTerrainMeshWithMaterial(cmd, volume.normals, normalsMaterial, Color.green);
            cmd.SetGlobalTexture("_TerrainBrushNormals", volume.normals);
            cmd.SetGlobalTexture("_TerrainBrushDepth", volume.depth);
            RenderTerrainMeshWithMaterial(cmd, volume.depth, depthMaterial, Color.white);
            foreach(BlendedBrush b in UnityEngine.Object.FindObjectsOfType<BlendedBrush>()) {
                b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainBlendMap", volume.texture);
                b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainDepth", volume.depth);
                b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainNormals", volume.normals);
            }
            return cmd;
        }
        private void GenerateTexture() {
            CommandBuffer cmd = new CommandBuffer();
            cmd.SetGlobalTexture("_TerrainBrushDepthNormal", Texture2D.normalTexture);
            GenerateTexture(cmd);
        }
        private void GenerateTexture(CommandBuffer cmd) {
            List<Brush> activeBrushes = new List<Brush>(UnityEngine.Object.FindObjectsOfType<Brush>());
            // Calculate bounds
            if (activeBrushes.Count == 0) {
                Debug.Log("No brushes found.");
                return;
            }

            // Generate the volume that the textures exist on.
            Bounds encapsulatedBounds = new Bounds(activeBrushes[0].brushBounds.center, activeBrushes[0].brushBounds.size);
            foreach(Brush b in activeBrushes) {
                encapsulatedBounds.Encapsulate(b.brushBounds.min);
                encapsulatedBounds.Encapsulate(b.brushBounds.max);
            }
            if (volume == null) {
                volume = new TerrainBrushVolume();
            }
            volume.ResizeToBounds(encapsulatedBounds, texturePowSize, pixelPadding);

            // Regenerate texture if needed.
            if ( volume.texture == null || volume.texture.width != 1<<texturePowSize || volume.texture.height != 1<<texturePowSize) {
                volume.texture = GetNewTexture();
            }

            // Sort the brushes from bottom to top(in camera space)
            activeBrushes.Sort((a,b)=>Vector3.Dot(b.brushBounds.center, volume.rotation * Vector3.forward).CompareTo(Vector3.Dot(a.brushBounds.center,volume.rotation*Vector3.forward)));

            // Debug draw corners on the texture in uv space
            /* Vector4[] UVpoints = { new Vector4(0,0,0,1), new Vector4(1,0,0,1), new Vector4(1,1,0,1), new Vector4(0,1,0,1), new Vector4(0,0,0,1),
                                   new Vector4(0,0,1,1), new Vector4(1,0,1,1), new Vector4(1,1,1,1), new Vector4(0,1,1,1), new Vector4(0,0,1,1) };
            for(int i=0;i<UVpoints.Length;i++) {
                // We project them out into the world to make sure it looks right.
                Debug.DrawLine(volume.textureToWorld.MultiplyPoint(UVpoints[i]), volume.textureToWorld.MultiplyPoint(UVpoints[(i+1)%UVpoints.Length]), Color.red, 5f);
            }*/

            // Render step
            if (cmd == null) {
                cmd = new CommandBuffer();
            }

            // Now prepare a temporary buffer to render to
            cmd.GetTemporaryRT(temporaryMask.id, volume.texture.descriptor);
            cmd.SetRenderTarget(temporaryMask.Identifier());
            cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.SetGlobalMatrix("_WorldToTexture", volume.worldToTexture);
            cmd.SetViewProjectionMatrices(view, projection);
            // Finally queue up the render commands
            foreach(Brush b in activeBrushes) {
                b.Execute(cmd, temporaryMask, volume, view, projection);
            }
            // After the render, we blit directly into the texture
            cmd.Blit(temporaryMask.Identifier(), volume.texture);
            // Then clean up our stuffs.
            cmd.ReleaseTemporaryRT(temporaryMask.id);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
#if UNITY_EDITOR
        public void Bake() {
            if (foliageMeshes == null || foliageMeshes.Length == 0) {
                Debug.LogWarning("No foliage specified. Terrain won't bake.");
                return;
            }
            foreach(var f in foliageMeshes) {
                if (f == null || f.foliageMesh == null) {
                    Debug.LogWarning("Null mesh detected in foliage data. Terrain won't bake.");
                    return;
                }
            }
            if (terrainWrapPrefab == null || terrainMaterial == null || meshBrushTargetLayers == 0) {
                Debug.LogWarning("TerrainBrushOverseer is missing the prefab, material, or layermask! Nothing will generate.", gameObject);
                return;
            }
            if (useBakedMaps) {
                Debug.LogWarning("Something attempted to change the terrain, but the terrain is using baked maps!", gameObject);
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || Application.isPlaying || BuildPipeline.isBuildingPlayer) {
                return;
            }
            if (!SceneManager.GetActiveScene().IsValid() || !SceneManager.GetActiveScene().isLoaded) {
                return;
            }
            GenerateTexture();
            TerrainBrushScheduler.instance.GenerateMesh(transform, volume.worldToTexture, meshBrushTargetLayers, terrainMaterial, terrainWrapPrefab, chunkCountSquared, resolutionPow, smoothness).OnFinish += () => {
                GenerateTexture(GenerateDepthNormals());
                TerrainBrushScheduler.instance.GenerateFoliage(foliageSaveInScene, seed, foliagePerlinScale, foliageDensity, foliageRecursiveCount, volume.worldToTexture, volume.texture, foliageMeshes);
            };
            terrainMaterial.SetTexture("_TerrainBlendMap", volume.texture);
        }
        [MenuItem("Tools/TerrainBrush/New Path Brush")]
        private static void NewPathBrush() {
            string findPath = Path.GetDirectoryName(SceneManager.GetActiveScene().path)+"/PathBrush.prefab";
            GameObject tempPathObj = AssetDatabase.LoadAssetAtPath<GameObject>(findPath);
            if (tempPathObj == null) {
                GameObject pathObj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("9caf7ba2c57f48f4ea061249fed87358"));
                GameObject pathObjInstance = GameObject.Instantiate(pathObj);
                PrefabUtility.SaveAsPrefabAsset(pathObjInstance, findPath);
                tempPathObj = AssetDatabase.LoadAssetAtPath<GameObject>(findPath);
            }
            PrefabUtility.InstantiatePrefab(tempPathObj);
        }

        [MenuItem("Tools/TerrainBrush/New Mesh Brush")]
        private static void NewMeshBrush() {
            string findPath = Path.GetDirectoryName(SceneManager.GetActiveScene().path)+"/MeshBrush.prefab";
            GameObject tempPathObj = AssetDatabase.LoadAssetAtPath<GameObject>(findPath);
            if (tempPathObj == null) {
                GameObject pathObj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("df565f2ee7110db45a9212d73d3fb46e"));
                GameObject pathObjInstance = GameObject.Instantiate(pathObj);
                PrefabUtility.SaveAsPrefabAsset(pathObjInstance, findPath);
                tempPathObj = AssetDatabase.LoadAssetAtPath<GameObject>(findPath);
            }
            PrefabUtility.InstantiatePrefab(tempPathObj);
        }
        [MenuItem("Tools/TerrainBrush/Bake Textures")]
        private static void FinalizeTexture() {
            Texture2D outputTexture = new Texture2D(TerrainBrushOverseer.instance.volume.texture.width, TerrainBrushOverseer.instance.volume.texture.height, TextureFormat.RGBA32, true, true);
            RenderTexture.active = TerrainBrushOverseer.instance.volume.texture;
            outputTexture.ReadPixels(new Rect(0,0,TerrainBrushOverseer.instance.volume.texture.width, TerrainBrushOverseer.instance.volume.texture.height), 0, 0);
            outputTexture.Apply();
            string filename = Path.GetDirectoryName(TerrainBrushOverseer.instance.gameObject.scene.path) + "/TerrainBrushOutput"+TerrainBrushOverseer.instance.gameObject.scene.name+".png";
            //then Save To Disk as PNG
            File.WriteAllBytes(filename, outputTexture.EncodeToPNG());

            Texture2D outputNormalsTexture = new Texture2D(TerrainBrushOverseer.instance.volume.texture.width, TerrainBrushOverseer.instance.volume.texture.height, TextureFormat.RGBAFloat, true, true);
            RenderTexture.active = TerrainBrushOverseer.instance.volume.normals;
            //Graphics.Blit(TerrainBrushOverseer.instance.volume.depthNormals,outputDepthTexture);
            outputNormalsTexture.ReadPixels(new Rect(0,0,TerrainBrushOverseer.instance.volume.normals.width, TerrainBrushOverseer.instance.volume.normals.height), 0, 0);
            outputNormalsTexture.Apply();
            string normalsFilename = Path.GetDirectoryName(TerrainBrushOverseer.instance.gameObject.scene.path) + "/TerrainBrushNormalsOutput"+TerrainBrushOverseer.instance.gameObject.scene.name+".exr";
            File.WriteAllBytes(normalsFilename, outputNormalsTexture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));

            Texture2D outputDepthTexture = new Texture2D(TerrainBrushOverseer.instance.volume.depth.width, TerrainBrushOverseer.instance.volume.depth.height, TextureFormat.RFloat, true, true);
            RenderTexture.active = TerrainBrushOverseer.instance.volume.depth;
            outputDepthTexture.ReadPixels(new Rect(0,0,TerrainBrushOverseer.instance.volume.depth.width, TerrainBrushOverseer.instance.volume.depth.height), 0, 0);
            outputDepthTexture.Apply();
            string depthFilename = Path.GetDirectoryName(TerrainBrushOverseer.instance.gameObject.scene.path) + "/TerrainBrushDepthOutput"+TerrainBrushOverseer.instance.gameObject.scene.name+".png";
            //then Save To Disk as PNG
            File.WriteAllBytes(depthFilename, outputDepthTexture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));

            UnityEditor.AssetDatabase.Refresh();
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath( filename );
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            TextureImporter normalImporter = (TextureImporter)TextureImporter.GetAtPath( normalsFilename );
            normalImporter.sRGBTexture = false;
            normalImporter.alphaSource = TextureImporterAlphaSource.None;
            normalImporter.mipmapEnabled = false;
            normalImporter.isReadable = true;
            EditorUtility.SetDirty(normalImporter);
            normalImporter.SaveAndReimport();

            TextureImporter depthImporter = (TextureImporter)TextureImporter.GetAtPath( depthFilename );
            depthImporter.sRGBTexture = false;
            depthImporter.alphaSource = TextureImporterAlphaSource.None;
            depthImporter.textureType = TextureImporterType.SingleChannel;
            depthImporter.mipmapEnabled = false;
            depthImporter.isReadable = true;
            EditorUtility.SetDirty(depthImporter);
            depthImporter.SaveAndReimport();

            TerrainBrushOverseer.instance.cachedMaskMap = AssetDatabase.LoadAssetAtPath<Texture2D>(filename);
            TerrainBrushOverseer.instance.cachedNormals = AssetDatabase.LoadAssetAtPath<Texture2D>(normalsFilename);
            TerrainBrushOverseer.instance.cachedDepth = AssetDatabase.LoadAssetAtPath<Texture2D>(depthFilename);
            /*TerrainBrushOverseer.instance.terrainMaterial?.SetTexture("_TerrainBlendMap", TerrainBrushOverseer.instance.cachedMaskMap);

            foreach(BlendedBrush b in UnityEngine.Object.FindObjectsOfType<BlendedBrush>()) {
                b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainBlendMap", TerrainBrushOverseer.instance.cachedMaskMap);
                b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainDepth", TerrainBrushOverseer.instance.cachedDepth);
                b.GetComponent<Renderer>().sharedMaterial?.SetTexture("_TerrainNormals", TerrainBrushOverseer.instance.cachedNormals);
            }
            //TerrainBrushOverseer.instance.GenerateFoliage();
            TerrainBrushScheduler.instance.GenerateFoliage(TerrainBrushOverseer.instance.foliageSaveInScene,
                                                           TerrainBrushOverseer.instance.seed,
                                                           TerrainBrushOverseer.instance.foliagePerlinScale,
                                                           TerrainBrushOverseer.instance.foliageDensity,
                                                           TerrainBrushOverseer.instance.foliageRecursiveCount,
                                                           TerrainBrushOverseer.instance.volume.textureToWorld,
                                                           TerrainBrushOverseer.instance.cachedMaskMap,
                                                           TerrainBrushOverseer.instance.foliageMeshes);*/
        }

        private void OnValidate() {
            Shader.SetGlobalFloat("_FoliageFadeDistance", foliageFadeDistance);
        }
        #endif
    }
}

