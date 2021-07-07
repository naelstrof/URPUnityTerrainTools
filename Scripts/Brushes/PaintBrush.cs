using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TerrainBrush {
    // Made to be inherited.
    public class PaintBrush : Brush {
        public Material renderMaterial;
        public Color color = Color.white;
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

        public override void Execute(CommandBuffer cmd, RenderTargetHandle renderTarget, TerrainBrushVolume volume, Matrix4x4 view, Matrix4x4 projection) {
            Material tempMaterial = Material.Instantiate(renderMaterial);
            tempMaterial.SetColor("_Color", color);
            cmd.DrawMesh(renderMesh, transform.localToWorldMatrix, tempMaterial, 0, 0);
        }

        public override void OnDrawGizmos() {
            base.OnDrawGizmos();
            Gizmos.DrawIcon(transform.position, "ico_brush.png", true);
        }
        public void OnValidate() {
            if (renderMesh == null || renderMaterial == null) {
                return;
            }
            TerrainBrushOverseer.instance.Bake();
        }
        public void OnDrawGizmosSelected() {
            if (renderMesh == null || renderMaterial == null) {
                return;
            }
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = renderMaterial.color;
            Gizmos.DrawWireMesh(renderMesh, 0, Vector3.zero, Quaternion.identity, Vector3.one);
        }
    }
}