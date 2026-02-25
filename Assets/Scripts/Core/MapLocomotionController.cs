using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace Core
{
    public class MapLocomotionController : MonoBehaviour
    {
        [Header("Locomotion Providers")]
        [SerializeField] private ContinuousMoveProvider moveProvider;
        [SerializeField] private SnapTurnProvider snapTurnProvider;
        [SerializeField] private ContinuousTurnProvider continuousTurnProvider;

        [Header("Map Navigation")]
        [SerializeField] private MapHotspotNavigator mapHotspotNavigator;

        public static MapLocomotionController Instance { get; private set; }

        private bool isMapOpen = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            AutoFindComponents();
        }

        private void AutoFindComponents()
        {
            if (moveProvider == null)
            {
                moveProvider = FindFirstObjectByType<ContinuousMoveProvider>();
            }

            if (snapTurnProvider == null)
            {
                snapTurnProvider = FindFirstObjectByType<SnapTurnProvider>();
            }

            if (continuousTurnProvider == null)
            {
                continuousTurnProvider = FindFirstObjectByType<ContinuousTurnProvider>();
            }

            if (mapHotspotNavigator == null)
            {
                mapHotspotNavigator = FindFirstObjectByType<MapHotspotNavigator>();
            }
        }

        public void OnMapOpened()
        {
            if (isMapOpen) return;

            isMapOpen = true;

            if (moveProvider != null)
            {
                moveProvider.enabled = false;
            }

            if (mapHotspotNavigator != null)
            {
                mapHotspotNavigator.enabled = true;
            }

            Debug.Log("[MapLocomotionController] Map opened - Movement disabled, Map navigation enabled");
        }

        public void OnMapClosed()
        {
            if (!isMapOpen) return;

            isMapOpen = false;

            if (moveProvider != null)
            {
                moveProvider.enabled = true;
            }

            if (mapHotspotNavigator != null)
            {
                mapHotspotNavigator.enabled = false;
            }

            Debug.Log("[MapLocomotionController] Map closed - Movement enabled, Map navigation disabled");
        }

        public void DisableAllLocomotion()
        {
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

            if (mapHotspotNavigator != null)
            {
                mapHotspotNavigator.enabled = false;
            }
        }

        public void EnableAllLocomotion()
        {
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
        }

        public bool IsMapOpen => isMapOpen;
    }
}
