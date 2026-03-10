using Photon.Pun;
using TMPro;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Displays the Photon NickName of the owning player above the avatar.
    /// The canvas always faces the local camera (billboard effect).
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PlayerNameTag : MonoBehaviourPun
    {
        [SerializeField] private TMP_Text nameLabel;

        private Transform mainCameraTransform;

        private void Start()
        {
            nameLabel.text = photonView.Owner?.NickName ?? string.Empty;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                mainCameraTransform = mainCamera.transform;
        }

        private void LateUpdate()
        {
            if (mainCameraTransform == null)
                return;

            nameLabel.transform.parent.rotation = Quaternion.LookRotation(
                nameLabel.transform.parent.position - mainCameraTransform.position
            );
        }
    }
}
