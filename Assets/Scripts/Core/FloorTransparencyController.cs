using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Disables floor renderers while a Gsplat scene is active so the floor mesh
    /// does not occlude the point-cloud ground. Colliders are always preserved.
    /// </summary>
    public class FloorTransparencyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GsplatSceneLoader sceneLoader;
        [SerializeField] private GameObject floorRoot;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.4f;

        private Renderer[] floorRenderers = System.Array.Empty<Renderer>();
        private Coroutine currentFade;

        private void Awake()
        {
            if (sceneLoader == null)
                sceneLoader = FindFirstObjectByType<GsplatSceneLoader>();

            if (floorRoot != null)
                floorRenderers = floorRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        private void OnEnable()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadStarted += OnSceneLoadStarted;
                sceneLoader.OnSceneUnloaded    += OnSceneUnloaded;
            }
            else
            {
                Debug.LogWarning("[FloorTransparencyController] sceneLoader is not assigned — floor fade events will not fire.", this);
            }
        }

        private void OnDisable()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadStarted -= OnSceneLoadStarted;
                sceneLoader.OnSceneUnloaded    -= OnSceneUnloaded;
            }
        }

        private void OnSceneLoadStarted(string regionId)
        {
            RestartFade(fadeIn: false);
        }

        private void OnSceneUnloaded(string regionId)
        {
            // IsLoading == true means we're switching between gsplat scenes — keep floor hidden.
            // IsLoading == false means full unload with no replacement — restore floor.
            if (sceneLoader != null && sceneLoader.IsLoading)
                return;

            RestartFade(fadeIn: true);
        }

        private void RestartFade(bool fadeIn)
        {
            if (currentFade != null)
                StopCoroutine(currentFade);

            currentFade = StartCoroutine(FadeRenderers(fadeIn));
        }

        private IEnumerator FadeRenderers(bool fadeIn)
        {
            if (floorRenderers.Length == 0)
            {
                Debug.LogWarning("[FloorTransparencyController] No renderers found on floorRoot — check assignment in Inspector.", this);
                yield break;
            }

            if (fadeIn)
            {
                // Enable all renderers first, then let them fade in via alpha on their instance materials.
                SetRenderersEnabled(true);
            }

            float elapsed = 0f;
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha   = fadeIn ? 1f : 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                SetFloorAlpha(Mathf.Lerp(startAlpha, endAlpha, t));
                yield return null;
            }

            SetFloorAlpha(endAlpha);

            if (!fadeIn)
            {
                SetRenderersEnabled(false);
            }
        }

        private void SetRenderersEnabled(bool enabled)
        {
            foreach (Renderer rend in floorRenderers)
            {
                if (rend != null)
                    rend.enabled = enabled;
            }
        }

        /// <summary>Sets alpha on all instance materials of the floor renderers.</summary>
        private void SetFloorAlpha(float alpha)
        {
            foreach (Renderer rend in floorRenderers)
            {
                if (rend == null || !rend.enabled) continue;

                foreach (Material mat in rend.materials)
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }
        }
    }
}
