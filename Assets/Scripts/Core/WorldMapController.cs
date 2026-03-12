using System.Collections;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Orchestrates the world map: owns the tile renderer, handles region loading,
    /// and exposes ShowWorldMap / HideWorldMap for other systems.
    ///
    /// The static RawImage texture has been replaced by MapTileRenderer (Mapbox tiles).
    /// This component no longer requires a RawImage on the same GameObject.
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SceneMapData sceneMapData;

        [Header("References")]
        [SerializeField] private GsplatSceneLoader sceneLoader;
        [SerializeField] private NetworkedMapSync networkedMapSync;
        [SerializeField] private Canvas worldMapCanvas;
        [SerializeField] private MapTileRenderer tileRenderer;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip regionClickSound;

        [Header("Multi-click Prevention")]
        [SerializeField] private float clickCooldown = 0.5f;

        private bool isProcessingClick = false;
        private float lastClickTime = 0f;

        private void Start()
        {
            if (sceneLoader == null)
                sceneLoader = FindFirstObjectByType<GsplatSceneLoader>();

            if (networkedMapSync == null)
                networkedMapSync = FindFirstObjectByType<NetworkedMapSync>();
        }

        /// <summary>Loads the given region with click feedback. Called by MapHotspotNavigator on confirm.</summary>
        public void LoadRegion(MapRegion region)
        {
            if (isProcessingClick || Time.time - lastClickTime < clickCooldown)
                return;

            if (sceneLoader == null || sceneLoader.IsLoading)
                return;

            lastClickTime = Time.time;
            StartCoroutine(OnRegionClickedWithFeedback(region));
        }

        private IEnumerator OnRegionClickedWithFeedback(MapRegion region)
        {
            isProcessingClick = true;
            PlaySound(regionClickSound);
            yield return new WaitForSeconds(0.3f);
            OnRegionClicked(region);
            isProcessingClick = false;
        }

        private void OnRegionClicked(MapRegion region)
        {
            if (worldMapCanvas != null)
                worldMapCanvas.gameObject.SetActive(false);

            if (ItemBarController.Instance != null)
                ItemBarController.Instance.DisableUIRay();

            // Route through NetworkedMapSync so the selection is broadcast to all players.
            // Falls back to direct load when offline.
            if (networkedMapSync != null)
                networkedMapSync.RequestLoadRegion(region.regionId);
            else
                sceneLoader.LoadScene(region.regionId, region.plyAssetPath, region.cameraConfig);
        }

        /// <summary>Makes the world map canvas visible.</summary>
        public void ShowWorldMap()
        {
            if (worldMapCanvas != null)
                worldMapCanvas.gameObject.SetActive(true);
        }

        /// <summary>Hides the world map canvas.</summary>
        public void HideWorldMap()
        {
            if (worldMapCanvas != null)
                worldMapCanvas.gameObject.SetActive(false);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
