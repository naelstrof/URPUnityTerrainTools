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
namespace TerrainBrush {
    public class TerrainBrushOverseer : MonoBehaviour {
        public LayerMask meshBrushTargetLayers;
        public GameObject terrainWrapPrefab;
        private static TerrainBrushOverseer _instance;
        public static TerrainBrushOverseer instance {
            get {
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
        public enum BakeState {
            Idle = 0,
            Prepass,
            MeshGeneration,
            TextureGeneration,
            Finished,
        }
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

        private BakeState currentState = BakeState.Idle;
        public TerrainBrushVolume volume = new TerrainBrushVolume();

        [HideInInspector]
        public List<TerrainWrap> activeTerrainWraps = new List<TerrainWrap>();

        public float pixelPadding = 1f;
        public int texturePowSize = 10;
        private RenderTexture GetTexture() {
            Scene activeScene = SceneManager.GetActiveScene();
            Assert.IsTrue(activeScene.IsValid());
            RenderTexture texture = new RenderTexture(1<<texturePowSize, 1<<texturePowSize, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
            string texturePath = Path.GetDirectoryName(activeScene.path) + "/TerrainBrushVolume"+activeScene.name+".renderTexture";
            AssetDatabase.CreateAsset (texture, texturePath);
            AssetDatabase.SaveAssets();
            return texture;
        }
        private void BakeTick() {
            try {
                switch(currentState) {
                    case BakeState.Idle: {
                        currentState = BakeState.Prepass;
                        GenerateTexture();
                        break;
                    } 
                    case BakeState.Prepass: {
                        currentState = BakeState.MeshGeneration;
                        GenerateMesh();
                        break;
                    }
                    case BakeState.MeshGeneration: {
                        Assert.IsTrue(activeTerrainWraps.Count > 0);
                        if (activeTerrainWraps[activeTerrainWraps.Count-1].generated) {
                            currentState = BakeState.TextureGeneration;
                        }
                        break;
                    }
                    case BakeState.TextureGeneration: {
                        GenerateTexture(GenerateDepthNormals());
                        currentState = BakeState.Finished;
                        EditorApplication.update -= BakeTick;
                        break;
                    }
                }
            } catch (Exception e) {
                Debug.LogException(e);
                currentState = BakeState.Idle;
                EditorApplication.update -= BakeTick;
            }
        }
        public void Bake() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                return;
            }
            if (!SceneManager.GetActiveScene().IsValid() || !SceneManager.GetActiveScene().isLoaded) {
                return;
            }
            currentState = BakeState.Idle;
            EditorApplication.update -= BakeTick;
            EditorApplication.update += BakeTick;
        }
        public CommandBuffer GenerateDepthNormals() {
            CommandBuffer cmd = new CommandBuffer();
            // If these are null, then we have nothing to draw, so we just return.
            if (volume == null || volume.texture == null) {
                return cmd;
            }
            RenderTargetHandle temporaryTexture = new RenderTargetHandle();
            temporaryTexture.Init("_TerrainBrushDepthNormalGenerate");
            cmd.GetTemporaryRT(temporaryTexture.id, volume.texture.width, volume.texture.height, 0, FilterMode.Bilinear, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, 1, false, RenderTextureMemoryless.None, false);
            cmd.SetRenderTarget(temporaryTexture.id);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.SetViewProjectionMatrices(view, projection);
            Material depthNormalMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("e193d81be99018543abf37ccc4ba04a6"));
            foreach(TerrainWrap wrap in activeTerrainWraps) {
                cmd.DrawMesh(wrap.GetComponent<MeshFilter>().sharedMesh, wrap.transform.localToWorldMatrix, depthNormalMaterial);
            }
            cmd.SetGlobalTexture("_TerrainBrushDepthNormal", temporaryTexture.id);
            return cmd;
        }
        [MenuItem("TerrainBrush/Finalize Texture")]
        private static void FinalizeTexture() {
            Texture2D outputTexture = new Texture2D(TerrainBrushOverseer.instance.volume.texture.width, TerrainBrushOverseer.instance.volume.texture.height, TextureFormat.RGBA32, true, true);
            RenderTexture.active = TerrainBrushOverseer.instance.volume.texture;
            outputTexture.ReadPixels(new Rect(0,0,TerrainBrushOverseer.instance.volume.texture.width, TerrainBrushOverseer.instance.volume.texture.height), 0, 0);
            outputTexture.Apply();
            string filename = Path.GetDirectoryName(TerrainBrushOverseer.instance.gameObject.scene.path) + "/TerrainBrushOutput"+TerrainBrushOverseer.instance.gameObject.scene.name+".png";
            //then Save To Disk as PNG
            File.WriteAllBytes(filename, outputTexture.EncodeToPNG());
            UnityEditor.AssetDatabase.Refresh();
            Texture2D realTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filename);
            UnityEngine.Object.FindObjectOfType<TerrainWrap>().GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BlendMap", realTexture);
        }

