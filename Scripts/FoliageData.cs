using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TerrainBrush {
    #if UNITY_EDITOR
    [CustomEditor(typeof(FoliageData))]
    public class FoliageDataEditor : Editor {
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) {
            FoliageData data = (FoliageData)target;
            PreviewRenderUtility previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.camera.backgroundColor = Color.clear;
            foreach(var light in previewRenderUtility.lights) {
                light.intensity = 10f;
            }

            previewRenderUtility.camera.farClipPlane = 30f;
            previewRenderUtility.camera.transform.position = -Vector3.forward*10f;
            previewRenderUtility.camera.transform.rotation = Quaternion.identity;
            previewRenderUtility.BeginPreview(new Rect(0,0,width*0.5f,height), GUIStyle.none);
            Matrix4x4 trs = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(-90f,Vector3.right), Vector3.one);
            previewRenderUtility.DrawMesh(data.foliageMesh, trs, data.foliageMaterial, 0);
            previewRenderUtility.camera.Render();
            RenderTexture finish = (RenderTexture)previewRenderUtility.EndPreview();
            RenderTexture.active = finish;
            Texture2D tex = new Texture2D(width,height);
            tex.ReadPixels(new Rect(0,0, width, height),0,0);
            tex.Apply();
            RenderTexture.active = null;
            previewRenderUtility.Cleanup();
            return tex;
        }
    }
    [CustomPreview(typeof(FoliageData))]
    public class FoliageDataPreview : ObjectPreview {
        private Vector2 _drag;
        private FoliageData _targetFoliage;
        public override bool HasPreviewGUI() {
            return true;
        }
        public override void OnPreviewSettings() {
            if (GUILayout.Button("Reset Camera", EditorStyles.whiteMiniLabel)) {
                _drag = Vector2.zero;
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background) {
            _drag = Drag2D(_drag, r);
            _targetFoliage = target as FoliageData;
            PreviewRenderUtility previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.camera.farClipPlane = 30f;
            previewRenderUtility.camera.transform.position = -Vector3.forward*16f;
            previewRenderUtility.camera.transform.rotation = Quaternion.identity;
            foreach(var light in previewRenderUtility.lights) {
                light.intensity *= 10f;
            }
            //Only render our 3D 'preview' when the UI is 'repainting'.
            //The OnPreviewGUI, like other GUI methods, will be called LOTS
            //of times ever frame to handle different events.
            //We only need to Render our preview once when the GUI is being repainted!
            if (Event.current.type == EventType.Repaint)
            {
                //Tell the PRU to prepair itself - we pass along the
                //rect of the preview area so the PRU knows what size 
                //of a preview to render.
                previewRenderUtility.BeginPreview(r, background);

                //We draw our mesh manually - it is not attached to any 'gameobject' in the preview 'scene'.
                //The preview 'scene' only contains a camera and a light. We need to render things manually.
                //We pass along the mesh set on the mesh filter and the material set on the renderer
                Matrix4x4 trs = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(_drag.x, Vector3.up)*Quaternion.AngleAxis(-90f+_drag.y,Vector3.right), Vector3.one);
                previewRenderUtility.DrawMesh(_targetFoliage.foliageMesh, trs, _targetFoliage.foliageMaterial, 0);
                
                //Tell the camera to actually render the preview.
                previewRenderUtility.camera.Render();

                //Now that we are done, we can end the preview. This method will spit out a Texture
                //The texture contains the image that was rendered by the preview utillity camera :)
                Texture resultRender = previewRenderUtility.EndPreview();
                
                //If we omit the line bellow, then you wouldnt actually see anything in the preview!
                //The preview image is generated, but that was all done in our 'virtual' PreviewRenderUtility 'scene'.
                //We still need to draw something in the PreviewGUI area..!
                
                //So we draw the image that was generated into the preview GUI area, filling the entire area with this image.
                GUI.DrawTexture(r, resultRender, ScaleMode.StretchToFill, false);
            }
            previewRenderUtility.Cleanup();
        }
        public static Vector2 Drag2D(Vector2 scrollPosition, Rect position) {
            int controlID = GUIUtility.GetControlID("Slider".GetHashCode(), FocusType.Passive);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && position.width > 50f) {
                        GUIUtility.hotControl = controlID;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID) {
                        GUIUtility.hotControl = 0;
                    }
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID) {
                        scrollPosition -= current.delta * (float)((!current.shift) ? 1 : 3) / Mathf.Min(position.width, position.height) * 140f;
                        scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
                        current.Use();
                        GUI.changed = true;
                    }
                    break;
            }
            return scrollPosition;
        }
    }
    #endif
    [CreateAssetMenu(menuName = "Terrain Brush Tools/Foliage Data")]
    public class FoliageData : ScriptableObject {
        #if UNITY_EDITOR
        [System.Flags]
        public enum FoliageAspect {
            Grass = (1 << 0),
            Spiller = (1 << 1),
            Filler = (1 << 2),
            Thriller = (1 << 3),
        }
        [EnumFlagsAttribute]
        public FoliageAspect aspectFlags;
        public bool HasAspect(FoliageAspect aspectFlags) {
            if ((((int)aspectFlags) & ((int)this.aspectFlags)) != 0) {
                return true;
            }
            return false;
        }
        public Mesh foliageMesh;
        public Material foliageMaterial;
        #endif
    }
    #if UNITY_EDITOR
    public class EnumFlagsAttribute : PropertyAttribute {
        public EnumFlagsAttribute() { }
    }

    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    public class EnumFlagsAttributeDrawer : PropertyDrawer {
        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
            _property.intValue = EditorGUI.MaskField( _position, _label, _property.intValue, _property.enumNames );
        }
    }
    #endif

}