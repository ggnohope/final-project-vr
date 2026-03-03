using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    [RequireComponent(typeof(RawImage))]
    public class WorldMapController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SceneMapData sceneMapData;

        [Header("References")]
        [SerializeField] private GsplatSceneLoader sceneLoader;
        [SerializeField] private Canvas worldMapCanvas;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip regionClickSound;

        [Header("Multi-click Prevention")]
        [SerializeField] private float clickCooldown = 0.5f;

        private RawImage mapImage;
        private RectTransform rectTransform;
        private bool isProcessingClick = false;
        private float lastClickTime = 0f;

        private void Awake()
        {
            mapImage = GetComponent<RawImage>();
            rectTransform = GetComponent<RectTransform>();

            if (mapImage == null)
            {
                return;
            }

            mapImage.enabled = true;

            if (sceneMapData != null && sceneMapData.worldMapTexture != null)
            {
                mapImage.texture = sceneMapData.worldMapTexture;
            }
        }

        private void Start()
        {
            if (sceneLoader == null)
            {
                sceneLoader = FindFirstObjectByType<GsplatSceneLoader>();
            }
        }

        /// <summary>Loads the given region with click feedback. Called by MapHotspotNavigator on confirm.</summary>
        public void LoadRegion(MapRegion region)
        {
            if (isProcessingClick || Time.time - lastClickTime < clickCooldown)
            {
                return;
            }

            if (sceneLoader == null || sceneLoader.IsLoading)
            {
                return;
            }

            lastClickTime = Time.time;
            StartCoroutine(OnRegionClickedWithFeedback(region));
        }

        private IEnumerator OnRegionClickedWithFeedback(MapRegion region)
        {
            isProcessingClick = true;

            StartCoroutine(ClickPulseAnimation());
            PlaySound(regionClickSound);

            yield return new WaitForSeconds(0.3f);

            OnRegionClicked(region);

            isProcessingClick = false;
        }

        private IEnumerator ClickPulseAnimation()
        {
            const float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
                rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            rectTransform.localScale = Vector3.one;
        }

        private void OnRegionClicked(MapRegion region)
        {
            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(false);
            }

            // Disable UI Ray now that the map is closed
            if (ItemBarController.Instance != null)
            {
                ItemBarController.Instance.DisableUIRay();
            }

            sceneLoader.LoadScene(region.regionId, region.plyAssetPath, region.cameraConfig);
        }

        /// <summary>Makes the world map canvas visible.</summary>
        public void ShowWorldMap()
        {
            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(true);
            }
        }

        /// <summary>Hides the world map canvas.</summary>
        public void HideWorldMap()
        {
            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(false);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
