using System;
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
        public string regionId;
        public string displayName;

        [Tooltip("Province centroid in decimal degrees. x = latitude (N), y = longitude (E).")]
        public Vector2 latLng;

        public string plyAssetPath;

        [Tooltip("Path to a video clip inside Resources folder (without extension), e.g. 'Videos/MyFlyover'. Used for the FlyCam preview mode.")]
        public string videoResourcePath;

        public CameraConfig cameraConfig;
        public Color regionHighlightColor;
    }

    [CreateAssetMenu(fileName = "SceneMapData", menuName = "World Map/Scene Map Data", order = 1)]
    public class SceneMapData : ScriptableObject
    {
        [Header("World Map Settings")]
        [Tooltip("No longer used — map tiles are fetched from Mapbox. Can be left empty.")]
        public Texture2D worldMapTexture;
        
        [Header("Region Definitions")]
        public MapRegion[] regions;

        [Header("Transition Settings")]
        public float transitionFadeTime = 0.5f;
        public float minimumLoadTime = 1.0f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Loading Screen")]
        public Sprite loadingScreenOverlay;
        public string loadingTextFormat = "Loading {0}...";

        /// <summary>
        /// Legacy UV-based lookup — no longer used with tile map.
        /// Kept to avoid breaking references; always returns null.
        /// </summary>
        public MapRegion? GetRegionByPosition(Vector2 normalizedPosition)
        {
            return null;
        }

        public MapRegion? GetRegionById(string regionId)
        {
            foreach (var region in regions)
            {
                if (region.regionId == regionId)
                {
                    return region;
                }
            }
            return null;
        }
    }
}
