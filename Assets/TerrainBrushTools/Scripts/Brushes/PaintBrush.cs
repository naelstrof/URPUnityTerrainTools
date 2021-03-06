using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TerrainBrush {
    // Made to be inherited.
    [ExecuteInEditMode]
    public class PaintBrush : Brush {
        private static string gizmoName = "ico_brush.png";
        private string internalGizmoPath = "";

        private string gizmoPath { 
            get {
                #if UNITY_EDITOR
                if (string.IsNullOrEmpty(internalGizmoPath)) {
                    var assembly = Assembly.GetExecutingAssembly();
                    var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
                    if (packageInfo == null) {
                        internalGizmoPath = "Assets/TerrainBrushTools/Gizmos/"+gizmoName;
                    } else {
                        internalGizmoPath = packageInfo.assetPath+"/Gizmos/"+gizmoName;
                    }
                }
                #endif
                return internalGizmoPath;
            }
        }
        public Material renderMaterial;
        public Color color = Color.white;
        public Mesh renderMesh;
        public override Bounds brushBounds {
            get {
                if ( renderMesh == null ){
                    return new Bounds();
                }
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
            tempMaterial.SetColor("_BaseColor", color);
            cmd.DrawMesh(renderMesh, transform.localToWorldMatrix, tempMaterial, 0, 0);
        }

        public void OnDrawGizmos() {
            Gizmos.DrawIcon(transform.position, gizmoPath, true);
        }
        public override void OnDrawGizmosSelected() {
            if (renderMesh == null || renderMaterial == null) {
                return;
            }
            base.OnDrawGizmosSelected();
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = renderMaterial.color;
            Gizmos.DrawWireMesh(renderMesh, 0, Vector3.zero, Quaternion.identity, Vector3.one);
        }
    }
}