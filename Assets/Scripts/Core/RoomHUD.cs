using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    /// <summary>
    /// Lightweight VR HUD displaying room and player info.
    /// Hidden by default — toggle visibility with the left controller Y button (SecondaryButton).
    /// Refreshes PUN data every <see cref="refreshInterval"/> seconds and on relevant PUN callbacks.
    /// </summary>
    public class RoomHUD : MonoBehaviourPunCallbacks
    {
        private const float DefaultRefreshInterval = 2f;
        private const string PingUnavailable = "--";
        private const string HostLabel = "HOST";

        [Header("UI References")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private TMP_Text playerListText;

        [Header("Toggle Input")]
        [Tooltip("Input action bound to Left Controller Y button. Assign an InputActionReference from the XRI Default Input Actions asset.")]
        [SerializeField] private InputActionReference toggleAction;

        [Header("Refresh")]
        [Tooltip("How often (in seconds) the HUD data is polled while visible.")]
        [SerializeField] private float refreshInterval = DefaultRefreshInterval;

        private float refreshTimer;
        private bool isVisible;

        // Fallback action created at runtime when no InputActionReference is assigned
        private InputAction fallbackToggleAction;

        // ─────────────────────────────────────────────────────────────
        #region Unity

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (toggleAction != null)
            {
                toggleAction.action.Enable();
                toggleAction.action.performed += OnTogglePerformed;
            }
            else
            {
                // Fallback: bind directly to Left Controller Secondary Button (Y)
                fallbackToggleAction = new InputAction(
                    name: "HUDToggle",
                    type: InputActionType.Button,
                    binding: "<XRController>{LeftHand}/{SecondaryButton}"
                );
                fallbackToggleAction.performed += OnTogglePerformed;
                fallbackToggleAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (toggleAction != null)
            {
                toggleAction.action.performed -= OnTogglePerformed;
            }
            else if (fallbackToggleAction != null)
            {
                fallbackToggleAction.performed -= OnTogglePerformed;
                fallbackToggleAction.Disable();
                fallbackToggleAction.Dispose();
                fallbackToggleAction = null;
            }
        }

        private void Update()
        {
            if (!isVisible)
                return;

            refreshTimer += Time.deltaTime;
            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;
                Refresh();
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Input

        private void OnTogglePerformed(InputAction.CallbackContext ctx)
        {
            SetVisible(!isVisible);
        }

        /// <summary>Shows or hides the HUD panel and triggers an immediate data refresh on show.</summary>
        private void SetVisible(bool visible)
        {
            isVisible = visible;

            if (hudPanel != null)
                hudPanel.SetActive(visible);

            if (visible)
            {
                refreshTimer = 0f;
                Refresh();
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region PUN Callbacks

        public override void OnPlayerEnteredRoom(Player newPlayer) { if (isVisible) Refresh(); }
        public override void OnPlayerLeftRoom(Player otherPlayer) { if (isVisible) Refresh(); }
        public override void OnMasterClientSwitched(Player newMasterClient) { if (isVisible) Refresh(); }
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { if (isVisible) Refresh(); }
        public override void OnJoinedRoom() => Refresh();

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Refresh

        /// <summary>Rebuilds all HUD text from current Photon room state.</summary>
        public void Refresh()
        {
            if (!PhotonNetwork.InRoom)
            {
                SetUnavailable();
                return;
            }

            Room room = PhotonNetwork.CurrentRoom;

            if (roomNameText != null)
                roomNameText.text = room.Name;

            if (playerCountText != null)
                playerCountText.text = $"{room.PlayerCount} / {room.MaxPlayers}";

            if (playerListText != null)
                playerListText.text = BuildPlayerList(room);
        }

        private void SetUnavailable()
        {
            if (roomNameText != null) roomNameText.text = "-";
            if (playerCountText != null) playerCountText.text = "- / -";
            if (playerListText != null) playerListText.text = string.Empty;
        }

        /// <summary>Builds a rich-text player list: bold for local, gold HOST badge, grey ping.</summary>
        private static string BuildPlayerList(Room room)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Player player in room.Players.Values)
            {
                bool isLocal = player.IsLocal;
                bool isMaster = player.IsMasterClient;

                string name = string.IsNullOrEmpty(player.NickName)
                    ? $"Player {player.ActorNumber}"
                    : player.NickName;

                string namePart = isLocal ? $"<b>{name}</b>" : name;
                string hostPart = isMaster ? $" <color=#FFD700>[{HostLabel}]</color>" : string.Empty;
                string pingPart = isLocal ? $"{PhotonNetwork.GetPing()} ms" : PingUnavailable;

                sb.AppendLine($"{namePart}{hostPart}  <color=#AAAAAA>{pingPart}</color>");
            }

            return sb.ToString().TrimEnd();
        }

        #endregion
    }
}
