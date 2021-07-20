using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TerrainBrush {
    [ExecuteInEditMode]
    public class BlendedBrush : Brush {
        public override Bounds brushBounds {
            get {
                return GetComponent<Renderer>().bounds;
            }
        }
        public override void Execute(CommandBuffer cmd, RenderTargetHandle renderTarget, TerrainBrushVolume volume, Matrix4x4 view, Matrix4x4 projection) {
            // Nothing needs to be done, just needs to be included on the map.
        }
    }
}