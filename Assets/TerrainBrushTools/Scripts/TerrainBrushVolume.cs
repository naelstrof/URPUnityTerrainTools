using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TerrainBrush {
    [System.Serializable]
    public class TerrainBrushVolume : ISerializationCallbackReceiver {
        public TerrainBrushVolume() { }
        public void ResizeToBounds(Bounds b, int texturePowSize, float padding) {
            float textureSize = 1<<texturePowSize;
            Vector3 pixelPadding = new Vector3(padding/textureSize*b.size.x, padding/textureSize*b.size.y, padding/textureSize*b.size.z);
            internalBounds = b;
            internalBounds.min -= pixelPadding;
            internalBounds.max += pixelPadding;
            internalTextureToWorld = Matrix4x4.TRS(new Vector3(b.min.x, b.max.y, b.min.z), Quaternion.FromToRotation(Vector3.forward, Vector3.down), new Vector3(b.size.x, b.size.z, b.size.y));
        }

        public void OnBeforeSerialize() {
            internalMatrix1 = internalTextureToWorld.GetRow(0);
            internalMatrix2 = internalTextureToWorld.GetRow(1);
            internalMatrix3 = internalTextureToWorld.GetRow(2);
            internalMatrix4 = internalTextureToWorld.GetRow(3);
        }

        public void OnAfterDeserialize() {
            internalTextureToWorld.SetRow(0,internalMatrix1);
            internalTextureToWorld.SetRow(1,internalMatrix2);
            internalTextureToWorld.SetRow(2,internalMatrix3);
            internalTextureToWorld.SetRow(3,internalMatrix4);
        }

        // Variables we serialize and store to disk

        [SerializeField] [HideInInspector]
        private Bounds internalBounds;
        [SerializeField] [HideInInspector]
        private Vector4 internalMatrix1 = new Vector4(1,0,0,0);
        [SerializeField] [HideInInspector]
        private Vector4 internalMatrix2 = new Vector4(0,1,0,0);
        [SerializeField] [HideInInspector]
        private Vector4 internalMatrix3 = new Vector4(0,0,1,0);
        [SerializeField] [HideInInspector]
        private Vector4 internalMatrix4 = new Vector4(0,0,0,1);
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
