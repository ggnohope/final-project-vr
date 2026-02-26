using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [Serializable]
    public struct CameraConfig
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView;
    }

    [Serializable]
    public struct MapRegion
    {
        /// <summary>Unique identifier matching the MapHotspot's regionId.</summary>
        public string regionId;

        /// <summary>Human-readable name shown in the tooltip.</summary>
        public string displayName;

        /// <summary>Resources-relative path to the GsplatAsset (.ply).</summary>
        public string plyAssetPath;

        /// <summary>Normalized UV rect (0–1) defining the clickable area on the map texture.</summary>
        public Rect bounds;

        /// <summary>Camera transform and FOV to apply when this region is loaded.</summary>
        public CameraConfig cameraConfig;
    }

    /// <summary>
    /// ScriptableObject holding world map configuration: texture and all region definitions.
    /// Create via Assets > Create > World Map > Scene Map Data.
    /// Assign to WorldMapController and MapHotspotNavigator in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldMapData", menuName = "World Map/Scene Map Data")]
    public class SceneMapData : ScriptableObject
    {
        [Header("Map Texture")]
        public Texture2D worldMapTexture;

        [Header("Regions")]
        [SerializeField] private List<MapRegion> regions = new List<MapRegion>();

        /// <summary>Read-only access to all defined regions.</summary>
        public IReadOnlyList<MapRegion> Regions => regions;

        /// <summary>
        /// Returns the first region whose normalized UV bounds contain the given position.
        /// Returns null if no region matches.
        /// </summary>
        public MapRegion? GetRegionByPosition(Vector2 normalizedPosition)
        {
            foreach (MapRegion region in regions)
            {
                if (region.bounds.Contains(normalizedPosition))
                {
                    return region;
                }
            }

            return null;
        }

        /// <summary>Returns the region matching the given regionId, or null if not found.</summary>
        public MapRegion? GetRegionById(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                return null;
            }

            foreach (MapRegion region in regions)
            {
                if (string.Equals(region.regionId, regionId, StringComparison.Ordinal))
                {
                    return region;
                }
            }

            return null;
        }
    }
}
