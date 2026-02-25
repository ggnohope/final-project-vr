using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    /// <summary>
    /// Legacy component - Map is now opened via ItemBarCanvas
    /// This component is kept for backwards compatibility and debugging
    /// </summary>
    public class WorldMapInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldMapController worldMapController;
        [SerializeField] private GsplatSceneLoader sceneLoader;

        [Header("Debug Only - Map opens via ItemBarCanvas")]
        [SerializeField] private bool enableKeyboardShortcut = false;
        [SerializeField] private KeyCode toggleMapKey = KeyCode.M;

        private bool isMapVisible;

        private void Update()
        {
            if (enableKeyboardShortcut && Input.GetKeyDown(toggleMapKey))
            {
                ToggleMap();
            }
        }

        public void ToggleMap()
        {
            if (sceneLoader != null && sceneLoader.IsLoading)
            {
                return;
            }

            isMapVisible = !isMapVisible;

            if (isMapVisible)
            {
                ShowMap();
            }
            else
            {
                HideMap();
            }
        }

        public void ShowMap()
        {
            if (worldMapController != null)
            {
                worldMapController.ShowWorldMap();
                isMapVisible = true;
                
                if (MapLocomotionController.Instance != null)
                {
                    MapLocomotionController.Instance.OnMapOpened();
                }
            }
        }

        public void HideMap()
        {
            if (worldMapController != null)
            {
                worldMapController.HideWorldMap();
                isMapVisible = false;
                
                if (MapLocomotionController.Instance != null)
                {
                    MapLocomotionController.Instance.OnMapClosed();
                }
            }
        }
    }
}
