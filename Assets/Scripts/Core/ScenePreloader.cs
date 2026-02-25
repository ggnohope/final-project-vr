using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gsplat;

namespace Core
{
    public class ScenePreloader : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SceneMapData sceneMapData;
        [SerializeField] private bool preloadOnStart = false;
        
        [Header("Performance")]
        [SerializeField] private float loadDelayBetweenAssets = 0.5f;

        private Dictionary<string, GsplatAsset> preloadedAssets = new Dictionary<string, GsplatAsset>();
        private Queue<string> preloadQueue = new Queue<string>();
        private bool isPreloading;

        private void Start()
        {
            if (preloadOnStart && sceneMapData != null)
            {
                StartCoroutine(PreloadAllRegions());
            }
        }

        public void PreloadRegion(string regionId)
        {
            if (sceneMapData == null) return;

            MapRegion? region = sceneMapData.GetRegionById(regionId);
            if (!region.HasValue) return;

            if (!preloadedAssets.ContainsKey(regionId))
            {
                preloadQueue.Enqueue(regionId);
                
                if (!isPreloading)
                {
                    StartCoroutine(ProcessPreloadQueue());
                }
            }
        }

        private IEnumerator PreloadAllRegions()
        {
            if (sceneMapData == null || sceneMapData.regions == null)
                yield break;

            foreach (var region in sceneMapData.regions)
            {
                PreloadRegion(region.regionId);
                yield return new WaitForSeconds(loadDelayBetweenAssets);
            }
        }

        private IEnumerator ProcessPreloadQueue()
        {
            isPreloading = true;

            while (preloadQueue.Count > 0)
            {
                string regionId = preloadQueue.Dequeue();
                MapRegion? region = sceneMapData.GetRegionById(regionId);
                
                if (region.HasValue && !preloadedAssets.ContainsKey(regionId))
                {
                    yield return LoadAssetAsync(regionId, region.Value.plyAssetPath);
                    yield return new WaitForSeconds(loadDelayBetweenAssets);
                }
            }

            isPreloading = false;
        }

        private IEnumerator LoadAssetAsync(string regionId, string plyPath)
        {
            ResourceRequest request = Resources.LoadAsync<GsplatAsset>(plyPath);
            
            while (!request.isDone)
            {
                yield return null;
            }

            if (request.asset != null)
            {
                GsplatAsset asset = request.asset as GsplatAsset;
                preloadedAssets[regionId] = asset;
                Debug.Log($"[ScenePreloader] Preloaded region: {regionId}");
            }
            else
            {
                Debug.LogError($"[ScenePreloader] Failed to preload asset at path: {plyPath}");
            }
        }

        public GsplatAsset GetPreloadedAsset(string regionId)
        {
            if (preloadedAssets.ContainsKey(regionId))
            {
                return preloadedAssets[regionId];
            }
            return null;
        }

        public void UnloadRegion(string regionId)
        {
            if (preloadedAssets.ContainsKey(regionId))
            {
                GsplatAsset asset = preloadedAssets[regionId];
                Resources.UnloadAsset(asset);
                preloadedAssets.Remove(regionId);
                Debug.Log($"[ScenePreloader] Unloaded region: {regionId}");
            }
        }

        public void UnloadAllRegions()
        {
            foreach (var kvp in preloadedAssets)
            {
                Resources.UnloadAsset(kvp.Value);
            }
            preloadedAssets.Clear();
            
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private void OnDestroy()
        {
            UnloadAllRegions();
        }
    }
}
