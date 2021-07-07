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
        // This probably shouldn't happen without the user knowing.
            /*int mask = 1;
            for (int i=0;i<31;i++) {
                if ((mask & TerrainBrushOverseer.instance.meshBrushTargetLayers) != 0) {
                    gameObject.layer = i;
                    break;
                }
                mask <<= 1;
            }*/
            TerrainBrushOverseer.instance?.Bake();
        }
        public void OnDrawGizmos() {
            if (transform.hasChanged) {
                TerrainBrushOverseer.instance?.Bake();
                transform.hasChanged = false;
            }
            Gizmos.DrawIcon(transform.position, gizmoPath, true);
        }
        public void OnDisable() {
            TerrainBrushOverseer.instance?.Bake();
        }
        #endif
    }
}