using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Core
{
    /// <summary>
    /// Renders a slippy tile map on a Unity Canvas using Mapbox tiles.
    /// Manages a pool of RawImage GameObjects that are repositioned and re-textured
    /// whenever the map is panned or zoomed.
    ///
    /// SETUP:
    /// 1. Attach this component to the MapImage RectTransform inside the WorldMapCanvas.
    /// 2. Assign MapboxConfig and MapTileFetcher references.
    /// 3. Assign tileContainer (a child RectTransform that fills the visible map area).
    /// 4. Subscribe to OnViewChanged to reposition hotspots after any pan/zoom.
    ///
    /// COORDINATE CONVENTIONS:
    /// - centerLatLng.x = latitude, centerLatLng.y = longitude (decimal degrees).
    /// - Canvas pixel (0,0) = canvas center. X right, Y up.
    ///
    /// ZOOM:
    /// - zoom is fractional. Tiles are fetched at FloorZoom(zoom).
    /// - Sub-integer zoom provides smooth interpolated positioning.
    /// </summary>
    public class MapTileRenderer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MapboxConfig config;
        [SerializeField] private MapTileFetcher tileFetcher;

        [Header("Canvas")]
        [Tooltip("RectTransform that tiles are parented to. Should fill the map panel. " +
                 "If left empty, uses this component's own RectTransform.")]
        [SerializeField] private RectTransform tileContainer;

        [Header("Placeholder")]
        [Tooltip("Color shown on a tile slot while the texture is being fetched.")]
        [SerializeField] private Color placeholderColor = new Color(0.18f, 0.20f, 0.24f, 1f);

        // --- State ---

        private Vector2 centerLatLng;
        private float zoom;
        private bool isInitialized = false;

        // Last valid canvas size — used as fallback when rect returns zero
        // (which can happen if Canvas layout hasn't run yet in a given frame)
        private Vector2 cachedCanvasSize = Vector2.zero;

        // Active tile images, keyed by tile coordinate
        private readonly Dictionary<MapTileFetcher.TileKey, RawImage> activeTiles = new();
        // Pool of inactive tile GOs available for reuse
        private readonly Queue<RawImage> tilePool = new();

        // --- Events ---

        /// <summary>
        /// Fired after every RefreshTiles() call (pan, zoom, or initial load).
        /// Listeners (e.g. MapHotspotNavigator) should call LatLngToCanvasPosition
        /// and update their hotspot anchoredPositions.
        /// </summary>
        public event Action<Vector2 /*centerLatLng*/, float /*zoom*/> OnViewChanged;

        // --- Unity lifecycle ---

        private void Awake()
        {
            // Fall back to own RectTransform so tileContainer can be left unassigned in Inspector
            if (tileContainer == null)
                tileContainer = GetComponent<RectTransform>();
        }

        private void Start()
        {
            centerLatLng = config.defaultCenter;
            zoom = config.defaultZoom;
            isInitialized = true;
            // Wait one frame so the Canvas layout system has measured tileContainer.rect
            StartCoroutine(InitialRefreshNextFrame());
        }

        private IEnumerator InitialRefreshNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshTiles();
        }

        private void OnEnable()
        {
            // Re-render after the canvas becomes visible again (e.g. map re-opened)
            if (isInitialized)
                StartCoroutine(InitialRefreshNextFrame());
        }

        // --- Public API ---

        /// <summary>Sets map center and zoom level, then refreshes the tile grid.</summary>
        public void SetView(float lat, float lng, float newZoom)
        {
            centerLatLng = new Vector2(lat, lng);
            zoom = Mathf.Clamp(newZoom, config.minZoom, config.maxZoom);
            RefreshTiles();
        }

        /// <summary>
        /// Pans the map by a delta in canvas pixels.
        /// Positive X = pan right (map moves right, view moves left = longitude decreases).
        /// Positive Y = pan up   (map moves up,   view moves down  = latitude increases).
        /// </summary>
        public void Pan(Vector2 pixelDelta)
        {
            Vector2 centerTile = MapTileCoordinates.LatLngToTileFractional(centerLatLng.x, centerLatLng.y, zoom);

            // Rendered tile size in canvas pixels accounts for fractional zoom scaling
            float scaleFactor = Mathf.Pow(2f, zoom - MapTileCoordinates.FloorZoom(zoom));
            float renderedTileSize = config.tileSize * scaleFactor;

            // Convert pixel delta to tile-space delta (1 tile unit = renderedTileSize canvas pixels)
            float tileDeltaX =  pixelDelta.x / renderedTileSize;
            float tileDeltaY = -pixelDelta.y / renderedTileSize;

            Vector2 newTile = centerTile + new Vector2(tileDeltaX, tileDeltaY);

            float maxTile = Mathf.Pow(2f, zoom);
            newTile.y = Mathf.Clamp(newTile.y, 0.001f, maxTile - 0.001f);

            float lng = newTile.x / maxTile * 360f - 180f;
            float latRad = (float)Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * newTile.y / maxTile)));
            float lat = latRad * Mathf.Rad2Deg;

            // Clamp to configured bounds (Vietnam bbox by default)
            lat = Mathf.Clamp(lat, config.minBounds.x, config.maxBounds.x);
            lng = Mathf.Clamp(lng, config.minBounds.y, config.maxBounds.y);

            centerLatLng = new Vector2(lat, lng);
            RefreshTiles();
        }

        /// <summary>
        /// Snaps zoom to the nearest integer level in the direction of delta.
        /// Tile textures only exist at integer zoom levels, so fractional zoom
        /// never triggers new tile fetches — snapping guarantees a tile refresh
        /// on every joystick input that crosses a zoom boundary.
        /// </summary>
        public void AdjustZoom(float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return;

            float target = delta > 0f
                ? Mathf.Floor(zoom) + 1f   // zoom in  → next integer up
                : Mathf.Ceil(zoom)  - 1f;  // zoom out → next integer down

            zoom = Mathf.Clamp(target, config.minZoom, config.maxZoom);
            RefreshTiles();
        }

        /// <summary>
        /// Converts a lat/lng to a canvas-local pixel offset from the canvas center.
        /// Use this to position hotspot RectTransforms after any pan/zoom.
        /// </summary>
        public Vector2 LatLngToCanvasPosition(float lat, float lng)
        {
            return MapTileCoordinates.LatLngToCanvasPosition(
                lat, lng,
                centerLatLng.x, centerLatLng.y,
                zoom, config.tileSize);
        }

        public Vector2 CenterLatLng => centerLatLng;
        public float CurrentZoom => zoom;

        // --- Tile grid ---

        private void RefreshTiles()
        {
            if (config == null || tileFetcher == null || tileContainer == null)
            {
                Debug.LogWarning("[MapTileRenderer] Missing config, tileFetcher, or tileContainer.");
                return;
            }

            int tileZoom = MapTileCoordinates.FloorZoom(zoom);
            int tilePixels = config.tileSize;

            // At fractional zoom, each floor-zoom tile appears larger by scaleFactor
            float scaleFactor   = Mathf.Pow(2f, zoom - tileZoom);
            float renderTileSize = tilePixels * scaleFactor;

            Rect canvas = tileContainer.rect;

            // Guard: if the Canvas layout hasn't measured the rect yet this frame,
            // use the last known good size. If we have no cached size either, abort —
            // the InitialRefreshNextFrame coroutine will retry after ForceUpdateCanvases.
            if (canvas.width > 1f && canvas.height > 1f)
                cachedCanvasSize = new Vector2(canvas.width, canvas.height);
            else if (cachedCanvasSize.sqrMagnitude < 1f)
            {
                Debug.LogWarning($"[MapTileRenderer] RefreshTiles aborted — canvas not ready: rect={canvas} | cachedCanvasSize={cachedCanvasSize}");
                return;
            }

            float halfW = cachedCanvasSize.x * 0.5f;
            float halfH = cachedCanvasSize.y * 0.5f;

            // Center of the view in fractional tile space at zoom z
            Vector2 centerTile = MapTileCoordinates.LatLngToTileFractional(centerLatLng.x, centerLatLng.y, zoom);

            // Convert center to floor-zoom tile space for integer tile arithmetic
            float centerTileFloorX = centerTile.x / scaleFactor;
            float centerTileFloorY = centerTile.y / scaleFactor;

            // Tiles needed to fill the canvas, with +2 safety buffer each side
            int tilesX = Mathf.CeilToInt(halfW / renderTileSize) + 2;
            int tilesY = Mathf.CeilToInt(halfH / renderTileSize) + 2;

            int centerTileX = Mathf.FloorToInt(centerTileFloorX);
            int centerTileY = Mathf.FloorToInt(centerTileFloorY);

            // Build the set of tile keys required this frame
            var neededKeys = new HashSet<MapTileFetcher.TileKey>();
            for (int dy = -tilesY; dy <= tilesY; dy++)
            {
                int ty = centerTileY + dy;
                if (ty < 0 || ty >= (1 << tileZoom)) continue;

                for (int dx = -tilesX; dx <= tilesX; dx++)
                {
                    int tx = MapTileCoordinates.WrapTileX(centerTileX + dx, tileZoom);
                    neededKeys.Add(new MapTileFetcher.TileKey(tx, ty, tileZoom));
                }
            }

            // Return tiles that are no longer visible to the pool
            var toRemove = new List<MapTileFetcher.TileKey>();
            foreach (var kv in activeTiles)
            {
                if (!neededKeys.Contains(kv.Key))
                {
                    ReturnToPool(kv.Key, kv.Value);
                    toRemove.Add(kv.Key);
                }
            }

            int returnedCount = toRemove.Count;
            foreach (var key in toRemove) activeTiles.Remove(key);

            // Create or reposition tiles for all needed keys
            int newRequests = 0;
            int repositioned = 0;
            foreach (var key in neededKeys)
            {
                Vector2 canvasPos = TileToCanvasPosition(key.X, key.Y, centerTileFloorX, centerTileFloorY, renderTileSize);

                if (!activeTiles.TryGetValue(key, out RawImage tileImage))
                {
                    tileImage = GetFromPool();
                    tileImage.color   = placeholderColor;
                    tileImage.texture = null;
                    activeTiles[key]  = tileImage;

                    tileFetcher.PinTile(key);

                    var capturedKey   = key;
                    var capturedImage = tileImage;

                    tileFetcher.RequestTile(key.X, key.Y, key.Zoom, tex =>
                    {
                        bool isStillActive = capturedImage != null && activeTiles.TryGetValue(capturedKey, out RawImage current) && current == capturedImage;
                        if (isStillActive)
                        {
                            capturedImage.texture = tex;
                            capturedImage.color   = tex != null ? Color.white : placeholderColor;
                        }
                    });
                    newRequests++;
                }
                else
                {
                    repositioned++;
                }

                // Size and position — +1px overlap prevents sub-pixel seam artifacts
                RectTransform rt = tileImage.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(renderTileSize + 1f, renderTileSize + 1f);
                rt.anchoredPosition = canvasPos;
            }

            OnViewChanged?.Invoke(centerLatLng, zoom);
        }

        /// <summary>
        /// Positions a tile's CENTER on the canvas.
        /// tileX/Y are in floor-zoom integer tile space.
        /// centerTileFloorX/Y are the map center in the same floor-zoom space.
        /// renderTileSize accounts for fractional zoom scaling.
        /// </summary>
        private Vector2 TileToCanvasPosition(int tileX, int tileY,
            float centerTileFloorX, float centerTileFloorY, float renderTileSize)
        {
            float pixelX =  (tileX + 0.5f - centerTileFloorX) * renderTileSize;
            float pixelY = -(tileY + 0.5f - centerTileFloorY) * renderTileSize; // flip Y for canvas
            return new Vector2(pixelX, pixelY);
        }

        // --- Tile pool ---

        private RawImage GetFromPool()
        {
            if (tilePool.Count > 0)
            {
                RawImage pooled = tilePool.Dequeue();
                pooled.gameObject.SetActive(true);
                // Keep tiles behind other UI children (hotspots, tooltips)
                pooled.transform.SetAsFirstSibling();
                return pooled;
            }

            GameObject go = new GameObject("MapTile", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(tileContainer, false);
            // Tiles always behind RegionHotspots and other siblings
            go.transform.SetAsFirstSibling();
            RawImage img = go.GetComponent<RawImage>();
            img.raycastTarget = false; // tiles must not block hotspot clicks
            return img;
        }

        private void ReturnToPool(MapTileFetcher.TileKey key, RawImage tile)
        {
            if (tile == null) return;
            tileFetcher?.UnpinTile(key);
            tile.texture = null;
            tile.color   = placeholderColor;
            tile.gameObject.SetActive(false);
            tilePool.Enqueue(tile);
        }

        private void OnDestroy()
        {
            foreach (var kv in activeTiles)
            {
                tileFetcher?.UnpinTile(kv.Key);
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            }
            while (tilePool.Count > 0)
            {
                var t = tilePool.Dequeue();
                if (t != null) Destroy(t.gameObject);
            }
        }
    }
}
