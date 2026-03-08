using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Drives the NetworkPlayer transform from the local XR Origin every frame.
    /// Only runs logic on the owner client (photonView.IsMine).
    /// Remote clients receive updated transforms via PhotonTransformView (PUN).
    ///
    /// Body root follows the Main Camera's world XZ position (floor-projected),
    /// so the avatar stays grounded even when the headset tilts.
    /// Head child mirrors the camera's full world position and rotation.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkPlayerSync : MonoBehaviourPun
    {
        [Header("Avatar Parts to Drive")]
        [Tooltip("Child transform representing the head — follows Main Camera full transform.")]
        [SerializeField] private Transform headTransform;

        // Cached references — resolved once in Start
        private XROrigin xrOrigin;
        private Transform cameraTransform;

        private void Start()
        {
            if (!photonView.IsMine)
                return;

            ResolveXRReferences();
        }

        private void LateUpdate()
        {
            if (!photonView.IsMine || xrOrigin == null || cameraTransform == null)
                return;

            SyncBodyToCamera();
            SyncHeadToCamera();
        }

        // ─────────────────────────────────────────────────────────────
        #region Sync

        /// <summary>
        /// Positions the NetworkPlayer root at the camera's world XZ, clamped to the XR Origin's Y floor.
        /// Yaw matches the camera's horizontal look direction.
        /// </summary>
        private void SyncBodyToCamera()
        {
            // Use camera world position for XZ, XR Origin Y for floor grounding
            Vector3 camPos = cameraTransform.position;
            float floorY = xrOrigin.transform.position.y;

            transform.position = new Vector3(camPos.x, floorY, camPos.z);

            // Yaw-only rotation from camera forward projected onto XZ plane
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        /// <summary>Matches the Head child full world position and rotation to the Main Camera.</summary>
        private void SyncHeadToCamera()
        {
            if (headTransform == null)
                return;

            headTransform.position = cameraTransform.position;
            headTransform.rotation = cameraTransform.rotation;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Setup

        /// <summary>Finds the XROrigin and Main Camera in the scene at runtime.</summary>
        private void ResolveXRReferences()
        {
            xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("[NetworkPlayerSync] XROrigin not found in scene. Avatar will not sync.");
                return;
            }

            Camera mainCam = xrOrigin.Camera;
            if (mainCam == null)
            {
                Debug.LogError("[NetworkPlayerSync] XROrigin.Camera is null. Head will not sync.");
                return;
            }

            cameraTransform = mainCam.transform;
            Debug.Log(
                $"[NetworkPlayerSync] Resolved XROrigin '{xrOrigin.name}' at {xrOrigin.transform.position}\n" +
                $"  Camera: '{cameraTransform.name}' world pos: {cameraTransform.position}\n" +
                $"  Camera local pos: {cameraTransform.localPosition}"
            );
        }

        #endregion
    }
}
