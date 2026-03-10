using Photon.Pun;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Represents a networked VR player avatar in the shared scene.
    /// Hides all body renderers for the local player so they do not occlude the camera.
    /// Displays the remote player's Photon nickname on the floating name tag.
    /// Attach this to the VRNetworkPlayer prefab alongside a PhotonView.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkPlayerAvatar : MonoBehaviourPun
    {
        [Header("Avatar Parts")]
        [Tooltip("The name tag shown above the avatar head.")]
        [SerializeField] private TextMesh nameTag;

        [Tooltip("Root of the VR character model whose renderers are hidden for the local player.")]
        [SerializeField] private GameObject characterModelRoot;

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

        /// <summary>Hides all character renderers that would occlude the local player's camera view.</summary>
        private void DisableLocalAvatarVisuals()
        {
            if (characterModelRoot != null)
            {
                foreach (Renderer r in characterModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
                    r.enabled = false;
            }

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
