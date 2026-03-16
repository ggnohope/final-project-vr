using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core
{
    /// <summary>
    /// Represents a single map hotspot on the world map canvas.
    /// Interaction model:
    ///   - UI Ray hover (PointerEnter/Exit) → highlight on/off
    ///   - UI Ray click (trigger press while hovering) → select and load region
    /// </summary>
    public class MapHotspot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

        /// <summary>Fired when the pointer enters this hotspot. Arg: hotspot index.</summary>
        public event Action<int> OnHoverEnter;

        /// <summary>Fired when the pointer exits this hotspot. Arg: hotspot index.</summary>
        public event Action<int> OnHoverExit;

        private RectTransform rectTransform;
        private Vector3 originalScale;
        private bool isActive = false;
        private Coroutine pulseCoroutine;
        private MapHotspotNavigator navigator;

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

        /// <summary>Assigns the navigator so clicking this hotspot directly selects and confirms it.</summary>
        public void SetNavigator(MapHotspotNavigator nav)
        {
            navigator = nav;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetActive(true);
            OnHoverEnter?.Invoke(hotspotIndex);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetActive(false);
            OnHoverExit?.Invoke(hotspotIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (navigator == null) return;
            navigator.SelectAndConfirmHotspot(hotspotIndex);
        }

        public string RegionId => regionId;
        public int Index => hotspotIndex;
        public bool IsActive => isActive;
    }
}
