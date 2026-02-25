using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Core
{
    public class VRMapStartupController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldMapController worldMapController;
        [SerializeField] private GsplatSceneLoader sceneLoader;
        [SerializeField] private Canvas worldMapCanvas;

        [Header("Locomotion Components")]
        [SerializeField] private DynamicMoveProvider moveProvider;
        [SerializeField] private SnapTurnProvider snapTurnProvider;
        [SerializeField] private ContinuousTurnProvider continuousTurnProvider;

        private bool isLocomotionLocked = false;

        private void Start()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadCompleted += OnSceneLoaded;
            }
        }

        private void OnDestroy()
        {
            if (sceneLoader != null)
            {
                sceneLoader.OnSceneLoadCompleted -= OnSceneLoaded;
            }
        }

        public void LockLocomotion()
        {
            if (isLocomotionLocked) return;

            if (moveProvider != null)
            {
                moveProvider.enabled = false;
            }

            if (snapTurnProvider != null)
            {
                snapTurnProvider.enabled = false;
            }

            if (continuousTurnProvider != null)
            {
                continuousTurnProvider.enabled = false;
            }

            isLocomotionLocked = true;
        }

        public void UnlockLocomotion()
        {
            if (!isLocomotionLocked) return;

            if (moveProvider != null)
            {
                moveProvider.enabled = true;
            }

            if (snapTurnProvider != null)
            {
                snapTurnProvider.enabled = true;
            }

            if (continuousTurnProvider != null)
            {
                continuousTurnProvider.enabled = true;
            }

            isLocomotionLocked = false;
        }

        private void OnSceneLoaded(string regionId)
        {
            UnlockLocomotion();

            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(false);
            }
        }

        public bool IsLocked => isLocomotionLocked;
    }
}
