using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>
    /// Manages world map hotspots and region-load confirmation.
    ///
    /// INTERACTION MODEL (UI Ray):
    ///   - Right controller UI Ray hover over a hotspot → hotspot highlights (via IPointerEnterHandler on MapHotspot)
    ///   - Right controller trigger click while hovering → SelectAndConfirmHotspot → region loads
    ///
    /// SETUP (Auto-generate mode):
    ///   1. Enable autoGenerateFromData
    ///   2. Assign hotspotPrefab, hotspotsContainer, sceneMapData, tileRenderer
    ///
    /// SETUP (Manual mode):
    ///   1. Disable autoGenerateFromData
    ///   2. Populate hotspots[] manually
    /// </summary>
    public class MapHotspotNavigator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldMapController worldMapController;
        [SerializeField] private SceneMapData sceneMapData;
        [SerializeField] private MapTileRenderer tileRenderer;

        [Header("Auto Generation")]
        [SerializeField] private bool autoGenerateFromData = false;
        [SerializeField] private GameObject hotspotPrefab;
        [SerializeField] private RectTransform hotspotsContainer;

        [Tooltip("Only used in manual mode (autoGenerateFromData = false).")]
        [SerializeField] private MapHotspot[] hotspots;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip navigationSound;
        [SerializeField] private AudioClip confirmSound;

        [Header("Tooltip (Optional)")]
        [SerializeField] private MapRegionTooltip tooltip;
        [SerializeField] private bool showTooltipOnSelection = true;

        [Header("Hotspot Action Panel (Optional)")]
        [SerializeField] private HotspotActionPanel actionPanel;
        [SerializeField] private FlyCamVideoPanel flyCamVideoPanel;

        private int currentHotspotIndex = -1;
        private Dictionary<string, int> regionToIndexMap;

        private void OnEnable()
        {
            if (tileRenderer != null)
                tileRenderer.OnViewChanged += OnMapViewChanged;
        }

        private void OnDisable()
        {
            if (tileRenderer != null)
                tileRenderer.OnViewChanged -= OnMapViewChanged;
        }

        private void Start()
        {
            // Fallback auto-find if Inspector references were lost after recompile
            if (actionPanel == null)
            {
                // HotspotActionPanel is a sibling under the same parent Canvas
                Transform parent = transform.parent != null ? transform.parent : transform;
                actionPanel = parent.GetComponentInChildren<HotspotActionPanel>(true);
            }

            if (flyCamVideoPanel == null)
                flyCamVideoPanel = FindFirstObjectByType<FlyCamVideoPanel>(FindObjectsInactive.Include);

            if (actionPanel != null)
                actionPanel.Initialize(this, flyCamVideoPanel);
            else
                Debug.LogError("[MapHotspotNavigator] HotspotActionPanel not found! Assign it in Inspector.");

            if (autoGenerateFromData)
            {
                StartCoroutine(GenerateHotspotsNextFrame());
            }
            else
            {
                InitializeHotspots();
            }
        }

        private IEnumerator GenerateHotspotsNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            GenerateHotspotsFromData();
            InitializeHotspots();
        }

        /// <summary>
        /// Destroys all existing generated hotspots and recreates them from SceneMapData.regions.
        /// </summary>
        public void GenerateHotspotsFromData()
        {
            if (sceneMapData == null || hotspotPrefab == null || hotspotsContainer == null)
            {
                return;
            }

            for (int i = hotspotsContainer.childCount - 1; i >= 0; i--)
                Destroy(hotspotsContainer.GetChild(i).gameObject);

            var generated = new List<MapHotspot>();

            foreach (var region in sceneMapData.regions)
            {
                GameObject instanceGO = Instantiate(hotspotPrefab, hotspotsContainer);
                instanceGO.name = $"Hotspot_{region.regionId}";

                MapHotspot instance = instanceGO.GetComponent<MapHotspot>()
                    ?? instanceGO.GetComponentInChildren<MapHotspot>();

                if (instance == null)
                {
                    Destroy(instanceGO);
                    continue;
                }

                instance.Initialize(region.regionId, generated.Count);
                instance.SetNavigator(this);

                Image highlightImage = instance.GetComponent<Image>();
                if (highlightImage != null)
                    highlightImage.color = region.regionHighlightColor;

                RectTransform rt = instance.GetComponent<RectTransform>();
                rt.anchoredPosition = tileRenderer != null
                    ? tileRenderer.LatLngToCanvasPosition(region.latLng.x, region.latLng.y)
                    : Vector2.zero;

                generated.Add(instance);
            }

            hotspots = generated.ToArray();
        }

        /// <summary>Subscribed to MapTileRenderer.OnViewChanged — repositions hotspots after pan/zoom.</summary>
        private void OnMapViewChanged(Vector2 centerLatLng, float zoom) => RepositionHotspots();

        /// <summary>Recalculates anchoredPosition for every hotspot based on the current tile renderer view.</summary>
        public void RepositionHotspots()
        {
            if (hotspots == null || tileRenderer == null || sceneMapData == null) return;

            for (int i = 0; i < hotspots.Length; i++)
            {
                if (hotspots[i] == null) continue;
                MapRegion? region = sceneMapData.GetRegionById(hotspots[i].RegionId);
                if (!region.HasValue) continue;

                RectTransform rt = hotspots[i].GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = tileRenderer.LatLngToCanvasPosition(region.Value.latLng.x, region.Value.latLng.y);
            }
        }

        private void InitializeHotspots()
        {
            if (hotspots == null || hotspots.Length == 0) return;

            regionToIndexMap = new Dictionary<string, int>();

            for (int i = 0; i < hotspots.Length; i++)
            {
                if (hotspots[i] == null) continue;
                hotspots[i].Initialize(hotspots[i].RegionId, i);
                hotspots[i].SetNavigator(this);
                hotspots[i].OnHoverEnter += OnHotspotHoverEnter;
                hotspots[i].OnHoverExit  += OnHotspotHoverExit;
                regionToIndexMap[hotspots[i].RegionId] = i;
            }
        }

        private void OnHotspotHoverEnter(int index)
        {
            if (hotspots == null || index < 0 || index >= hotspots.Length) return;

            MapRegion? region = sceneMapData != null
                ? sceneMapData.GetRegionById(hotspots[index].RegionId)
                : null;

            if (!region.HasValue)
            {
                Debug.LogWarning($"[MapHotspotNavigator] HoverEnter index={index} — region NOT found for id='{hotspots[index].RegionId}'");
                return;
            }

            Vector3 worldPos = hotspots[index].transform.position;
            Debug.Log($"[MapHotspotNavigator] HoverEnter '{region.Value.regionId}' worldPos={worldPos} | actionPanel={(actionPanel != null ? "OK" : "NULL")}");

            if (showTooltipOnSelection && tooltip != null)
                tooltip.Show(region.Value.displayName, worldPos);

            if (actionPanel != null)
                actionPanel.ShowForRegion(region.Value, worldPos);
            else
                Debug.LogError("[MapHotspotNavigator] actionPanel is NULL — check Inspector reference.");
        }

        private void OnHotspotHoverExit(int index)
        {
            if (tooltip != null)
                tooltip.Hide();

            // Start auto-hide timer so the user can still move cursor onto the buttons
            if (actionPanel != null)
                actionPanel.StartAutoHide();
        }

        /// <summary>
        /// Called by MapHotspot.OnPointerClick via UI Ray trigger press.
        /// Marks the clicked hotspot as current and immediately loads its region.
        /// </summary>
        public void SelectAndConfirmHotspot(int index)
        {
            if (hotspots == null || index < 0 || index >= hotspots.Length) return;

            currentHotspotIndex = index;
            PlaySound(confirmSound);

            MapRegion? region = sceneMapData != null
                ? sceneMapData.GetRegionById(hotspots[index].RegionId)
                : null;

            if (!region.HasValue)
            {
                return;
            }

            if (tooltip != null)
                tooltip.Hide();

            if (actionPanel != null)
                actionPanel.Hide();

            if (worldMapController != null)
                worldMapController.LoadRegion(region.Value);
        }

        /// <summary>Loads a region directly — used by HotspotActionPanel's Load Map button.</summary>
        public void LoadRegionDirectly(MapRegion region)
        {
            if (tooltip != null)
                tooltip.Hide();

            if (actionPanel != null)
                actionPanel.Hide();

            if (worldMapController != null)
                worldMapController.LoadRegion(region);
        }

        /// <summary>Immediately loads a region without hotspot click, used by external systems.</summary>
        public void ConfirmSelection()
        {
            if (hotspots == null || currentHotspotIndex < 0 || currentHotspotIndex >= hotspots.Length) return;
            SelectAndConfirmHotspot(currentHotspotIndex);
        }

        /// <summary>Navigates to a hotspot by regionId (used by external systems, e.g. mini-map).</summary>
        public void NavigateToRegion(string regionId)
        {
            if (regionToIndexMap != null && regionToIndexMap.TryGetValue(regionId, out int index))
                currentHotspotIndex = index;
        }

        public void ResetToFirstHotspot() => currentHotspotIndex = 0;

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        public MapHotspot CurrentHotspot => hotspots != null && currentHotspotIndex >= 0 && currentHotspotIndex < hotspots.Length
            ? hotspots[currentHotspotIndex]
            : null;

        public int CurrentIndex => currentHotspotIndex;
        public int HotspotCount => hotspots != null ? hotspots.Length : 0;
    }
}

