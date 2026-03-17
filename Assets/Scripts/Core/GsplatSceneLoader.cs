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
            if (gsplatRenderer != null && gsplatRenderer.GsplatAsset != null)
            {
                gsplatRenderer.GsplatAsset = null;
            }

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

            else
            {
                yield return new WaitForEndOfFrame();
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

            CapSHBandsForPlatform(currentAsset);

            if (gsplatRenderer != null)
            {
                // Disable → assign asset → re-enable so that OnEnable() initializes
                // m_renderer with the asset already set. This prevents a race condition
                // where URP's render graph accesses SorterResource before Update() has
                // had a chance to create m_renderer, causing a NullReferenceException.
                gsplatRenderer.enabled = false;
                gsplatRenderer.GsplatAsset = currentAsset;
                gsplatRenderer.enabled = true;
            }

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Caps SHBands on the asset to the highest value whose GPU buffer fits within the platform's
        /// maximum GraphicsBuffer size. On Meta Quest (Adreno GPU) the limit is 128 MB.
        /// This modifies the runtime instance only — the serialized asset on disk is unaffected.
        /// </summary>
        private void CapSHBandsForPlatform(GsplatAsset asset)
        {
            if (asset == null || asset.SHBands == 0)
                return;

#if !UNITY_EDITOR
            // SH coefficient counts per band level (excluding DC term, stored as Vector3 per splat)
            // Band 1: 3 coeffs, Band 2: 8 coeffs, Band 3: 15 coeffs
            const long maxBufferBytes = 134_217_728L; // 128 MB — Adreno GPU limit on Meta Quest
            const int bytesPerCoefficient = 12;       // sizeof(Vector3)
            int[] coefficientsPerBand = { 0, 3, 8, 15 };

            byte originalBands = asset.SHBands;
            byte safeBands = 0;

            for (byte bands = asset.SHBands; bands > 0; bands--)
            {
                long shBufferSize = (long)coefficientsPerBand[bands] * bytesPerCoefficient * asset.SplatCount;
                if (shBufferSize <= maxBufferBytes)
                {
                    safeBands = bands;
                    break;
                }
            }

            if (safeBands < originalBands)
            {
                asset.SHBands = safeBands;
            }
#endif
        }

        private void ApplyCameraConfiguration(CameraConfig config)
        {
            if (mainCamera == null)
                return;

            Unity.XR.CoreUtils.XROrigin xrOrigin =
                FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();

            if (xrOrigin != null)
            {
                // config.position is the intended XROrigin floor position for this hotspot.
                // Setting it directly (rather than trying to offset by headset tracking height)
                // ensures the floor stays at the correct Y level regardless of the player's
                // real-world standing height — preventing the player from falling through the floor.
                xrOrigin.transform.position = config.position;

                // Apply yaw only — pitch/roll are owned by TrackedPoseDriver (headset orientation).
                float yaw = config.rotation.eulerAngles.y;
                xrOrigin.transform.eulerAngles = new Vector3(0f, yaw, 0f);
            }
            else
            {
                // Fallback for non-XR builds / Editor without XR simulator.
                mainCamera.transform.position = config.position;
                mainCamera.transform.rotation = config.rotation;
            }

            if (config.fieldOfView > 0)
                mainCamera.fieldOfView = config.fieldOfView;
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
