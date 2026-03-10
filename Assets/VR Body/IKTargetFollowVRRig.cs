using Photon.Pun;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform ikTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    /// <summary>
    /// Valid when vrTarget and ikTarget are distinct non-null objects.
    /// vrTarget == ikTarget is a circular reference that causes drift.
    /// </summary>
    public bool IsValid => vrTarget != null && ikTarget != null && vrTarget != ikTarget;

    public void Map()
    {
        ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}

public class IKTargetFollowVRRig : MonoBehaviour
{
    [Range(0, 1)]
    public float turnSmoothness = 0.1f;
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;

    private PhotonView photonView;
    private bool ownershipResolved;
    private bool isMine;

    private void Start()
    {
        photonView        = GetComponentInParent<PhotonView>();
        ownershipResolved = photonView != null;
        isMine            = ownershipResolved && photonView.IsMine;
    }

    private void LateUpdate()
    {
        if (!ownershipResolved) { RunFullIK(); return; }

        if (!isMine)  { ApplyIKMappingOnly(); return; }

        if (!head.IsValid) return;

        RunFullIK();
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Full IK for the local player: repositions body root and maps all VR tracking targets.</summary>
    private void RunFullIK()
    {
        transform.position = head.ikTarget.position + headBodyPositionOffset;

        float yaw = head.vrTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(transform.eulerAngles.x, yaw + headBodyYawOffset, transform.eulerAngles.z),
            turnSmoothness
        );

        head.Map();
        if (leftHand.IsValid)  leftHand.Map();
        if (rightHand.IsValid) rightHand.Map();
    }

    /// <summary>
    /// Remote avatar: IK targets are already positioned by PhotonTransformView.
    /// Only repositions and rotates body root to follow the synced head target.
    /// Animation Rigging constraints drive the skeleton bones from those targets.
    /// </summary>
    private void ApplyIKMappingOnly()
    {
        if (head.ikTarget == null) return;

        transform.position = head.ikTarget.position + headBodyPositionOffset;

        float yaw = head.ikTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(transform.eulerAngles.x, yaw + headBodyYawOffset, transform.eulerAngles.z),
            turnSmoothness
        );
    }
}
