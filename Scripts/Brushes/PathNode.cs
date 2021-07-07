using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainBrush {
    public class PathNode : MonoBehaviour {
        public void OnDrawGizmos() {
            Gizmos.DrawIcon(transform.position, "ico_pathnode.png", true);
        }
    }
}
