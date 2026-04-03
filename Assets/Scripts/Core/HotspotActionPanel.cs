using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Core
{
    /// <summary>
    /// A small panel with two action buttons shown when hovering a map hotspot:
    ///   - "FlyCam"   → opens the FlyCamVideoPanel with the region's video
    ///   - "Load Map" → loads the Gaussian Splatting scene (existing flow)
    ///
    /// Assign this to a UI panel GameObject that is a child of the WorldMapCanvas.
    /// </summary>
    public class HotspotActionPanel : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Buttons")]
        [SerializeField] private Button flyCamButton;
        [SerializeField] private Button loadMapButton;
        [SerializeField] private TMP_Text regionLabelText;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeSpeed = 10f;

        [Header("Auto-hide delay (seconds after pointer leaves panel)")]
        [SerializeField] private float autoHideDelay = 1.2f;

        private FlyCamVideoPanel videoPanel;
        private MapHotspotNavigator navigator;
        private MapRegion? currentRegion;
        private float targetAlpha = 0f;
        private bool pointerOverPanel = false;
        private float hideTimer = 0f;
        private bool pendingHide = false;

        private Canvas rootCanvas;
        private RectTransform canvasRect;
        private RectTransform panelRect;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            panelRect = GetComponent<RectTransform>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (flyCamButton != null)
                flyCamButton.onClick.AddListener(OnFlyCamClicked);
            else
                Debug.LogError("[HotspotActionPanel] flyCamButton is not assigned in the Inspector.", this);

            if (loadMapButton != null)
                loadMapButton.onClick.AddListener(OnLoadMapClicked);
            else
                Debug.LogError("[HotspotActionPanel] loadMapButton is not assigned in the Inspector.", this);

            Canvas parent = GetComponentInParent<Canvas>(true);
            if (parent != null)
            {
                rootCanvas = parent.rootCanvas;
                canvasRect = rootCanvas.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogError("[HotspotActionPanel] No parent Canvas found — this panel must be a child of a Canvas.", this);
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (canvasGroup == null) return;

            // Countdown auto-hide when panel is not hovered and hotspot lost
            if (pendingHide && !pointerOverPanel)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                {
                    pendingHide = false;
                    Hide();
                }
            }

            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

            bool interactive = canvasGroup.alpha > 0.1f;
            canvasGroup.interactable = interactive;
            canvasGroup.blocksRaycasts = interactive;
        }

        /// <summary>Injects dependencies from the navigator.</summary>
        public void Initialize(MapHotspotNavigator nav, FlyCamVideoPanel vidPanel)
        {
            navigator = nav;
            videoPanel = vidPanel;

            if (rootCanvas == null)
            {
                Canvas parent = GetComponentInParent<Canvas>(true);
                if (parent != null)
                {
                    rootCanvas = parent.rootCanvas;
                    canvasRect = rootCanvas.GetComponent<RectTransform>();
                }
            }
        }

        /// <summary>Shows the panel anchored near the given hotspot world position.</summary>
        public void ShowForRegion(MapRegion region, Vector3 hotspotWorldPosition)
        {
            currentRegion = region;
            pendingHide = false;

            if (regionLabelText != null)
                regionLabelText.text = region.displayName;

            gameObject.SetActive(true);

            PositionNearHotspot(hotspotWorldPosition);
            targetAlpha = 1f;
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Called when the hotspot is no longer hovered.
        /// Starts auto-hide timer so the user can still reach the buttons.
        /// </summary>
        public void StartAutoHide()
        {
            if (!gameObject.activeSelf) return;
            pendingHide = true;
            hideTimer = autoHideDelay;
        }

        /// <summary>Hides the panel immediately (e.g. after button click).</summary>
        public void Hide()
        {
            pendingHide = false;
            targetAlpha = 0f;
            Invoke(nameof(Deactivate), 0.25f);
        }

        private void Deactivate()
        {
            if (targetAlpha > 0f) return;
            gameObject.SetActive(false);
        }

        public bool IsVisible => gameObject.activeSelf && targetAlpha > 0f;

        // Keep panel alive while cursor is on it
        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerOverPanel = true;
            pendingHide = false;
            targetAlpha = 1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOverPanel = false;
            StartAutoHide();
        }

        private void PositionNearHotspot(Vector3 worldPosition)
        {
            if (panelRect == null || rootCanvas == null || canvasRect == null)
            {
                Debug.LogError("[HotspotActionPanel] Cannot position panel — missing panelRect, rootCanvas, or canvasRect.", this);
                return;
            }

            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);

            bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, cam, out Vector2 localPoint);

            if (!converted) return;

            panelRect.anchoredPosition = localPoint + new Vector2(20f, 20f);
        }

        private void OnFlyCamClicked()
        {
            if (videoPanel == null || !currentRegion.HasValue) return;

            string videoPath = currentRegion.Value.videoResourcePath;
            if (string.IsNullOrEmpty(videoPath))
                return;

            videoPanel.Show(videoPath, currentRegion.Value.displayName);
            Hide();
        }

        private void OnLoadMapClicked()
        {
            if (navigator == null || !currentRegion.HasValue) return;

            Hide();
            navigator.LoadRegionDirectly(currentRegion.Value);
        }
    }
}
