using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TerrainBrush {
    // Made to be inherited.
    public class MeshBrush : Brush {
        public Material renderMaterial;
        public Mesh renderMesh;
        public override Bounds brushBounds {
            get {
                // FIXME: Bounds are meant to be axis aligned, though meshes can just be transformed like whatever.
                // There's probably a helper function to transform local bounds into world bounds, but here's my crappy solution anyway.
                Vector3 minPoint = transform.TransformPoint(renderMesh.bounds.min);
                Vector3 maxPoint = transform.TransformPoint(renderMesh.bounds.max);
                Vector3 center = transform.TransformPoint(renderMesh.bounds.center);
                Bounds b = new Bounds();
                b.center = center;
                b.Encapsulate(minPoint);
                b.Encapsulate(maxPoint);
                return b;
            }
        }

        public override void Execute(CommandBuffer cmd, RenderTargetHandle renderTarget, TerrainBrushVolume volume) {
            cmd.DrawMesh(renderMesh, transform.localToWorldMatrix, renderMaterial, 0, 0);
        }

        public override void OnDrawGizmos() {
            if (renderMesh == null || renderMaterial == null) {
                return;
            }
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = renderMaterial.color;
            Gizmos.DrawWireMesh(renderMesh, 0, Vector3.zero, Quaternion.identity, Vector3.one);
        }
    }
}