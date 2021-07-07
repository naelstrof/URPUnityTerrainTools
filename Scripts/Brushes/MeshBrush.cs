using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainBrush {
    public class MeshBrush : MonoBehaviour {
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
            TerrainBrushOverseer.instance.Bake();
        }
        public void OnDrawGizmos() {
            if (transform.hasChanged) {
                TerrainBrushOverseer.instance.Bake();
                transform.hasChanged = false;
            }
            Gizmos.DrawIcon(transform.position, "ico_meshbrush.png", true);
        }
        public void OnDisable() {
            TerrainBrushOverseer.instance.Bake();
        }
    }
}