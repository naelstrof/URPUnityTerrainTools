using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TerrainBrush {
    public class PathBrush : Brush {
        float pointsPerMeter = 1f;
        [SerializeField][HideInInspector]
        private LineRenderer line;
        public bool closedLoop = false;
        //public List<Transform> pathNodes = new List<Transform>();
        private List<JPBotelho.CatmullRom.CatmullFactors> factors;
        private float maxWidth {
            get {
                float maxWidth = 0f;
                foreach( var key in line.widthCurve.keys) {
                    maxWidth = Mathf.Max(key.value, maxWidth);
                }
                return maxWidth;
            }
        }
        public override Bounds brushBounds {
            get {
                if (transform.childCount <= 0) {
                    return new Bounds();
                }
                List<Transform> pathNodes = new List<Transform>();
                for(int i=0;i<transform.childCount;i++) {
                    pathNodes.Add(transform.GetChild(i));
                }
                Bounds b = new Bounds(transform.GetChild(0).position, Vector3.zero);
                for(int i=0;i<pathNodes.Count-1;i++) {
                    float len = Vector3.Distance(pathNodes[i].position, pathNodes[i+1].position);
                    for (float t=0;t<1f;t+=pointsPerMeter/len) {
                        Vector3 point = JPBotelho.CatmullRom.CalculatePosition(factors[i].p1, factors[i].p2, factors[i].p3, factors[i].p4, t);
                        b.Encapsulate(point);
                        b.Encapsulate(point+Vector3.one*maxWidth);
                        b.Encapsulate(point-Vector3.one*maxWidth);
                    }
                }
                return b;
            }
        }
        public override void Execute(CommandBuffer cmd, RenderTargetHandle renderTarget, TerrainBrushVolume volume) {
            OnValidate();
            Mesh tempMesh = new Mesh();
            line.BakeMesh(tempMesh, true);
            cmd.DrawMesh(tempMesh,Matrix4x4.identity,line.sharedMaterials[0], 0, 0);
            //cmd.DrawRenderer(GetComponent<LineRenderer>(), GetComponent<LineRenderer>().sharedMaterial);
        }

        public void OnValidate() {
            if (line == null) {
                GameObject gameObjectLine = new GameObject("PathLineRenderer", new System.Type[]{typeof(LineRenderer)});
                gameObjectLine.transform.parent = transform;
                line = gameObjectLine.GetComponent<LineRenderer>();
            }
            line.alignment = LineAlignment.TransformZ;
            line.transform.rotation = Quaternion.FromToRotation(line.transform.forward, Vector3.up)*line.transform.rotation;
            List<Transform> pathNodes = new List<Transform>();
            for(int i=0;i<transform.childCount;i++) {
                pathNodes.Add(transform.GetChild(i));
            }
            if (pathNodes.Count <= 2) { return; }
            factors = JPBotelho.CatmullRom.GenerateCatmullFactors(pathNodes,closedLoop);
            // Precalculate the needed points
            int totalPoints = 0;
            for(int i=0;i<pathNodes.Count-1;i++) {
                float len = Vector3.Distance(pathNodes[i].position, pathNodes[i+1].position);
                for (float t=0;t<1f;t+=pointsPerMeter/len) {
                    totalPoints++;
                }
            }
            line.positionCount = totalPoints;
            // Now calculate them for real
            int currentPoint = 0;
            for(int i=0;i<pathNodes.Count-1;i++) {
                float len = Vector3.Distance(pathNodes[i].position, pathNodes[i+1].position);
                for (float t=0;t<1f;t+=pointsPerMeter/len) {
                    line.SetPosition(currentPoint++, JPBotelho.CatmullRom.CalculatePosition(factors[i].p1, factors[i].p2, factors[i].p3, factors[i].p4, t));
                }
            }

        }
        public override void OnDrawGizmos() {
            if (transform.hasChanged) {
                OnValidate();
                transform.hasChanged = false;
            }
            for(int i=0;i<transform.childCount;i++) {
                Transform t = transform.GetChild(i);
                if (t.hasChanged) {
                    OnValidate();
                    t.hasChanged = false;
                }
                float mw = maxWidth;
                Gizmos.DrawWireSphere(t.position, mw);
            }
            //Gizmos.DrawWireCube(brushBounds.center, brushBounds.size);
        }
    }
}
