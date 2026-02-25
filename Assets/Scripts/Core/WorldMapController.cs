using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Core
{
    [RequireComponent(typeof(RawImage))]
    public class WorldMapController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Data")]
        [SerializeField] private SceneMapData sceneMapData;

        [Header("References")]
        [SerializeField] private GsplatSceneLoader sceneLoader;
        [SerializeField] private Canvas worldMapCanvas;

        [Header("Visual Feedback")]
        [SerializeField] private Image regionHighlightOverlay;
        [SerializeField] private Color hoverTint = new Color(1f, 1f, 0.5f, 1f);
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float hoverTransitionSpeed = 8f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip regionHoverSound;
        [SerializeField] private AudioClip regionClickSound;

        [Header("Tooltip")]
        [SerializeField] private MapRegionTooltip tooltip;

        [Header("Multi-click Prevention")]
        [SerializeField] private float clickCooldown = 0.5f;

        private RawImage mapImage;
        private RectTransform rectTransform;
        private MapRegion? hoveredRegion;
        private bool isProcessingClick = false;
        private float lastClickTime = 0f;
        private Color originalColor;
        private Color targetColor;
        private Vector3 targetScale;

        private void Awake()
        {
            mapImage = GetComponent<RawImage>();
            rectTransform = GetComponent<RectTransform>();

            if (mapImage != null)
            {
                mapImage.enabled = true;
                
                if (sceneMapData != null && sceneMapData.worldMapTexture != null)
                {
                    mapImage.texture = sceneMapData.worldMapTexture;
                }

                originalColor = mapImage.color;
                targetColor = originalColor;
            }
            else
            {
                Debug.LogError("[WorldMapController] RawImage component not found!");
            }

            if (rectTransform != null)
            {
                targetScale = Vector3.one;
            }

            if (regionHighlightOverlay != null)
            {
                regionHighlightOverlay.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (sceneLoader == null)
            {
                sceneLoader = FindFirstObjectByType<GsplatSceneLoader>();
            }
        }

        private void Update()
        {
            UpdateSmoothTransitions();
        }

        private void UpdateSmoothTransitions()
        {
            if (mapImage != null)
            {
                mapImage.color = Color.Lerp(mapImage.color, targetColor, Time.deltaTime * hoverTransitionSpeed);
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * hoverTransitionSpeed);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isProcessingClick || Time.time - lastClickTime < clickCooldown)
            {
                return;
            }

            if (sceneMapData == null || sceneLoader == null || sceneLoader.IsLoading)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                return;
            }

            Vector2 normalizedPosition = GetNormalizedPosition(localPoint);
            MapRegion? region = sceneMapData.GetRegionByPosition(normalizedPosition);

            if (region.HasValue)
            {
                lastClickTime = Time.time;
                StartCoroutine(OnRegionClickedWithFeedback(region.Value));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (sceneLoader != null && sceneLoader.IsLoading) return;

            UpdateHoverState(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearHoverState();
            
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }

        private void UpdateHoverState(PointerEventData eventData)
        {
            if (sceneMapData == null) return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                ClearHoverState();
                return;
            }

            Vector2 normalizedPosition = GetNormalizedPosition(localPoint);
            MapRegion? region = sceneMapData.GetRegionByPosition(normalizedPosition);

            if (region.HasValue && hoveredRegion?.regionId != region.Value.regionId)
            {
                hoveredRegion = region.Value;
                ApplyHoverEffect();
                PlaySound(regionHoverSound);
            }
            else if (!region.HasValue && hoveredRegion.HasValue)
            {
                ClearHoverState();
            }
        }

        private void ApplyHoverEffect()
        {
            targetColor = hoverTint;
            targetScale = Vector3.one * hoverScale;

            if (tooltip != null && hoveredRegion.HasValue)
            {
                tooltip.Show(hoveredRegion.Value.displayName, Input.mousePosition);
            }
        }

        private void ClearHoverState()
        {
            if (hoveredRegion.HasValue)
            {
                hoveredRegion = null;
                targetColor = originalColor;
                targetScale = Vector3.one;
            }
        }

        private System.Collections.IEnumerator OnRegionClickedWithFeedback(MapRegion region)
        {
            isProcessingClick = true;

            StartCoroutine(ClickPulseAnimation());
            PlaySound(regionClickSound);

            yield return new WaitForSeconds(0.3f);

            OnRegionClicked(region);

            isProcessingClick = false;
        }

        private System.Collections.IEnumerator ClickPulseAnimation()
        {
            float duration = 0.2f;
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
            PlaySound(regionClickSound);

            Debug.Log($"[WorldMapController] Loading region: {region.displayName} ({region.regionId})");

            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(false);
            }

            sceneLoader.LoadScene(region.regionId, region.plyAssetPath, region.cameraConfig);
        }

        private Vector2 GetNormalizedPosition(Vector2 localPoint)
        {
            Rect rect = rectTransform.rect;
            Vector2 normalizedPosition = new Vector2(
                (localPoint.x - rect.xMin) / rect.width,
                (localPoint.y - rect.yMin) / rect.height
            );

            return normalizedPosition;
        }

        public void ShowWorldMap()
        {
            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(true);
            }
        }

        public void HideWorldMap()
        {
            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(false);
            }
        }

        public void LoadRegion(MapRegion region)
        {
            if (sceneLoader != null && !sceneLoader.IsLoading)
            {
                StartCoroutine(OnRegionClickedWithFeedback(region));
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public MapRegion? GetRegionAtScreenPosition(Vector2 screenPosition, Camera eventCamera)
        {
            if (sceneMapData == null)
            {
                return null;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPosition, eventCamera, out localPoint))
            {
                return null;
            }

            Vector2 normalizedPosition = GetNormalizedPosition(localPoint);
            return sceneMapData.GetRegionByPosition(normalizedPosition);
        }
    }
}
