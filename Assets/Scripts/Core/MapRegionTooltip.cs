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
        [SerializeField] private Vector2 offset = new Vector2(0f, 100f);
        [SerializeField] private float scaleSpeed = 10f;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);

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

        public void Show(string regionName, Vector3 worldPosition)
        {
            Show(regionName, null, worldPosition);
        }

        public void Show(string regionName, string description, Vector3 worldPosition)
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

            UpdatePosition(worldPosition);
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

        private void UpdatePosition(Vector3 worldPosition)
        {
            if (tooltipRect != null)
            {
                tooltipRect.position = worldPosition + (Vector3)offset;
            }
        }

        public void SetInstruction(string instruction)
        {
            if (instructionText != null)
            {
                instructionText.text = instruction;
            }
        }
    }
}
