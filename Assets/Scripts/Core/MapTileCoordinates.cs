using System;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Static utility for Web Mercator (EPSG:3857) tile coordinate math.
    ///
    /// COORDINATE CONVENTIONS:
    /// - lat/lng: decimal degrees, WGS84. lat positive = North, lng positive = East.
    /// - Tile XY origin: (0,0) = top-left (NW corner of world). X increases East, Y increases South.
    /// - Canvas pixel origin: (0,0) = canvas center. X increases Right, Y increases Up (Unity convention).
    ///
    /// ZOOM:
    /// - Integer zoom is used for tile fetching (there are 2^zoom tiles per axis).
    /// - Fractional zoom is used for sub-tile pixel offset calculations.
    /// </summary>
    public static class MapTileCoordinates
    {
        /// <summary>
        /// Converts lat/lng to fractional tile position at the given zoom.
        /// The integer part is the tile column/row; the fraction is the offset within that tile.
        /// </summary>
        public static Vector2 LatLngToTileFractional(float lat, float lng, float zoom)
        {
            float n = Mathf.Pow(2f, zoom);
            float latRad = lat * Mathf.Deg2Rad;
            float x = (lng + 180f) / 360f * n;
            float y = (1f - Mathf.Log(Mathf.Tan(latRad) + 1f / Mathf.Cos(latRad)) / Mathf.PI) / 2f * n;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Converts an integer tile (X, Y, zoom) to the lat/lng of its North-West corner.
        /// </summary>
        public static Vector2 TileToLatLng(int tileX, int tileY, int zoom)
        {
            float n = Mathf.Pow(2f, zoom);
            float lng = tileX / n * 360f - 180f;
            float latRad = (float)Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * tileY / n)));
            float lat = latRad * Mathf.Rad2Deg;
            return new Vector2(lat, lng);
        }

        /// <summary>
        /// Converts a lat/lng to a canvas-local pixel offset from the canvas center.
        /// Positive X = right, positive Y = up (Unity canvas convention).
        /// </summary>
        public static Vector2 LatLngToCanvasPosition(
            float lat, float lng,
            float centerLat, float centerLng,
            float zoom, int tileSize)
        {
            Vector2 point  = LatLngToTileFractional(lat, lng, zoom);
            Vector2 center = LatLngToTileFractional(centerLat, centerLng, zoom);
            Vector2 tileDelta = point - center;

            // Tile Y increases downward; canvas Y increases upward — flip Y.
            return new Vector2(tileDelta.x * tileSize, -tileDelta.y * tileSize);
        }

        /// <summary>Wraps a tile X coordinate to stay within [0, 2^zoom).</summary>
        public static int WrapTileX(int tileX, int zoom)
        {
            int n = 1 << zoom;
            return ((tileX % n) + n) % n;
        }

        /// <summary>Returns the integer zoom level used for tile fetching (floor of fractional zoom).</summary>
        public static int FloorZoom(float zoom) => Mathf.FloorToInt(zoom);
    }
}
