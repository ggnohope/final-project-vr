using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Links the VR character's IKTargetFollowVRRig to the local XR Origin tracking sources
    /// (headset and controllers) at runtime for the local player only.
    ///
    /// For remote players this component is inactive — their IK targets are driven
    /// by NetworkPlayerSync transforms that are synced via PhotonTransformView.
    ///
    /// Attach this alongside IKTargetFollowVRRig on the VRNetworkPlayer prefab root.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(IKTargetFollowVRRig))]
    public class VRNetworkPlayerIKLinker : MonoBehaviourPun
    {
        [Header("IK Target Transforms on This Prefab")]
        [Tooltip("Child transform used as the VR head IK target (mapped to headset).")]
        [SerializeField] private Transform headIKTarget;

        [Tooltip("Child transform used as the left hand IK target (mapped to left controller).")]
        [SerializeField] private Transform leftHandIKTarget;

        [Tooltip("Child transform used as the right hand IK target (mapped to right controller).")]
        [SerializeField] private Transform rightHandIKTarget;

        [Header("IK Offsets")]
        [Tooltip("Position offset applied to the head VRMap (e.g. to compensate for headset tracking origin).")]
        [SerializeField] private Vector3 headPositionOffset = new Vector3(0f, -0.1f, 0.13f);

        [Tooltip("Rotation offset applied to the head VRMap.")]
        [SerializeField] private Vector3 headRotationOffset = new Vector3(0f, 180f, 0f);

        [Tooltip("Position offset applied to the left hand VRMap.")]
        [SerializeField] private Vector3 leftHandPositionOffset = Vector3.zero;

        [Tooltip("Rotation offset applied to the left hand VRMap.")]
        [SerializeField] private Vector3 leftHandRotationOffset = new Vector3(-90f, 180f, 0f);

        [Tooltip("Position offset applied to the right hand VRMap.")]
        [SerializeField] private Vector3 rightHandPositionOffset = Vector3.zero;

        [Tooltip("Rotation offset applied to the right hand VRMap.")]
        [SerializeField] private Vector3 rightHandRotationOffset = new Vector3(-90f, 0f, 0f);

        private void Start()
        {
            if (!photonView.IsMine)
                return;

            LinkIKToXROrigin();
        }

        /// <summary>
        /// Locates the XROrigin in the scene and wires the IKTargetFollowVRRig VRMaps
        /// to the headset camera and XR controller transforms.
        /// </summary>
        private void LinkIKToXROrigin()
        {
            XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("[VRNetworkPlayerIKLinker] XROrigin not found in scene.");
                return;
            }

            Camera headsetCamera = xrOrigin.Camera;
            if (headsetCamera == null)
            {
                Debug.LogError("[VRNetworkPlayerIKLinker] XROrigin.Camera is null.");
                return;
            }

            Transform cameraOffset = xrOrigin.CameraFloorOffsetObject != null
                ? xrOrigin.CameraFloorOffsetObject.transform
                : xrOrigin.transform;

            Transform leftController  = FindChildByName(cameraOffset, "LeftHandController");
            Transform rightController = FindChildByName(cameraOffset, "RightHandController");

            IKTargetFollowVRRig ikRig = GetComponent<IKTargetFollowVRRig>();

            ikRig.head = BuildVRMap(
                vrTarget: headsetCamera.transform,
                ikTarget: headIKTarget,
                positionOffset: headPositionOffset,
                rotationOffset: headRotationOffset
            );

            ikRig.leftHand = BuildVRMap(
                vrTarget: leftController,
                ikTarget: leftHandIKTarget,
                positionOffset: leftHandPositionOffset,
                rotationOffset: leftHandRotationOffset
            );

            ikRig.rightHand = BuildVRMap(
                vrTarget: rightController,
                ikTarget: rightHandIKTarget,
                positionOffset: rightHandPositionOffset,
                rotationOffset: rightHandRotationOffset
            );

            if (leftController == null)
                Debug.LogError("[VRNetworkPlayerIKLinker] LeftHandController not found under XROrigin.");
            if (rightController == null)
                Debug.LogError("[VRNetworkPlayerIKLinker] RightHandController not found under XROrigin.");
        }

        /// <summary>Constructs a VRMap struct with the supplied tracking source and IK target.</summary>
        private static VRMap BuildVRMap(Transform vrTarget, Transform ikTarget, Vector3 positionOffset, Vector3 rotationOffset)
        {
            return new VRMap
            {
                vrTarget = vrTarget,
                ikTarget = ikTarget,
                trackingPositionOffset = positionOffset,
                trackingRotationOffset = rotationOffset
            };
        }

        /// <summary>Depth-first search for a child transform by exact name.</summary>
        private static Transform FindChildByName(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildByName(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
