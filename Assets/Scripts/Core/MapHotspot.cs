using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    public class MapHotspot : MonoBehaviour
    {
        [Header("Region Data")]
        [SerializeField] private string regionId;
        [SerializeField] private int hotspotIndex;

        [Header("Visual Components")]
        [SerializeField] private Image highlightImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Highlight Settings")]
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private Color activeColor = new Color(1f, 0.9f, 0.3f, 0.9f);
        [SerializeField] private float transitionDuration = 0.2f;

        [Header("Scale Pulse")]
        [SerializeField] private bool enableScalePulse = true;
        [SerializeField] private float pulseScale = 1.15f;
        [SerializeField] private float pulseSpeed = 2f;

        private RectTransform rectTransform;
        private Vector3 originalScale;
        private bool isActive = false;
        private Coroutine pulseCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            SetIdleState();
        }

        public void SetActive(bool active, bool immediate = false)
        {
            if (isActive == active && !immediate) return;

            isActive = active;

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            if (immediate)
            {
                ApplyStateImmediate();
            }
            else
            {
                StartCoroutine(TransitionToState());
            }
        }

        private void ApplyStateImmediate()
        {
            if (isActive)
            {
                if (highlightImage != null)
                {
                    highlightImage.color = activeColor;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                if (enableScalePulse)
                {
                    pulseCoroutine = StartCoroutine(PulseAnimation());
                }
            }
            else
            {
                SetIdleState();
            }
        }

        private IEnumerator TransitionToState()
        {
            float elapsed = 0f;
            Color startColor = highlightImage != null ? highlightImage.color : idleColor;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
            Vector3 startScale = rectTransform.localScale;

            Color targetColor = isActive ? activeColor : idleColor;
            float targetAlpha = isActive ? 1f : 0.3f;
            Vector3 targetScale = originalScale;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;

                if (highlightImage != null)
                {
                    highlightImage.color = Color.Lerp(startColor, targetColor, t);
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                }

                rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }

            if (highlightImage != null)
            {
                highlightImage.color = targetColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }

            rectTransform.localScale = targetScale;

            if (isActive && enableScalePulse)
            {
                pulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }

        private IEnumerator PulseAnimation()
        {
            while (isActive)
            {
                float time = Time.time * pulseSpeed;
                float scale = 1f + Mathf.Sin(time) * (pulseScale - 1f) * 0.5f;
                rectTransform.localScale = originalScale * scale;

                yield return null;
            }

            rectTransform.localScale = originalScale;
        }

        private void SetIdleState()
        {
            if (highlightImage != null)
            {
                highlightImage.color = idleColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.3f;
            }

            rectTransform.localScale = originalScale;
        }

        public void Initialize(string id, int index)
        {
            regionId = id;
            hotspotIndex = index;
        }

        public string RegionId => regionId;
        public int Index => hotspotIndex;
        public bool IsActive => isActive;
    }
}
