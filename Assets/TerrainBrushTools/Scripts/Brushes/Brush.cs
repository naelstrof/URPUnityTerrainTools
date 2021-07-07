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
        public virtual void OnDrawGizmos() {
            if (transform.hasChanged) {
                TerrainBrushOverseer.instance.Bake();
                transform.hasChanged=false;
            }
            //Gizmos.DrawWireCube(brushBounds.center, brushBounds.size);
        }

    }
}