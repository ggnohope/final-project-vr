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
                Debug.LogWarning("[MapHotspotNavigator] Cannot generate hotspots: missing sceneMapData, hotspotPrefab, or hotspotsContainer.");
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
                    Debug.LogWarning($"[MapHotspotNavigator] hotspotPrefab missing MapHotspot component. Skipping '{region.regionId}'.");
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
            Debug.Log($"[MapHotspotNavigator] Auto-generated {hotspots.Length} hotspots.");
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
                regionToIndexMap[hotspots[i].RegionId] = i;
            }

            Debug.Log($"[MapHotspotNavigator] Initialized {hotspots.Length} hotspots.");
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
                Debug.LogWarning($"[MapHotspotNavigator] No region data for hotspot {index}.");
                return;
            }

            if (tooltip != null)
                tooltip.Hide();

            Debug.Log($"[MapHotspotNavigator] Confirmed: {region.Value.displayName}");

            if (worldMapController != null)
                worldMapController.LoadRegion(region.Value);
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

