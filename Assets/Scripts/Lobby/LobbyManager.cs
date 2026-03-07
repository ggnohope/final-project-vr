using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    /// <summary>
    /// Manages the VR Lobby scene: player name entry, room creation, quick join, and room list.
    /// Transitions to the Props scene once a room is joined.
    /// </summary>
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        private const string PropsSceneName = "Props";
        private const byte MaxPlayersPerRoom = 8;

        // ── Login Panel ───────────────────────────────────────────────
        [Header("Login Panel")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button confirmNameButton;

        // ── Room Panel ────────────────────────────────────────────────
        [Header("Room Panel")]
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button quickJoinButton;

        // ── Room List Panel ───────────────────────────────────────────
        [Header("Room List Panel")]
        [SerializeField] private GameObject roomListPanel;
        [SerializeField] private Transform roomListContent;
        [SerializeField] private GameObject roomListEntryPrefab;
        [SerializeField] private Button backFromListButton;

        // ── Status ────────────────────────────────────────────────────
        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        private readonly Dictionary<string, RoomInfo> cachedRoomList = new();
        private readonly Dictionary<string, GameObject> roomListEntries = new();

        // ─────────────────────────────────────────────────────────────
        #region Unity

        private void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            quickJoinButton.onClick.AddListener(OnQuickJoinClicked);

            if (backFromListButton != null)
                backFromListButton.onClick.AddListener(OnBackFromListClicked);

            ShowPanel(loginPanel);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Button Handlers

        /// <summary>Validates the player name and connects to Photon master.</summary>
        private void OnConfirmNameClicked()
        {
            string playerName = playerNameInput.text.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                SetStatus("Please enter a name.");
                return;
            }

            PhotonNetwork.NickName = playerName;
            SetStatus("Connecting...");
            confirmNameButton.interactable = false;

            if (PhotonNetwork.IsConnected)
                ShowPanel(roomPanel);
            else
                PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>Creates a room with an optional custom name.</summary>
        private void OnCreateRoomClicked()
        {
            string roomName = roomNameInput.text.Trim();
            if (string.IsNullOrEmpty(roomName))
                roomName = "Room " + Random.Range(1000, 9999);

            var options = new RoomOptions { MaxPlayers = MaxPlayersPerRoom };
            SetStatus($"Creating room \"{roomName}\"...");
            PhotonNetwork.CreateRoom(roomName, options);
        }

        /// <summary>Attempts to join any open random room; creates one if none exist.</summary>
        private void OnQuickJoinClicked()
        {
            SetStatus("Finding a room...");
            PhotonNetwork.JoinRandomRoom();
        }

        private void OnBackFromListClicked()
        {
            ShowPanel(roomPanel);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region PUN Callbacks

        public override void OnConnectedToMaster()
        {
            SetStatus("Connected.");
            ShowPanel(roomPanel);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            SetStatus($"Disconnected: {cause}");
            confirmNameButton.interactable = true;
            ShowPanel(loginPanel);
        }

        public override void OnJoinedRoom()
        {
            SetStatus("Joined room. Loading...");
            // Only the master client drives the scene load; others sync automatically.
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(PropsSceneName);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            // No rooms available – create one automatically.
            string roomName = "Room " + Random.Range(1000, 9999);
            var options = new RoomOptions { MaxPlayers = MaxPlayersPerRoom };
            SetStatus($"No rooms found. Creating \"{roomName}\"...");
            PhotonNetwork.CreateRoom(roomName, options);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            SetStatus($"Create room failed: {message}");
            ShowPanel(roomPanel);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            SetStatus($"Join room failed: {message}");
            ShowPanel(roomPanel);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            UpdateCachedRoomList(roomList);
            RefreshRoomListView();
        }

        public override void OnJoinedLobby()
        {
            cachedRoomList.Clear();
            ClearRoomListView();
        }

        public override void OnLeftLobby()
        {
            cachedRoomList.Clear();
            ClearRoomListView();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Room List

        private void UpdateCachedRoomList(List<RoomInfo> roomList)
        {
            foreach (RoomInfo info in roomList)
            {
                if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
                {
                    cachedRoomList.Remove(info.Name);
                    continue;
                }

                cachedRoomList[info.Name] = info;
            }
        }

        private void RefreshRoomListView()
        {
            ClearRoomListView();

            foreach (RoomInfo info in cachedRoomList.Values)
            {
                GameObject entry = Instantiate(roomListEntryPrefab, roomListContent);
                entry.transform.localScale = Vector3.one;

                // Support the PUN demo RoomListEntry component if available.
                var entryComp = entry.GetComponent<Photon.Pun.Demo.Asteroids.RoomListEntry>();
                if (entryComp != null)
                {
                    entryComp.Initialize(info.Name, (byte)info.PlayerCount, (byte)info.MaxPlayers);
                }
                else if (entry.GetComponentInChildren<TMP_Text>() is TMP_Text label)
                {
                    label.text = $"{info.Name}  ({info.PlayerCount}/{info.MaxPlayers})";
                }

                // Allow clicking the entry to join.
                if (entry.GetComponent<Button>() is Button btn)
                    btn.onClick.AddListener(() => PhotonNetwork.JoinRoom(info.Name));

                roomListEntries[info.Name] = entry;
            }
        }

        private void ClearRoomListView()
        {
            foreach (GameObject entry in roomListEntries.Values)
                Destroy(entry);

            roomListEntries.Clear();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Helpers

        private void ShowPanel(GameObject target)
        {
            if (loginPanel != null) loginPanel.SetActive(loginPanel == target);
            if (roomPanel != null) roomPanel.SetActive(roomPanel == target);
            if (roomListPanel != null) roomListPanel.SetActive(roomListPanel == target);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;

            Debug.Log($"[LobbyManager] {message}");
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Demo / Editor Helpers

#if UNITY_EDITOR
        /// <summary>
        /// Injects fake room entries for screenshot / demo purposes.
        /// Call via right-click on LobbyManager component → "Demo: Inject Fake Rooms".
        /// Works in Play Mode without a real Photon connection.
        /// </summary>
        [ContextMenu("Demo: Inject Fake Rooms")]
        private void InjectFakeRooms()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[LobbyManager] Demo inject only works in Play Mode.");
                return;
            }

            cachedRoomList.Clear();
            ClearRoomListView();

            var fakeRooms = new[]
            {
                ("Field Survey A", 3, 8),
                ("Mapping Session B", 1, 8),
                ("Rock ID Lab", 5, 8),
            };

            foreach (var (roomName, playerCount, maxPlayers) in fakeRooms)
            {
                // Spawn entry directly without RoomInfo (which requires Photon internals)
                GameObject entry = Instantiate(roomListEntryPrefab, roomListContent);
                entry.transform.localScale = Vector3.one;

                if (entry.GetComponentInChildren<TMP_Text>() is TMP_Text label)
                    label.text = $"{roomName}  ({playerCount}/{maxPlayers})";

                roomListEntries[roomName] = entry;
            }

            ShowPanel(roomListPanel);
            Debug.Log("[LobbyManager] Fake rooms injected for demo.");
        }
#endif

        #endregion
    }
}
