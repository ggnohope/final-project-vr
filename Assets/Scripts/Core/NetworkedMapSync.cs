using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Syncs the selected map region across all players in a Photon room.
    ///
    /// Flow:
    ///   1. Any player calls <see cref="RequestLoadRegion"/> (typically from WorldMapController).
    ///   2. This sets a Photon Room Custom Property with the chosen regionId.
    ///   3. All clients (including the sender) receive <see cref="OnRoomPropertiesUpdate"/>
    ///      and load the corresponding Gaussian Splat region locally.
    ///   4. Late-joining players also pick up the current region in <see cref="OnJoinedRoom"/>.
    ///
    /// Setup:
    ///   - Add this component to a persistent GameObject in the Props scene (e.g. GaussianSplattingManager).
    ///   - Assign <see cref="sceneMapData"/> and <see cref="sceneLoader"/> in the Inspector.
    /// </summary>
    public class NetworkedMapSync : MonoBehaviourPunCallbacks
    {
        private const string RoomPropertySelectedRegion = "selectedRegion";

        [Header("References")]
        [SerializeField] private SceneMapData sceneMapData;
        [SerializeField] private GsplatSceneLoader sceneLoader;

        private void Start()
        {
            if (sceneLoader == null)
                sceneLoader = FindFirstObjectByType<GsplatSceneLoader>();

            // If we joined a room that already has a region selected, load it immediately.
            if (PhotonNetwork.InRoom)
                TryLoadRegionFromRoomProperties(PhotonNetwork.CurrentRoom.CustomProperties);
        }

        // ─────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>
        /// Call this instead of GsplatSceneLoader.LoadScene directly.
        /// Broadcasts the region selection to all players via Room Properties.
        /// </summary>
        public void RequestLoadRegion(string regionId)
        {
            if (!PhotonNetwork.InRoom)
            {
                // Offline / single-player fallback: load directly.
                LoadRegionLocally(regionId);
                return;
            }

            var properties = new Hashtable
            {
                { RoomPropertySelectedRegion, regionId }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
            // OnRoomPropertiesUpdate fires on all clients including the local one.
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region PUN Callbacks

        /// <summary>Called on all clients when Room Custom Properties change.</summary>
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            TryLoadRegionFromRoomProperties(propertiesThatChanged);
        }

        /// <summary>Handles late-joining players who missed the original property set.</summary>
        public override void OnJoinedRoom()
        {
            TryLoadRegionFromRoomProperties(PhotonNetwork.CurrentRoom.CustomProperties);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Helpers

        private void TryLoadRegionFromRoomProperties(Hashtable properties)
        {
            if (properties == null || !properties.ContainsKey(RoomPropertySelectedRegion))
                return;

            string regionId = properties[RoomPropertySelectedRegion] as string;
            if (string.IsNullOrEmpty(regionId))
                return;

            // Skip if this region is already loaded or currently loading.
            if (sceneLoader != null && sceneLoader.CurrentSceneId == regionId)
                return;

            LoadRegionLocally(regionId);
        }

        private void LoadRegionLocally(string regionId)
        {
            if (sceneMapData == null)
            {
                Debug.LogError("[NetworkedMapSync] SceneMapData is not assigned.");
                return;
            }

            MapRegion? region = sceneMapData.GetRegionById(regionId);
            if (!region.HasValue)
            {
                Debug.LogWarning($"[NetworkedMapSync] Region '{regionId}' not found in SceneMapData.");
                return;
            }

            if (sceneLoader == null)
            {
                Debug.LogError("[NetworkedMapSync] GsplatSceneLoader is not assigned.");
                return;
            }

            sceneLoader.LoadScene(region.Value.regionId, region.Value.plyAssetPath, region.Value.cameraConfig);
        }

        #endregion
    }
}
