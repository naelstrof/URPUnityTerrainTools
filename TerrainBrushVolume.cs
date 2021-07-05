using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TerrainBrush {
    public class TerrainBrushVolume : ScriptableObject {
        public TerrainBrushVolume() { }
        public TerrainBrushVolume(Bounds b) {
            internalBounds = b;
            internalTextureToWorld = Matrix4x4.TRS(b.min, Quaternion.FromToRotation(Vector3.forward, Vector3.down), new Vector3(b.size.x, b.size.z, b.size.y));
        }
        public void ResizeToBounds(Bounds b) {
            internalBounds = b;
            internalTextureToWorld = Matrix4x4.TRS(b.min, Quaternion.FromToRotation(Vector3.forward, Vector3.down), new Vector3(b.size.x, b.size.z, b.size.y));
        }

        // Variables we serialize and store to disk
        public int texturePowSize = 10;
        public string scenePath;

        [SerializeField] [HideInInspector]
        private Bounds internalBounds;
        [SerializeField] [HideInInspector]
        private Matrix4x4 internalTextureToWorld;
        
        // Getters to prevent unintentional changes. There shouldn't be a reason to change these directly.
        public Bounds bounds { get => internalBounds; }
        public Matrix4x4 textureToWorld {get => internalTextureToWorld;}
        public RenderTexture texture;
        public Vector3 position { 
            get { return textureToWorld.MultiplyPoint(new Vector4(0f,0f,0f,1f)); }
        }
        public Vector3 lossyScale {
            get { return textureToWorld.lossyScale; }
        }
        public Quaternion rotation {
            // We cannot return textureToWorld.rotation because it's not a valid TRS.
            get { return Quaternion.FromToRotation(Vector3.forward, Vector3.down); }
        }
        public Matrix4x4 worldToTexture {
            get { return Matrix4x4.Inverse(textureToWorld); }
        }
    }
}
