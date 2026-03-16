using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Core
{
    public class MapRegionTooltip : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text regionNameText;
        [SerializeField] private TMP_Text regionDescriptionText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform tooltipRect;

        [Header("Animation")]
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private Vector2 offset = new Vector2(0f, 60f);
        [SerializeField] private float scaleSpeed = 10f;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        private Canvas parentCanvas;
        private RectTransform canvasRect;
        private bool isVisible = false;
        private float targetAlpha = 0f;
        private Vector3 targetScale = Vector3.one;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }

            if (tooltipRect != null)
            {
                tooltipRect.localScale = Vector3.zero;
            }

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                // Always get the root canvas for coordinate conversion
                parentCanvas = parentCanvas.rootCanvas;
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }

            if (tooltipRect != null)
            {
                tooltipRect.localScale = Vector3.Lerp(tooltipRect.localScale, targetScale, Time.deltaTime * scaleSpeed);
            }
        }

        /// <summary>Shows the tooltip at the given hotspot world position with only a name label.</summary>
        public void Show(string regionName, Vector3 hotspotWorldPosition)
        {
            Show(regionName, null, hotspotWorldPosition);
        }

        /// <summary>Shows the tooltip at the given hotspot world position with name and optional description.</summary>
        public void Show(string regionName, string description, Vector3 hotspotWorldPosition)
        {
            if (!isVisible)
            {
                gameObject.SetActive(true);
                isVisible = true;
            }

            if (regionNameText != null)
            {
                regionNameText.text = regionName;
            }

            if (regionDescriptionText != null)
            {
                if (!string.IsNullOrEmpty(description))
                {
                    regionDescriptionText.text = description;
                    regionDescriptionText.gameObject.SetActive(true);
                }
                else
                {
                    regionDescriptionText.gameObject.SetActive(false);
                }
            }

            if (instructionText != null)
            {
                instructionText.text = "Press Trigger to Load";
            }

            UpdatePosition(hotspotWorldPosition);
            targetAlpha = 1f;
            targetScale = Vector3.one;
        }

        public void Hide()
        {
            targetAlpha = 0f;
            targetScale = Vector3.zero;
            isVisible = false;
            Invoke(nameof(DeactivateTooltip), 0.2f);
        }

        private void DeactivateTooltip()
        {
            if (!isVisible)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Converts hotspot world position to canvas-local anchoredPosition and applies the offset.
        /// Handles World Space, Screen Space Camera, and Screen Space Overlay canvases correctly.
        /// </summary>
        private void UpdatePosition(Vector3 hotspotWorldPosition)
        {
            if (tooltipRect == null || parentCanvas == null || canvasRect == null)
            {
                Debug.LogWarning($"[MapRegionTooltip] UpdatePosition skipped — tooltipRect={tooltipRect}, parentCanvas={parentCanvas}, canvasRect={canvasRect}");
                return;
            }

            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, hotspotWorldPosition);

            bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, cam, out Vector2 localPoint);

            if (!converted)
            {
                Debug.LogWarning($"[MapRegionTooltip] ScreenPointToLocalPointInRectangle failed. hotspotWorldPos={hotspotWorldPosition}, screenPoint={screenPoint}");
                return;
            }

            Debug.Log($"[MapRegionTooltip] hotspotWorldPos={hotspotWorldPosition} | screenPoint={screenPoint} | canvasLocalPoint={localPoint} | finalAnchoredPos={localPoint + offset}");

            tooltipRect.anchoredPosition = localPoint + offset;
        }

        /// <summary>Overrides the instruction label text.</summary>
        public void SetInstruction(string instruction)
        {
            if (instructionText != null)
            {
                instructionText.text = instruction;
            }
        }
    }
}
