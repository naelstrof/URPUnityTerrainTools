using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TerrainBrush {
    public class PathBrush : Brush {
        [Range(0f,512f)]
        public float softRadius = 16f;
        [Range(0.5f,2f)]
        public float pointsPerMeter = 1f;
        [SerializeField][HideInInspector]
        private LineRenderer line;
        public bool closedLoop = false;
        public ComputeShader eikonalShader;
        public Material pathBlit;
        //public List<Transform> pathNodes = new List<Transform>();
        private List<JPBotelho.CatmullRom.CatmullFactors> factors;
        private float maxWidth {
            get {
                float maxWidth = 0f;
                foreach( var key in line.widthCurve.keys) {
                    maxWidth = Mathf.Max(key.value, maxWidth);
                }
                return maxWidth+softRadius*0.5f;
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
        public override void Execute(CommandBuffer cmd, RenderTargetHandle renderTarget, TerrainBrushVolume volume, Matrix4x4 view, Matrix4x4 projection) {
            OnValidate();
            // Now prepare a texture with our line rendered to it.
            RenderTargetHandle temporaryTextureA = new RenderTargetHandle();
            RenderTargetHandle temporaryTextureB = new RenderTargetHandle();
            temporaryTextureA.Init("_TerrainBrushSDFTemporaryTextureA");
            temporaryTextureB.Init("_TerrainBrushSDFTemporaryTextureB");
            cmd.GetTemporaryRT(temporaryTextureA.id, volume.texture.width, volume.texture.height, 0, FilterMode.Point, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, 1, true, RenderTextureMemoryless.None, false);
            cmd.GetTemporaryRT(temporaryTextureB.id, volume.texture.width, volume.texture.height, 0, FilterMode.Point, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, 1, true, RenderTextureMemoryless.None, false);
            cmd.SetRenderTarget(temporaryTextureA.id);
            cmd.ClearRenderTarget(true, true, Color.white);
            Mesh tempMesh = new Mesh();
            line.BakeMesh(tempMesh, true);
            cmd.DrawMesh(tempMesh, Matrix4x4.identity, line.sharedMaterials[0], 0, 0);

            int threadGroupsX = Mathf.FloorToInt(volume.texture.width / 8.0f);
            int threadGroupsY = Mathf.FloorToInt(volume.texture.height / 8.0f);
            for(int i=0;i<256;i++) {
                cmd.SetComputeTextureParam(eikonalShader, 0, "Source", temporaryTextureA.id);
                cmd.SetComputeTextureParam(eikonalShader, 0, "Result", temporaryTextureB.id);
                cmd.DispatchCompute(eikonalShader, 0, threadGroupsX, threadGroupsY, 1);

                cmd.SetComputeTextureParam(eikonalShader, 0, "Source", temporaryTextureB.id);
                cmd.SetComputeTextureParam(eikonalShader, 0, "Result", temporaryTextureA.id);
                cmd.DispatchCompute(eikonalShader, 0, threadGroupsX, threadGroupsY, 1);
            }
            pathBlit.SetFloat("_Radius", softRadius);
            cmd.Blit(temporaryTextureA.id, renderTarget.id, pathBlit);
            cmd.ReleaseTemporaryRT(temporaryTextureA.id);
            cmd.ReleaseTemporaryRT(temporaryTextureB.id);
            cmd.SetRenderTarget(renderTarget.id);

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
            int pointAdjustment = closedLoop ? 0 : 1;
            for(int i=0;i<pathNodes.Count-pointAdjustment;i++) {
                float len = Vector3.Distance(pathNodes[i%pathNodes.Count].position, pathNodes[(i+1)%pathNodes.Count].position);
                for (float t=0;t<1f;t+=pointsPerMeter/len) {
                    totalPoints++;
                }
            }
            line.positionCount = totalPoints;
            // Now calculate them for real
            int currentPoint = 0;
            for(int i=0;i<pathNodes.Count-pointAdjustment;i++) {
                float len = Vector3.Distance(pathNodes[i%pathNodes.Count].position, pathNodes[(i+1)%pathNodes.Count].position);
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
                //float mw = maxWidth;
                //Gizmos.DrawWireSphere(t.position, mw*0.5f);
            }
            //Gizmos.DrawWireCube(brushBounds.center, brushBounds.size);
        }
    }
}
