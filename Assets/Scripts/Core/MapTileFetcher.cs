using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Core
{
    /// <summary>
    /// Fetches Mapbox tile textures via UnityWebRequest and caches them in memory.
    ///
    /// USAGE:
    /// - Call RequestTile(x, y, zoom, callback) to fetch a tile asynchronously.
    /// - The callback receives a Texture2D (or null on failure) when the request completes.
    /// - If the same tile is already in-flight, the new callback is queued and fires when
    ///   the single network request completes (no duplicate HTTP requests).
    /// - Cached tiles are returned immediately without a network call.
    ///
    /// CACHE EVICTION:
    /// - Eviction policy is LRU: the least-recently-used tile is evicted first.
    ///   Any cache hit or insertion promotes the tile to MRU position.
    /// - Pinned tiles are never evicted. Call PinTile / UnpinTile to mark tiles
    ///   that are currently visible on screen; MapTileRenderer manages this automatically.
    /// </summary>
    public class MapTileFetcher : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private MapboxConfig config;

        [Header("Debug")]
        [SerializeField] private bool logRequests = false;
        [SerializeField] private bool logCacheHits = false;

        // --- Public types ---

        public struct TileKey : IEquatable<TileKey>
        {
            public readonly int X, Y, Zoom;

            public TileKey(int x, int y, int zoom) { X = x; Y = y; Zoom = zoom; }

            public bool Equals(TileKey other) => X == other.X && Y == other.Y && Zoom == other.Zoom;
            public override bool Equals(object obj) => obj is TileKey k && Equals(k);
            public override int GetHashCode() => HashCode.Combine(X, Y, Zoom);
            public override string ToString() => $"[{Zoom}/{X}/{Y}]";
        }

        // --- Private state ---

        private readonly Dictionary<TileKey, Texture2D> cache = new();

        // LRU order: front = most-recently-used, back = least-recently-used.
        private readonly LinkedList<TileKey> lruList = new();
        private readonly Dictionary<TileKey, LinkedListNode<TileKey>> lruNodes = new();

        // Keys currently displayed on screen — never evicted.
        private readonly HashSet<TileKey> pinnedKeys = new();

        // Stores pending callbacks per in-flight tile (supports multiple callers for the same key)
        private readonly Dictionary<TileKey, List<Action<Texture2D>>> pendingRequests = new();

        private const int MaxConcurrentRequests = 4;
        private int activeRequests = 0;

        public MapboxConfig Config => config;

        // --- Public API ---

        /// <summary>
        /// Marks a tile as visible on screen. Pinned tiles are exempt from LRU eviction.
        /// Call this when a tile becomes active in MapTileRenderer.
        /// </summary>
        public void PinTile(TileKey key) => pinnedKeys.Add(key);

        /// <summary>
        /// Removes the pin from a tile (e.g. when it leaves the viewport).
        /// The tile remains in cache but is now eligible for LRU eviction.
        /// </summary>
        public void UnpinTile(TileKey key) => pinnedKeys.Remove(key);

        /// <summary>
        /// Returns a cached texture synchronously, or null if not yet cached.
        /// Promotes the tile to MRU position.
        /// </summary>
        public Texture2D GetCachedTile(int x, int y, int zoom)
        {
            var key = new TileKey(x, y, zoom);
            if (!cache.TryGetValue(key, out Texture2D tex)) return null;

            Touch(key);
            return tex;
        }

        /// <summary>
        /// Requests a tile texture. If already cached, onComplete is called immediately.
        /// If an identical request is already in-flight, the callback is queued and fires
        /// when the single network request completes.
        /// </summary>
        public void RequestTile(int x, int y, int zoom, Action<Texture2D> onComplete)
        {
            var key = new TileKey(x, y, zoom);

            if (cache.TryGetValue(key, out Texture2D cached))
            {
                if (logCacheHits)
                    Debug.Log($"[MapTileFetcher] Cache hit {key}");

                Touch(key);
                onComplete?.Invoke(cached);
                return;
            }

            if (pendingRequests.TryGetValue(key, out List<Action<Texture2D>> callbacks))
            {
                // Request already in-flight — queue callback instead of issuing a second request
                if (onComplete != null)
                    callbacks.Add(onComplete);
                return;
            }

            pendingRequests[key] = new List<Action<Texture2D>> { onComplete };
            StartCoroutine(FetchRoutine(key));
        }

        /// <summary>
        /// Destroys all non-visible cached textures and clears the cache.
        /// Textures pinned by currently visible tiles are NOT destroyed here —
        /// they will be released when <see cref="MapTileRenderer"/> destroys their GameObjects.
        /// </summary>
        public void ClearCache()
        {
            foreach (var kv in cache)
            {
                if (pinnedKeys.Contains(kv.Key)) continue;
                if (kv.Value != null) Destroy(kv.Value);
            }
            cache.Clear();
            lruList.Clear();
            lruNodes.Clear();
            // Keep pinnedKeys intact — MapTileRenderer still holds those refs
            pendingRequests.Clear();
        }

        // --- Private ---

        private IEnumerator FetchRoutine(TileKey key)
        {
            // Throttle concurrent requests
            while (activeRequests >= MaxConcurrentRequests)
                yield return null;

            activeRequests++;

            string url = config.BuildTileUrl(key.X, key.Y, key.Zoom);

            if (logRequests)
                Debug.Log($"[MapTileFetcher] GET {key} → {url}");

            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            // Collect and clear callbacks before invoking (allows re-request within callbacks)
            pendingRequests.TryGetValue(key, out List<Action<Texture2D>> callbacks);
            pendingRequests.Remove(key);
            activeRequests--;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[MapTileFetcher] Failed {key}: {request.error}");
                if (callbacks != null)
                    foreach (var cb in callbacks) cb?.Invoke(null);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            AddToCache(key, texture);

            if (callbacks != null)
                foreach (var cb in callbacks) cb?.Invoke(texture);
        }

        private void AddToCache(TileKey key, Texture2D texture)
        {
            if (cache.Count >= config.maxCachedTiles)
                EvictLRU();

            // Guard against duplicate insertion: remove the existing LRU node first to
            // prevent orphaned nodes that would desync the LRU list from the cache dict.
            if (lruNodes.TryGetValue(key, out LinkedListNode<TileKey> existing))
            {
                lruList.Remove(existing);
                lruNodes.Remove(key);

                // Destroy the stale texture only when it differs from the new one
                if (cache.TryGetValue(key, out Texture2D stale) && stale != null && stale != texture)
                    Destroy(stale);
            }

            cache[key] = texture;
            // Insert at MRU position (front of list)
            lruNodes[key] = lruList.AddFirst(key);
        }

        /// <summary>Promotes key to MRU position (front of list).</summary>
        private void Touch(TileKey key)
        {
            if (!lruNodes.TryGetValue(key, out LinkedListNode<TileKey> node)) return;
            lruList.Remove(node);
            lruList.AddFirst(node);
        }

        /// <summary>
        /// Evicts the least-recently-used tile that is NOT pinned (not currently visible).
        /// If all cached tiles are pinned, eviction is skipped and the cache grows beyond
        /// the configured limit until tiles leave the viewport.
        /// </summary>
        private void EvictLRU()
        {
            LinkedListNode<TileKey> node = lruList.Last;
            while (node != null)
            {
                TileKey candidate = node.Value;
                if (!pinnedKeys.Contains(candidate))
                {
                    if (cache.TryGetValue(candidate, out Texture2D tex) && tex != null)
                        Destroy(tex);

                    cache.Remove(candidate);
                    lruList.Remove(node);
                    lruNodes.Remove(candidate);
                    return;
                }
                node = node.Previous;
            }
            // All cached tiles are currently visible — defer eviction
        }

        private void OnDestroy()
        {
            // Destroy only non-visible textures; visible ones are released when the
            // RawImage GameObjects are destroyed by MapTileRenderer.OnDestroy.
            var toDestroy = new List<TileKey>(cache.Keys);
            foreach (TileKey key in toDestroy)
            {
                if (pinnedKeys.Contains(key)) continue;
                if (cache.TryGetValue(key, out Texture2D tex) && tex != null)
                    Destroy(tex);
            }
            cache.Clear();
            lruList.Clear();
            lruNodes.Clear();
            pinnedKeys.Clear();
            pendingRequests.Clear();
        }
    }
}