        [ContextMenu("Generate Texture")]
        public void GenerateTexture() {
            CommandBuffer cmd = new CommandBuffer();
            cmd.SetGlobalTexture("_TerrainBrushDepthNormal", Texture2D.normalTexture);
            GenerateTexture(cmd);
        }
        public void GenerateTexture(CommandBuffer cmd) {
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
                volume.texture = GetTexture();
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
            RenderTargetHandle temporaryTexture = new RenderTargetHandle();
            temporaryTexture.Init("_TerrainBrushTemporaryTexture");

            cmd.GetTemporaryRT(temporaryTexture.id, volume.texture.descriptor);
            cmd.SetRenderTarget(temporaryTexture.id);
            cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.SetViewProjectionMatrices(view, projection);
            // Finally queue up the render commands
            foreach(Brush b in activeBrushes) {
                b.Execute(cmd, temporaryTexture, volume, view, projection);
            }
            // After the render, we blit directly into the texture
            cmd.Blit(temporaryTexture.id, volume.texture);
            // Then clean up our stuffs.
            cmd.ReleaseTemporaryRT(temporaryTexture.id);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
        [ContextMenu("Generate Mesh")]
        public void GenerateMesh() {
            //GameObject terrainWrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("f63f0a5e964e419408e9f8f5bce8b9dd"));
            // If our terrainwrap is null, we'll just auto generate it. This is because we cannot edit materials or prefabs within a package, that's illegal!
            if (terrainWrapPrefab == null) {
                GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("f63f0a5e964e419408e9f8f5bce8b9dd"));
                Material originalMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("ad5bce9faf2a3d04a9048b1c7606615d"));
                Scene activeScene = SceneManager.GetActiveScene();
                Assert.IsTrue(activeScene.IsValid());

                string matPath = Path.GetDirectoryName(activeScene.path) + "/TerrainBrushVolumeTerrainMat"+activeScene.name+".mat";
                AssetDatabase.CreateAsset(Material.Instantiate(originalMaterial), matPath);
                Material instanceMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);

                GameObject instance = GameObject.Instantiate(originalPrefab);
                string assetPath = Path.GetDirectoryName(activeScene.path) + "/TerrainBrushVolumeTerrainWrap"+activeScene.name+".prefab";
                instance.GetComponent<MeshRenderer>().sharedMaterial = instanceMaterial;
                PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
                terrainWrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
            for (int i=0; i<activeTerrainWraps.Count; i++) {
                if (activeTerrainWraps[i]==null) {
                    activeTerrainWraps.RemoveAt(i);
                    i--;
                }
            }
            if (activeTerrainWraps.Count<64) {
                foreach(TerrainWrap t in UnityEngine.Object.FindObjectsOfType<TerrainWrap>()) {
                    if (!activeTerrainWraps.Contains(t)) {
                        activeTerrainWraps.Add(t);
                    }
                }
            }
            // If we've recently baked, we'll have a non-render texture in this slot. So we update it.
            if (activeTerrainWraps.Count > 0) {
                activeTerrainWraps[0].GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BlendMap", volume.texture);
            }
            activeTerrainWraps.Sort((a,b)=>(a.chunkID.CompareTo(b.chunkID)));
            for (int i=0;i<64;i++) {
                if (activeTerrainWraps.Count<=i) {
                    GameObject newTerrainWrapObject = GameObject.Instantiate(terrainWrapPrefab, Vector3.zero, Quaternion.identity);
                    newTerrainWrapObject.transform.parent = transform;
                    activeTerrainWraps.Add(newTerrainWrapObject.GetComponent<TerrainWrap>());
                }
                activeTerrainWraps[i].SetChunkID(i);
            }
        }
        public void OnValidate() {
            Bake();
        }
    }
}
#endif