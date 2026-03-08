using Photon.Pun;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Represents a networked player avatar in the shared scene.
    /// Synchronises position and rotation of the avatar head across the network.
    /// Attach this to the NetworkPlayer prefab alongside a PhotonView and PhotonTransformView.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkPlayerAvatar : MonoBehaviourPun
    {
        private const string LocalLayerName = "Ignore Raycast";

        [Header("Avatar Parts")]
        [Tooltip("The head mesh renderer — hidden for the local player so it doesn't block the camera.")]
        [SerializeField] private Renderer headRenderer;

        [Tooltip("The name tag shown above the avatar head.")]
        [SerializeField] private TextMesh nameTag;

        private void Start()
        {
            if (photonView.IsMine)
            {
                DisableLocalAvatarVisuals();
            }
            else
            {
                ApplyRemotePlayerName();
            }
        }

        /// <summary>Hides visuals that would obstruct the local player's camera.</summary>
        private void DisableLocalAvatarVisuals()
        {
            if (headRenderer != null)
                headRenderer.enabled = false;

            if (nameTag != null)
                nameTag.gameObject.SetActive(false);
        }

        /// <summary>Displays the remote player's Photon nickname above the avatar.</summary>
        private void ApplyRemotePlayerName()
        {
            if (nameTag == null)
                return;

            string displayName = photonView.Owner != null ? photonView.Owner.NickName : "Player";
            nameTag.text = string.IsNullOrEmpty(displayName) ? "Player" : displayName;
        }
    }
}
