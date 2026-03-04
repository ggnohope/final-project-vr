using UnityEngine;

namespace Core
{
    /// <summary>
    /// ScriptableObject holding all Mapbox API configuration.
    /// Create via: Assets > Create > World Map > Mapbox Config
    /// Never hardcode the access token — assign it only in the asset, never commit to source control.
    /// </summary>
    [CreateAssetMenu(fileName = "MapboxConfig", menuName = "World Map/Mapbox Config", order = 2)]
    public class MapboxConfig : ScriptableObject
    {
        [Header("API")]
        [Tooltip("Your Mapbox public access token from mapbox.com/account/access-tokens")]
        public string accessToken = "";

        [Tooltip("Mapbox style ID. E.g. mapbox/streets-v12, mapbox/satellite-streets-v12, mapbox/outdoors-v12")]
        public string styleId = "mapbox/streets-v12";

        [Tooltip("Tile pixel size. Mapbox supports 256 or 512. 512 gives sharper tiles on high-DPI.")]
        public int tileSize = 256;

        [Header("Default View — Vietnam")]
        [Tooltip("Lat/lng shown at canvas center when the map first opens. x=latitude, y=longitude.")]
        public Vector2 defaultCenter = new Vector2(16.0f, 106.5f);

        [Tooltip("Zoom level on open. 5 fits all of Vietnam.")]
        [Range(1f, 20f)]
        public float defaultZoom = 5f;

        [Range(1f, 20f)]
        public float minZoom = 4f;

        [Range(1f, 20f)]
        public float maxZoom = 14f;

        [Header("Pan Bounds (decimal degrees)")]
        [Tooltip("Minimum lat/lng the map center can pan to. x = min latitude, y = min longitude.")]
        public Vector2 minBounds = new Vector2(7.0f, 100.5f);   // SW corner with padding
        [Tooltip("Maximum lat/lng the map center can pan to. x = max latitude, y = max longitude.")]
        public Vector2 maxBounds = new Vector2(24.5f, 110.5f);  // NE corner with padding
        [Tooltip("Max tiles kept in memory. Increase if zoom/pan stutters; decrease to save memory.")]
        public int maxCachedTiles = 64;

        /// <summary>Builds the Mapbox Styles API tile URL for the given tile coordinates.</summary>
        public string BuildTileUrl(int x, int y, int zoom)
        {
            return $"https://api.mapbox.com/styles/v1/{styleId}/tiles/{tileSize}/{zoom}/{x}/{y}?access_token={accessToken}";
        }
    }
}
