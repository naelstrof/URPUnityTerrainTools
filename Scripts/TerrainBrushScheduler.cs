using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace TerrainBrush {
    [ExecuteInEditMode]
    public class TerrainBrushScheduler : MonoBehaviour {
        // FIXME: These should be driven with constructors, but I'm lazy and also I suck so instead I used the new syntax sugar to make them that I learned about.
        public class TerrainBrushSchedule {
            public virtual bool isFinished {
                get {
                    return currentChunk >= chunkCountSquared*chunkCountSquared;
                }
            }
            public int currentChunk=0;
            public int chunkCountSquared;
            public List<TerrainWrap> terrainWraps;
            public virtual void IncrementalWork() { }
            public delegate void ScheduleFinishedDelegate();
            public ScheduleFinishedDelegate OnFinish; 
        }
        public class TerrainBrushMeshSchedule : TerrainBrushSchedule {
            public Bounds encapsulatedBounds;
            public int resolutionPow;
            public float smoothness;
            public Material terrainMaterial;
            public Transform parent;
            public GameObject terrainWrapPrefab;
            public Matrix4x4 worldToTexture;
            public LayerMask collidersMask;
            public override void IncrementalWork() {
                int numChunks = chunkCountSquared*chunkCountSquared;
                if (terrainWraps != null && currentChunk < numChunks) {
                    if (terrainWraps.Count<=currentChunk) {
                        GameObject newTerrainWrapObject = GameObject.Instantiate(terrainWrapPrefab, Vector3.zero, Quaternion.identity);
                        newTerrainWrapObject.transform.parent = parent;
                        terrainWraps.Add(newTerrainWrapObject.GetComponent<TerrainWrap>());
                    }
                    terrainWraps[currentChunk].GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
                    terrainWraps[currentChunk].Generate(currentChunk, worldToTexture, collidersMask, encapsulatedBounds, 1<<resolutionPow, chunkCountSquared, smoothness);
                }
                currentChunk++;
            }
        }
        public class TerrainBrushFoliageSchedule : TerrainBrushSchedule {
            public float density;
            public int recurseCount;
            public Matrix4x4 worldToTexture;
            public Texture2D maskTexture;
            public FoliageData[] foliageDatas;
            public float foliagePerlinScale;
            public bool saveInScene;
            public override bool isFinished {
                get {
                    return currentChunk >= terrainWraps.Count;
                }
            }
            public override void IncrementalWork() {
                if (terrainWraps != null) {
                    terrainWraps[currentChunk]?.GenerateFoliage(saveInScene, foliagePerlinScale, density, recurseCount, foliageDatas, worldToTexture, maskTexture);
                }
                currentChunk++;
            }
        }

        private List<TerrainBrushSchedule> schedules = new List<TerrainBrushSchedule>();

        public static TerrainBrushScheduler _instance;
        public static TerrainBrushScheduler instance {
            get {
                if (!SceneManager.GetActiveScene().IsValid() || !SceneManager.GetActiveScene().isLoaded) {
                    _instance = null;
                    return null;
                }
                if (_instance == null){
                    _instance = UnityEngine.Object.FindObjectOfType<TerrainBrushScheduler>();
                }
                if (_instance == null) {
                    GameObject gameObject = new GameObject("TerrainBrushScheduler", new System.Type[]{typeof(TerrainBrushScheduler)});
                    gameObject.hideFlags = HideFlags.HideAndDontSave;
                    _instance = gameObject.GetComponent<TerrainBrushScheduler>();
                }
                _instance.hideFlags = HideFlags.HideAndDontSave;
                return _instance;
            }
        }
        public void OnEnable() {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            #if UNITY_EDITOR
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= ClearSchedules;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += ClearSchedules;
            #endif
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ClearSchedules;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ClearSchedules;
        }
        private void ClearSchedules(Scene start, Scene end) {
            schedules.Clear();
        }
        public void Start() {
            #if UNITY_EDITOR
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= ClearSchedules;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += ClearSchedules;
            #endif
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ClearSchedules;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ClearSchedules;
        }
        public void Update() {
            for(int i=0;i<schedules.Count;i++){
                TerrainBrushSchedule schedule = schedules[i];
                schedule.IncrementalWork();
                // If we finished our work
                if (schedule.isFinished) {
                    schedule.OnFinish?.Invoke();
                    schedules.Remove(schedule);
                }
            }
        }
        public TerrainBrushFoliageSchedule GenerateFoliage(bool saveInScene, int seed, float foliagePerlinScale, float density, int recurseCount, Matrix4x4 worldToTexture, Texture maskTexture, FoliageData[] foliageDatas) {
            for(int i=0;i<schedules.Count;i++){
                if (schedules[i] is TerrainBrushFoliageSchedule) {
                    schedules.Remove(schedules[i]);
                    i = Mathf.Max(i--,0);
                }
            }
            UnityEngine.Random.InitState(seed);
            // We only support render textures or texture2D's
            Assert.IsTrue(maskTexture is RenderTexture || maskTexture is Texture2D);

            // Find all our existing terrain wraps, we'll call GenerateFoliage on them all over a couple of frames.
            List<TerrainWrap> terrainWraps = new List<TerrainWrap>(UnityEngine.Object.FindObjectsOfType<TerrainWrap>());

            // We cache all the data we're going to use to generate, so that things stay consistent.
            int chunkCountSquared = Mathf.RoundToInt(Mathf.Sqrt(terrainWraps.Count));
            TerrainBrushFoliageSchedule schedule = new TerrainBrushFoliageSchedule() {
                chunkCountSquared = chunkCountSquared,
                terrainWraps = terrainWraps,
                density = density,
                foliagePerlinScale = foliagePerlinScale,
                recurseCount = recurseCount,
                worldToTexture = worldToTexture,
                foliageDatas = foliageDatas,
                saveInScene = saveInScene,
            };
            // We automatically handle being given a RenderTexture or a Texture2D. We need it to be accessible from the CPU.
            if (maskTexture is RenderTexture) {
                RenderTexture renderTexture = (maskTexture as RenderTexture);
                Texture2D cpuMaskCopy = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, 0, true);
                RenderTexture old = RenderTexture.active;
                RenderTexture.active = renderTexture;
                cpuMaskCopy.ReadPixels(new Rect(0,0,renderTexture.width, renderTexture.height), 0, 0);
                cpuMaskCopy.Apply();
                RenderTexture.active = old;
                schedule.maskTexture = cpuMaskCopy;
            } else if (maskTexture is Texture2D) {
                schedule.maskTexture = (maskTexture as Texture2D);
            }

            schedules.Add(schedule);
            return schedule;
        }
        public TerrainBrushMeshSchedule GenerateMesh(Transform parent, Matrix4x4 worldToTexture, LayerMask colliderMask, Material terrainMaterial, GameObject terrainWrapPrefab, int chunkCountSquared, int resolutionPow, float smoothness) {
            for(int i=0;i<schedules.Count;i++){
                if (schedules[i] is TerrainBrushMeshSchedule) {
                    schedules.Remove(schedules[i]);
                    i = Mathf.Max(i--,0);
                }
            }
            Assert.IsTrue(terrainWrapPrefab != null);
            Assert.IsTrue(terrainMaterial != null);

            // Figure out where the mesh will go.
            Collider[] colliders = UnityEngine.Object.FindObjectsOfType<Collider>();
            if (colliders.Length <= 0) {
                Debug.LogWarning("No colliders found, nothing will generate.");
                return null;
            }
            Bounds encapsulatedBounds = new Bounds(colliders[0].bounds.center, colliders[0].bounds.size);
            foreach(Collider c in colliders) {
                if (((1<<c.gameObject.layer) & colliderMask) != 0) {
                    encapsulatedBounds.EncapsulateTransformedBounds(c.bounds);
                }
            }

            // Now gather all our existing terrainWraps for generation.
            List<TerrainWrap> terrainWraps = new List<TerrainWrap>(UnityEngine.Object.FindObjectsOfType<TerrainWrap>());
            terrainWraps.Sort((a,b)=>(a.chunkID.CompareTo(b.chunkID)));

            TerrainBrushMeshSchedule schedule = new TerrainBrushMeshSchedule() {
                chunkCountSquared = chunkCountSquared,
                resolutionPow = resolutionPow,
                smoothness = smoothness,
                collidersMask = colliderMask,
                worldToTexture = worldToTexture,
                terrainWrapPrefab = terrainWrapPrefab,
                encapsulatedBounds = encapsulatedBounds,
                terrainWraps = terrainWraps,
                terrainMaterial = terrainMaterial,
                parent = parent,
            };
            // Cache the data so we can do the work over some frames.
            schedules.Add(schedule);
            return schedule;
        }
    }
}
