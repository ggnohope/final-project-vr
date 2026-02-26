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
        public Rect uvBounds;
        public string plyAssetPath;
        public CameraConfig cameraConfig;
        public Color regionHighlightColor;
    }

    [CreateAssetMenu(fileName = "SceneMapData", menuName = "World Map/Scene Map Data", order = 1)]
    public class SceneMapData : ScriptableObject
    {
        [Header("World Map Settings")]
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

        public MapRegion? GetRegionByPosition(Vector2 normalizedPosition)
        {
            foreach (var region in regions)
            {
                if (region.uvBounds.Contains(normalizedPosition))
                {
                    return region;
                }
            }
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
