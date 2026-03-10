using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Drives only the IK target transforms (Head Target, Left Arm_target, Right Arm_target)
    /// from the local XR Origin each frame for the owner client.
    ///
    /// Body root positioning is delegated to IKTargetFollowVRRig (local)
    /// or PhotonTransformView (remote). This script does NOT touch transform.position.
    ///
    /// Remote clients receive IK target world positions via PhotonTransformView on each target child.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkPlayerSync : MonoBehaviourPun
    {
        [Header("IK Targets to Drive (local player only)")]
        [SerializeField] private Transform headIKTarget;
        [SerializeField] private Transform leftHandIKTarget;
        [SerializeField] private Transform rightHandIKTarget;

        private XROrigin xrOrigin;
        private Transform cameraTransform;
        private Transform leftControllerTransform;
        private Transform rightControllerTransform;

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

            SyncHeadIKTarget();
            SyncHandIKTargets();
        }

        private void SyncHeadIKTarget()
        {
            if (headIKTarget == null) return;
            headIKTarget.position = cameraTransform.position;
            headIKTarget.rotation = cameraTransform.rotation;
        }

        private void SyncHandIKTargets()
        {
            if (leftHandIKTarget != null && leftControllerTransform != null)
            {
                leftHandIKTarget.position = leftControllerTransform.position;
                leftHandIKTarget.rotation = leftControllerTransform.rotation;
            }

            if (rightHandIKTarget != null && rightControllerTransform != null)
            {
                rightHandIKTarget.position = rightControllerTransform.position;
                rightHandIKTarget.rotation = rightControllerTransform.rotation;
            }
        }

        private void ResolveXRReferences()
        {
            xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("[NetworkPlayerSync] XROrigin not found in scene.");
                return;
            }

            Camera mainCam = xrOrigin.Camera;
            if (mainCam == null)
            {
                Debug.LogError("[NetworkPlayerSync] XROrigin.Camera is null.");
                return;
            }

            cameraTransform = mainCam.transform;

            Transform cameraOffset = xrOrigin.CameraFloorOffsetObject != null
                ? xrOrigin.CameraFloorOffsetObject.transform
                : xrOrigin.transform;

            leftControllerTransform  = FindChildByName(cameraOffset, "LeftHandController");
            rightControllerTransform = FindChildByName(cameraOffset, "RightHandController");
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            if (parent.name == childName) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildByName(parent.GetChild(i), childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
