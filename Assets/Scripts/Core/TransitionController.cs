using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Core
{
    public class TransitionController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private Image fadeImage;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private Image loadingProgressBar;
        [SerializeField] private Image loadingSpinner;

        [Header("Animation Settings")]
        [SerializeField] private float spinnerRotationSpeed = 180f;
        [SerializeField] private Color fadeColor = Color.black;

        [Header("Scene Loader")]
        [SerializeField] private GsplatSceneLoader sceneLoader;
        [SerializeField] private SceneMapData sceneMapData;

        private Coroutine currentTransition;
        private bool isTransitioning;

        private void Awake()
        {
            if (fadeImage != null)
            {
                fadeImage.color = fadeColor;
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0;
                fadeCanvasGroup.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadStarted += OnSceneLoadStarted;
                sceneLoader.OnSceneLoadProgress += OnSceneLoadProgress;
                sceneLoader.OnSceneLoadCompleted += OnSceneLoadCompleted;
            }
        }

        private void OnDisable()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadStarted -= OnSceneLoadStarted;
                sceneLoader.OnSceneLoadProgress -= OnSceneLoadProgress;
                sceneLoader.OnSceneLoadCompleted -= OnSceneLoadCompleted;
            }
        }

        private void Update()
        {
            if (isTransitioning && loadingSpinner != null && loadingSpinner.gameObject.activeSelf)
            {
                loadingSpinner.transform.Rotate(0, 0, -spinnerRotationSpeed * Time.deltaTime);
            }
        }

        private void OnSceneLoadStarted(string regionId)
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }

            string regionName = GetRegionDisplayName(regionId);
            currentTransition = StartCoroutine(TransitionCoroutine(regionName));
        }

        private void OnSceneLoadProgress(string regionId, float progress)
        {
            if (loadingProgressBar != null)
            {
                loadingProgressBar.fillAmount = progress;
            }
        }

        private void OnSceneLoadCompleted(string regionId)
        {
        }

        private IEnumerator TransitionCoroutine(string regionName)
        {
            isTransitioning = true;

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(true);
            }

            if (loadingText != null && sceneMapData != null)
            {
                loadingText.text = string.Format(sceneMapData.loadingTextFormat, regionName);
            }

            if (loadingSpinner != null)
            {
                loadingSpinner.gameObject.SetActive(true);
            }

            if (loadingProgressBar != null)
            {
                loadingProgressBar.fillAmount = 0;
            }

            yield return FadeIn();

            while (sceneLoader != null && sceneLoader.IsLoading)
            {
                yield return null;
            }

            if (sceneMapData != null)
            {
                yield return new WaitForSeconds(sceneMapData.minimumLoadTime);
            }

            yield return FadeOut();

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(false);
            }

            if (loadingSpinner != null)
            {
                loadingSpinner.gameObject.SetActive(false);
            }

            isTransitioning = false;
            currentTransition = null;
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            float duration = sceneMapData != null ? sceneMapData.transitionFadeTime : 0.5f;
            AnimationCurve curve = sceneMapData != null ? sceneMapData.fadeCurve : AnimationCurve.Linear(0, 0, 1, 1);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = t;
                }

                yield return null;
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1;
            }
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float duration = sceneMapData != null ? sceneMapData.transitionFadeTime : 0.5f;
            AnimationCurve curve = sceneMapData != null ? sceneMapData.fadeCurve : AnimationCurve.Linear(0, 0, 1, 1);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - curve.Evaluate(elapsed / duration);
                
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = t;
                }

                yield return null;
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0;
            }
        }

        private string GetRegionDisplayName(string regionId)
        {
            if (sceneMapData != null)
            {
                MapRegion? region = sceneMapData.GetRegionById(regionId);
                if (region.HasValue)
                {
                    return region.Value.displayName;
                }
            }
            return regionId;
        }

        public void ShowTransition(string message)
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }

            if (loadingText != null)
            {
                loadingText.text = message;
            }

            currentTransition = StartCoroutine(ShowTransitionCoroutine());
        }

        private IEnumerator ShowTransitionCoroutine()
        {
            isTransitioning = true;

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(true);
            }

            yield return FadeIn();
        }

        public void HideTransition()
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }

            currentTransition = StartCoroutine(HideTransitionCoroutine());
        }

        private IEnumerator HideTransitionCoroutine()
        {
            yield return FadeOut();

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(false);
            }

            isTransitioning = false;
            currentTransition = null;
        }
    }
}
