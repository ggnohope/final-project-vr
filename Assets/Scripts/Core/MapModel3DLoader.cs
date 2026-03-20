using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Listens to GsplatSceneLoader events and places the GLB 3D models defined in
    /// each MapRegion's models list at their configured world-space positions with a uniform scale of 5.
    /// </summary>
    public class MapModel3DLoader : MonoBehaviour
    {
        private const string ResourcesBasePath = "3DModels/";
        private const float DefaultModelScale = 5f;

        [Header("References")]
        [SerializeField] private GsplatSceneLoader sceneLoader;
        [SerializeField] private SceneMapData sceneMapData;

        private readonly List<GameObject> activeModelRoots = new();

        // ─────────────────────────────────────────────────────────────
        #region Unity

        private void Awake()
        {
            if (sceneLoader == null)
                sceneLoader = FindFirstObjectByType<GsplatSceneLoader>();
        }

        private void OnEnable()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadStarted += HandleSceneLoadStarted;
                sceneLoader.OnSceneLoadCompleted += HandleSceneLoadCompleted;
                sceneLoader.OnSceneUnloaded += HandleSceneUnloaded;
            }
        }

        private void OnDisable()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadStarted -= HandleSceneLoadStarted;
                sceneLoader.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
                sceneLoader.OnSceneUnloaded -= HandleSceneUnloaded;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Event Handlers

        private void HandleSceneLoadStarted(string regionId)
        {
            UnloadAllModels();
        }

        private void HandleSceneLoadCompleted(string regionId)
        {
            if (sceneMapData == null)
            {
                Debug.LogWarning("[MapModel3DLoader] SceneMapData is not assigned.");
                return;
            }

            MapRegion? region = sceneMapData.GetRegionById(regionId);
            if (region == null)
            {
                Debug.LogWarning($"[MapModel3DLoader] No MapRegion found for regionId '{regionId}'.");
                return;
            }

            RegionModel3D[] models = region.Value.models;
            if (models == null || models.Length == 0)
            {
                Debug.Log($"[MapModel3DLoader] Region '{regionId}' has no models defined — skipping 3D model load.");
                return;
            }

            _ = LoadModelsForRegionAsync(regionId, models);
        }

        private void HandleSceneUnloaded(string regionId)
        {
            UnloadAllModels();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Private

        private async Task LoadModelsForRegionAsync(string regionId, RegionModel3D[] models)
        {
            Debug.Log($"[MapModel3DLoader] Loading {models.Length} model(s) for region '{regionId}'...");

            foreach (RegionModel3D model in models)
            {
                await LoadSingleModelAsync(model);
            }

            Debug.Log($"[MapModel3DLoader] Finished loading models for region '{regionId}'. Active models: {activeModelRoots.Count}");
        }

        private async Task LoadSingleModelAsync(RegionModel3D model)
        {
            string resourcePath = ResourcesBasePath + model.modelName;
            TextAsset glbAsset = Resources.Load<TextAsset>(resourcePath);

            if (glbAsset == null)
            {
                Debug.LogError($"[MapModel3DLoader] TextAsset not found at Resources/'{resourcePath}'. " +
                               $"Ensure '{model.modelName}.bytes' exists in Assets/Resources/3DModels/.");
                return;
            }

            GltfImport gltfImport = new GltfImport();
            bool parseSuccess = await gltfImport.LoadGltfBinary(glbAsset.bytes);

            if (!parseSuccess)
            {
                Debug.LogError($"[MapModel3DLoader] glTFast failed to parse '{model.modelName}.bytes'.");
                gltfImport.Dispose();
                return;
            }

            GameObject modelRoot = new GameObject(model.modelName);
            modelRoot.transform.position = model.position;
            modelRoot.transform.rotation = Quaternion.identity;
            modelRoot.transform.localScale = Vector3.one * DefaultModelScale;

            bool instantiated = await gltfImport.InstantiateMainSceneAsync(modelRoot.transform);

            if (!instantiated)
            {
                Debug.LogError($"[MapModel3DLoader] glTFast parsed '{model.modelName}' but failed to instantiate the scene.");
                Destroy(modelRoot);
                gltfImport.Dispose();
                return;
            }

            activeModelRoots.Add(modelRoot);
            Debug.Log($"[MapModel3DLoader] Placed '{model.modelName}' at {model.position} with scale {DefaultModelScale}.");
        }

        /// <summary>Destroys all currently active 3D model GameObjects.</summary>
        private void UnloadAllModels()
        {
            foreach (GameObject root in activeModelRoots)
            {
                if (root != null)
                    Destroy(root);
            }

            activeModelRoots.Clear();
            Debug.Log("[MapModel3DLoader] Unloaded all active 3D models.");
        }

        #endregion
    }
}
