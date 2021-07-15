using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TerrainBrush {
    [ExecuteInEditMode]
    public class MeshBrush : MonoBehaviour {
        #if UNITY_EDITOR
        private static string gizmoName = "ico_meshbrush.png";
        private string internalGizmoPath = "";
        private string gizmoPath { 
            get {
                if (string.IsNullOrEmpty(internalGizmoPath)) {
                    var assembly = Assembly.GetExecutingAssembly();
                    var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
                    if (packageInfo == null) {
                        internalGizmoPath = "Assets/TerrainBrushTools/Gizmos/"+gizmoName;
                    } else {
                        internalGizmoPath = packageInfo.assetPath+"/Gizmos/"+gizmoName;
                    }
                }
                return internalGizmoPath;
            }
        }
        public void OnEnable() {
            transform.hasChanged = false;
        }
        public void Start() {
            transform.hasChanged = false;
        }
        public void OnDrawGizmos() {
            Gizmos.DrawIcon(transform.position, gizmoPath, true);
        }
        public void OnDrawGizmosSelected() {
            if (transform.hasChanged) {
                TerrainBrushOverseer.instance?.Bake();
                transform.hasChanged=false;
            }
        }
        #endif
    }
}