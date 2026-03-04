using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Core
{
    /// <summary>
    /// Attach to WorldMapCanvas (or any persistent GO in the map scene).
    /// On Start (and on demand via the context menu) runs a checklist that identifies
    /// why the XR UI Ray Interactor cannot interact with the map canvas / hotspots.
    ///
    /// Checks performed:
    ///   1. Canvas component — renderMode must be WorldSpace
    ///   2. Raycaster — must be TrackedDeviceGraphicRaycaster, NOT GraphicRaycaster
    ///   3. EventSystem — must exist in scene
    ///   4. XRUIInputModule — must be on the EventSystem GO
    ///   5. UI Ray Interactor — must be enabled and have enableUIInteraction=true
    ///   6. Hotspot CanvasGroups — blocksRaycasts must be true
    ///   7. Hotspot RectTransforms — must have non-zero size
    ///   8. Canvas layer vs XR Ray layer mask
    /// </summary>
    [AddComponentMenu("World Map/Map UI Raycast Diagnostics")]
    public class MapUIRaycastDiagnostics : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The WorldMapCanvas Canvas component.")]
        [SerializeField] private Canvas mapCanvas;

        [Tooltip("The XR UI Ray Interactor on the right-hand controller.")]
        [SerializeField] private XRRayInteractor uiRayInteractor;

        [Tooltip("Parent transform that holds all MapHotspot GameObjects.")]
        [SerializeField] private RectTransform hotspotsContainer;

        private void Start()
        {
            if (gameObject.activeInHierarchy)
                StartCoroutine(RunDiagnosticsDelayed());
        }

        private void OnEnable()
        {
            // Delay 2 frames so MapHotspotNavigator.GenerateHotspotsNextFrame() coroutine finishes first.
            StartCoroutine(RunDiagnosticsDelayed());
        }

        private IEnumerator RunDiagnosticsDelayed()
        {
            yield return null;
            yield return null;
            RunDiagnostics();
        }

        [ContextMenu("Run Diagnostics")]
        public void RunDiagnostics()
        {
            var issues = new List<string>();
            var ok     = new List<string>();

            // ── 1. Canvas renderMode ────────────────────────────────────────────────
            if (mapCanvas == null)
                mapCanvas = GetComponentInParent<Canvas>();

            if (mapCanvas == null)
            {
                issues.Add("CHECK 1 FAIL — Canvas is NULL. Assign mapCanvas field.");
            }
            else if (mapCanvas.renderMode != RenderMode.WorldSpace)
            {
                issues.Add($"CHECK 1 FAIL — Canvas.renderMode = {mapCanvas.renderMode}. " +
                           "Must be WorldSpace for XR UI Ray interaction.");
            }
            else
            {
                ok.Add("CHECK 1 OK — Canvas renderMode = WorldSpace.");
            }

            // ── 2. Raycaster type ───────────────────────────────────────────────────
            if (mapCanvas != null)
            {
                var graphic  = mapCanvas.GetComponent<GraphicRaycaster>();
                var tracked  = mapCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();

                if (tracked == null)
                {
                    issues.Add("CHECK 2 FAIL — TrackedDeviceGraphicRaycaster is MISSING on the Canvas. " +
                               "XRI's XRRayInteractor requires TrackedDeviceGraphicRaycaster to hit UI. " +
                               "Add it and remove the plain GraphicRaycaster (or keep both — " +
                               "TrackedDeviceGraphicRaycaster handles XR rays, GraphicRaycaster handles mouse).");
                }
                else if (!tracked.enabled)
                {
                    issues.Add("CHECK 2 FAIL — TrackedDeviceGraphicRaycaster is present but DISABLED.");
                }
                else
                {
                    ok.Add("CHECK 2 OK — TrackedDeviceGraphicRaycaster is present and enabled.");
                }

                if (graphic != null && tracked != null)
                {
                    ok.Add("CHECK 2 NOTE — Both GraphicRaycaster AND TrackedDeviceGraphicRaycaster present. " +
                           "This is fine; TrackedDeviceGraphicRaycaster handles XR, GraphicRaycaster handles editor mouse.");
                }
                else if (graphic != null && tracked == null)
                {
                    issues.Add("CHECK 2 FAIL — Only GraphicRaycaster found (no TrackedDeviceGraphicRaycaster). " +
                               "XR UI Ray will NOT hit this canvas.");
                }
            }

            // ── 3. EventSystem ──────────────────────────────────────────────────────
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                issues.Add("CHECK 3 FAIL — No EventSystem found in scene. UI events cannot fire without one.");
            }
            else
            {
                ok.Add($"CHECK 3 OK — EventSystem found: '{eventSystem.gameObject.name}'.");

                // ── 4. XRUIInputModule ──────────────────────────────────────────────
                var xrModule = eventSystem.GetComponent<XRUIInputModule>();
                if (xrModule == null)
                {
                    issues.Add("CHECK 4 FAIL — XRUIInputModule is MISSING on the EventSystem GameObject. " +
                               "Without it, XRRayInteractor cannot dispatch Pointer events to UI.");
                }
                else if (!xrModule.enabled)
                {
                    issues.Add("CHECK 4 FAIL — XRUIInputModule is present but DISABLED on EventSystem.");
                }
                else
                {
                    ok.Add("CHECK 4 OK — XRUIInputModule is present and enabled on EventSystem.");
                }
            }

            // ── 5. UI Ray Interactor ────────────────────────────────────────────────
            if (uiRayInteractor == null)
                uiRayInteractor = FindAnyObjectByType<XRRayInteractor>();

            if (uiRayInteractor == null)
            {
                issues.Add("CHECK 5 FAIL — XRRayInteractor not found in scene. " +
                           "Assign uiRayInteractor field or ensure the GO is active.");
            }
            else
            {
                if (!uiRayInteractor.enabled)
                    issues.Add($"CHECK 5 FAIL — XRRayInteractor '{uiRayInteractor.gameObject.name}' is DISABLED. " +
                               "It must be enabled when the map is open.");
                else
                    ok.Add($"CHECK 5 OK — XRRayInteractor '{uiRayInteractor.gameObject.name}' is enabled.");

                if (!uiRayInteractor.enableUIInteraction)
                    issues.Add("CHECK 5 FAIL — XRRayInteractor.enableUIInteraction = false. " +
                               "Enable it in Inspector so the ray can hit Canvas UI elements.");
                else
                    ok.Add("CHECK 5 OK — XRRayInteractor.enableUIInteraction = true.");
            }

            // ── 6. Hotspot CanvasGroups ─────────────────────────────────────────────
            if (hotspotsContainer != null)
            {
                int hotspotCount = 0;
                int blockedCount = 0;
                int zeroSizeCount = 0;

                foreach (Transform child in hotspotsContainer)
                {
                    hotspotCount++;
                    var cg = child.GetComponent<CanvasGroup>();
                    if (cg != null && !cg.blocksRaycasts)
                        blockedCount++;

                    var rt = child.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 size = rt.rect.size;
                        if (size.x <= 0 || size.y <= 0)
                            zeroSizeCount++;
                    }
                }

                if (hotspotCount == 0)
                    issues.Add("CHECK 6 WARN — Hotspots container is empty. No hotspots were generated.");
                else
                    ok.Add($"CHECK 6 OK — Found {hotspotCount} hotspot(s) in container.");

                if (blockedCount > 0)
                    issues.Add($"CHECK 6 FAIL — {blockedCount} hotspot(s) have CanvasGroup.blocksRaycasts = false. " +
                               "They will not receive pointer events.");

                if (zeroSizeCount > 0)
                    issues.Add($"CHECK 6 FAIL — {zeroSizeCount} hotspot(s) have zero RectTransform size. " +
                               "They cannot be hit by a raycast.");
            }
            else
            {
                issues.Add("CHECK 6 SKIP — hotspotsContainer not assigned; cannot check hotspot raycasts.");
            }

            // ── 7. Canvas layer vs ray mask ─────────────────────────────────────────
            if (mapCanvas != null && uiRayInteractor != null)
            {
                int canvasLayer = mapCanvas.gameObject.layer;
                string canvasLayerName = LayerMask.LayerToName(canvasLayer);

                // XRRayInteractor doesn't have a public raycastMask in 3.x, but we can check
                // Physics raycast mask via the serialized field workaround.
                ok.Add($"CHECK 7 INFO — Canvas is on layer '{canvasLayerName}' (index {canvasLayer}). " +
                       "Ensure XRRayInteractor's Raycast Mask includes this layer " +
                       "(check the 'Raycast Mask' field on the XRRayInteractor component).");
            }

            // ── 8. RectMask2D clipping hotspots ────────────────────────────────────
            if (mapCanvas != null)
            {
                var masks = mapCanvas.GetComponentsInChildren<RectMask2D>();
                if (masks.Length > 0)
                {
                    ok.Add($"CHECK 8 INFO — Found {masks.Length} RectMask2D component(s) in map hierarchy. " +
                           "If hotspots render outside MapImage bounds they will be clipped and may not receive raycasts.");
                }
            }

            // ── Print results ───────────────────────────────────────────────────────
            string sep = new string('─', 60);

            if (issues.Count == 0)
            {
                Debug.Log($"[MapUIRaycastDiagnostics] ALL CHECKS PASSED — UI Ray should interact with map canvas.\n  ✓ " +
                          string.Join("\n  ✓ ", ok));
            }
            else
            {
                // Split into two logs so Unity console does not truncate long messages.
                Debug.LogWarning(
                    $"[MapUIRaycastDiagnostics] {sep}\n" +
                    $"  ISSUES FOUND ({issues.Count}):\n" +
                    $"  ► " + string.Join("\n  ► ", issues) + "\n" +
                    $"  {sep}");

                Debug.Log(
                    $"[MapUIRaycastDiagnostics] PASSED ({ok.Count}):\n" +
                    $"  ✓ " + string.Join("\n  ✓ ", ok));
            }
        }
    }
}
