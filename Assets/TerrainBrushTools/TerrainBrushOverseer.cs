using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
namespace TerrainBrush {
    public static class TerrainBrushOverseer {
        public static TerrainBrushVolume GetCurrentTerrainBrushVolume() {
            Scene activeScene = SceneManager.GetActiveScene();
            TerrainBrushVolume[] volumes = Resources.FindObjectsOfTypeAll<TerrainBrushVolume>();
            foreach(var v in volumes) {
                if (v.scenePath == activeScene.path) {
                    return v;
                }
            }
            TerrainBrushVolume newVolume = ScriptableObject.CreateInstance<TerrainBrushVolume>();
            newVolume.scenePath = activeScene.path;
            RenderTexture texture= new RenderTexture(1<<newVolume.texturePowSize, 1<<newVolume.texturePowSize, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SNorm);
            string texturePath = Path.GetDirectoryName(activeScene.path) + "/TerrainBrushVolume"+activeScene.name+".renderTexture";
            AssetDatabase.CreateAsset (texture, texturePath);
            newVolume.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(texturePath);
            AssetDatabase.CreateAsset (newVolume, Path.GetDirectoryName(activeScene.path) + "/TerrainBrushVolume"+activeScene.name+".asset");
            AssetDatabase.SaveAssets();
            return newVolume;
        }
        [MenuItem("Tools/TerrainBrush/Generate Texture")]
        public static void GenerateTexture() {
            // Calculate bounds
            Brush[] brushes = Object.FindObjectsOfType<Brush>();
            if (brushes.Length == 0) {
                Debug.Log("No brushes found.");
                return;
            }
            Bounds encapsulatedBounds = new Bounds(brushes[0].brushBounds.center, brushes[0].brushBounds.size);
            foreach(Brush b in brushes) {
                encapsulatedBounds.Encapsulate(b.brushBounds.min);
                encapsulatedBounds.Encapsulate(b.brushBounds.max);
            }
            TerrainBrushVolume volume = GetCurrentTerrainBrushVolume();
            volume.ResizeToBounds(encapsulatedBounds);
            // corners on the texture in uv space
            Vector4[] UVpoints = { new Vector4(0,0,0,1), new Vector4(1,0,0,1), new Vector4(1,1,0,1), new Vector4(0,1,0,1)};
            for(int i=0;i<UVpoints.Length;i++) {
                // We project them out into the world to make sure it looks right.
                Debug.DrawLine(volume.textureToWorld.MultiplyPoint(UVpoints[i]), volume.textureToWorld.MultiplyPoint(UVpoints[(i+1)%UVpoints.Length]), Color.red, 5f);
            }

            // Render step
            CommandBuffer cmd = new CommandBuffer();

            // Now prepare a temporary buffer to render to
            RenderTargetHandle temporaryTexture = new RenderTargetHandle();
            temporaryTexture.Init("_TerrainBrushTemporaryTexture");

            cmd.GetTemporaryRT(temporaryTexture.id, volume.texture.descriptor);
            cmd.SetRenderTarget(temporaryTexture.id);

            // Generate our projection/view matrix
            Matrix4x4 projection = Matrix4x4.Ortho(-volume.bounds.extents.x, volume.bounds.extents.x,
                                                   -volume.bounds.extents.z, volume.bounds.extents.z, 0f, volume.bounds.size.z);

            // FIXME: I'm pretty sure the scale is inverted on the z axis due to API differences. Though I'm not sure.
            // As long as we stick to one graphics api, we don't care.
            Matrix4x4 view = Matrix4x4.Inverse(Matrix4x4.TRS(volume.bounds.center+Vector3.up*volume.bounds.size.y, volume.rotation, new Vector3(1, 1, -1)));
            cmd.SetViewProjectionMatrices(view, projection);
            // Finally queue up the render commands
            foreach(Brush b in brushes) {
                b.Execute(cmd, temporaryTexture, volume);
            }
            // After the render, we blit directly into the texture
            cmd.Blit(temporaryTexture.id, volume.texture);

            Graphics.ExecuteCommandBuffer(cmd);

            // Then clean up our stuffs.
            cmd.ReleaseTemporaryRT(temporaryTexture.id);
            cmd.Release();
            Debug.Log("Success! A render texture should be next to the scene now.");
        }
    }
}
#endif
