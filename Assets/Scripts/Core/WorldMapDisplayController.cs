using UnityEngine;

namespace Core
{
    /// <summary>
    /// Positions the WorldMapCanvas in front of the player and disables locomotion while the map is visible.
    /// Attach this component directly to the WorldMapCanvas GameObject.
    /// OnEnable: faces player, positions canvas, disables Move locomotion.
    /// OnDisable: restores Move locomotion.
    /// </summary>
    public class WorldMapDisplayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private GameObject moveLocomotionObject;

        [Header("Positioning")]
        [SerializeField] private float distanceFromPlayer = 2f;
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        private FloatingAnimation floatingAnimation;

        private void Awake()
        {
            floatingAnimation = GetComponent<FloatingAnimation>();

            if (playerCamera == null)
                playerCamera = Camera.main?.transform;
        }

        private void OnEnable()
        {
            PositionAndFacePlayer();
            DisableMovement();
        }

        private void OnDisable()
        {
            EnableMovement();
        }

        private void PositionAndFacePlayer()
        {
            if (playerCamera == null) return;

            Vector3 forward = playerCamera.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            forward.Normalize();

            transform.position = playerCamera.position + forward * distanceFromPlayer + positionOffset;

            Vector3 lookDir = transform.position - playerCamera.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir);

            if (floatingAnimation != null)
                floatingAnimation.ResetBasePosition();
        }

        private void DisableMovement()
        {
            if (moveLocomotionObject != null)
                moveLocomotionObject.SetActive(false);
        }

        private void EnableMovement()
        {
            if (moveLocomotionObject != null)
                moveLocomotionObject.SetActive(true);
        }
    }
}
