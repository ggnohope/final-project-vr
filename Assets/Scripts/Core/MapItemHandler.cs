using UnityEngine;

namespace Core
{
    public class MapItemHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas worldMapCanvas;
        [SerializeField] private Transform playerCamera;

        [Header("Positioning")]
        [SerializeField] private float distanceFromPlayer = 2f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, 0f);

        private bool isMapOpen = false;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main.transform;
            }

            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(false);
            }

            if (ItemBarController.Instance != null)
            {
                ItemBarController.Instance.OnItemSelected += OnItemSelected;
            }
        }

        private void OnDestroy()
        {
            if (ItemBarController.Instance != null)
            {
                ItemBarController.Instance.OnItemSelected -= OnItemSelected;
            }
        }

        private void OnItemSelected(ItemType itemType)
        {
            if (itemType == ItemType.Map)
            {
                ToggleMap();
            }
        }

        public void ToggleMap()
        {
            isMapOpen = !isMapOpen;

            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(isMapOpen);

                if (isMapOpen)
                {
                    PositionMapInFrontOfPlayer();
                }
            }
        }

        public void OpenMap()
        {
            if (!isMapOpen)
            {
                ToggleMap();
            }
        }

        public void CloseMap()
        {
            if (isMapOpen)
            {
                ToggleMap();
            }
        }

        private void PositionMapInFrontOfPlayer()
        {
            if (worldMapCanvas == null || playerCamera == null) return;

            Vector3 forward = playerCamera.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 targetPosition = playerCamera.position + forward * distanceFromPlayer + offset;

            worldMapCanvas.transform.position = targetPosition;

            worldMapCanvas.transform.LookAt(playerCamera.position);
            worldMapCanvas.transform.Rotate(0, 180, 0);
        }

        public bool IsMapOpen => isMapOpen;
    }
}
