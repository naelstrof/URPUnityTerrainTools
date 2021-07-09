using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TerrainBrush {
    public class PathNode : MonoBehaviour {
        #if UNITY_EDITOR
        private static string gizmoName = "ico_pathnode.png";
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
        public void OnDrawGizmos() {
            Gizmos.DrawIcon(transform.position, gizmoPath, true);
        }
        #endif
    }
}
