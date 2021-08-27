using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TerrainBrush {
    // Made to be inherited.
    [ExecuteInEditMode]
    public abstract class Brush : MonoBehaviour {
        public virtual Bounds brushBounds {
            get {
                return new Bounds(transform.position, transform.lossyScale);
            }
        }
        public abstract void Execute(CommandBuffer cmd, RenderTargetHandle renderTarget, TerrainBrushVolume volume, Matrix4x4 view, Matrix4x4 projection);
        public virtual void Start() {
            transform.hasChanged = false;
        }
        public virtual void OnEnable() {
            transform.hasChanged = false;
        }
        public virtual void OnDrawGizmosSelected() {
            #if UNITY_EDITOR
            if (transform.hasChanged) {
                TerrainBrushOverseer.instance?.Bake();
                transform.hasChanged=false;
            }
            #endif
            //Gizmos.DrawWireCube(brushBounds.center, brushBounds.size);
        }

    }
}