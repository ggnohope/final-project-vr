using System;
using System.Collections;
using UnityEngine;
using Gsplat;

namespace Core
{
    public class GsplatSceneLoader : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GsplatRenderer gsplatRenderer;
        [SerializeField] private Camera mainCamera;

        [Header("Performance")]
        [SerializeField] private bool forceGarbageCollection = true;
        [SerializeField] private int gcFrameDelay = 2;

        public bool IsLoading { get; private set; }
        public string CurrentSceneId { get; private set; }

        private GsplatAsset currentAsset;
        private Coroutine loadingCoroutine;

        public event Action<string> OnSceneLoadStarted;
        public event Action<string, float> OnSceneLoadProgress;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<string> OnSceneUnloaded;

        private void Awake()
        {
            if (gsplatRenderer == null)
            {
                gsplatRenderer = GetComponent<GsplatRenderer>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        public void LoadScene(string regionId, string plyPath, CameraConfig cameraConfig)
        {
            if (IsLoading)
            {
                return;
            }

            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
            }

            loadingCoroutine = StartCoroutine(LoadSceneCoroutine(regionId, plyPath, cameraConfig));
        }

        private IEnumerator LoadSceneCoroutine(string regionId, string plyPath, CameraConfig cameraConfig)
        {
            IsLoading = true;
            OnSceneLoadStarted?.Invoke(regionId);

            yield return StartCoroutine(UnloadCurrentSceneCoroutine());

            OnSceneLoadProgress?.Invoke(regionId, 0.3f);

            yield return StartCoroutine(LoadPlyAssetCoroutine(plyPath));

            OnSceneLoadProgress?.Invoke(regionId, 0.7f);

            ApplyCameraConfiguration(cameraConfig);

            OnSceneLoadProgress?.Invoke(regionId, 0.9f);

            yield return new WaitForEndOfFrame();

            CurrentSceneId = regionId;
            IsLoading = false;

            OnSceneLoadProgress?.Invoke(regionId, 1.0f);
            OnSceneLoadCompleted?.Invoke(regionId);
        }

        private IEnumerator UnloadCurrentSceneCoroutine()
        {
            if (currentAsset != null)
            {
                string previousSceneId = CurrentSceneId;
                
                if (gsplatRenderer != null && gsplatRenderer.GsplatAsset != null)
                {
                    gsplatRenderer.GsplatAsset = null;
                }

                yield return new WaitForEndOfFrame();

                Resources.UnloadAsset(currentAsset);
                currentAsset = null;

                if (forceGarbageCollection)
                {
                    for (int i = 0; i < gcFrameDelay; i++)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                    
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                OnSceneUnloaded?.Invoke(previousSceneId);
            }
        }

        private IEnumerator LoadPlyAssetCoroutine(string plyPath)
        {
            ResourceRequest request = Resources.LoadAsync<GsplatAsset>(plyPath);
            
            while (!request.isDone)
            {
                yield return null;
            }

            if (request.asset == null)
            {
                yield break;
            }

            currentAsset = request.asset as GsplatAsset;

            if (gsplatRenderer != null)
            {
                gsplatRenderer.GsplatAsset = currentAsset;
            }

            yield return new WaitForEndOfFrame();
        }

        private void ApplyCameraConfiguration(CameraConfig config)
        {
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.transform.position = config.position;
            mainCamera.transform.rotation = config.rotation;
            
            if (config.fieldOfView > 0)
            {
                mainCamera.fieldOfView = config.fieldOfView;
            }
        }

        public void UnloadCurrentScene()
        {
            if (!IsLoading)
            {
                StartCoroutine(UnloadCurrentSceneCoroutine());
            }
        }

        private void OnDestroy()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
            }
        }
    }
}
