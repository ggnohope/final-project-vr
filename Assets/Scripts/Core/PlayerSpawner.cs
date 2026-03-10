using Photon.Pun;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Spawns the local player's networked avatar when joining a PUN room.
    /// Place this MonoBehaviour in the Props scene (or any scene loaded after joining a room).
    /// The prefab referenced by <see cref="PlayerPrefabName"/> must exist under a Resources folder.
    /// </summary>
    public class PlayerSpawner : MonoBehaviourPunCallbacks
    {
        private const string DefaultPrefabName = "VRNetworkPlayer";

        [Header("Spawn Settings")]
        [Tooltip("Name of the prefab inside a Resources folder to instantiate over the network.")]
        [SerializeField] private string playerPrefabName = DefaultPrefabName;

        [Tooltip("Spawn position offset applied to the local player on join.")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, 0f);

        [Tooltip("Optional: list of spawn points. A random one is chosen if provided.")]
        [SerializeField] private Transform[] spawnPoints;

        private GameObject spawnedPlayer;

        // ─────────────────────────────────────────────────────────────
        #region Unity

        private void Start()
        {
            // If we are already in a room when this scene loads, spawn immediately.
            if (PhotonNetwork.InRoom && spawnedPlayer == null)
            {
                SpawnPlayer();
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region PUN Callbacks

        public override void OnJoinedRoom()
        {
            if (spawnedPlayer == null)
                SpawnPlayer();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Spawning

        /// <summary>Instantiates the local player avatar over the Photon network.</summary>
        private void SpawnPlayer()
        {
            Vector3 position = ChooseSpawnPosition();
            spawnedPlayer = PhotonNetwork.Instantiate(playerPrefabName, position, Quaternion.identity);
        }

        private Vector3 ChooseSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                return point != null ? point.position : spawnPosition;
            }

            return spawnPosition;
        }

        #endregion
    }
}
