using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Loads a GLB file from Resources/3DModels at runtime using glTFast and instantiates
    /// it as a child of this GameObject. Files must have a .bytes extension in the Resources folder.
    /// </summary>
    public class ModelLoader : MonoBehaviour
    {
        private const string ResourcesBasePath = "3DModels/";

        [Header("Model Settings")]
        [Tooltip("File name of the model inside Resources/3DModels (without extension).")]
        [SerializeField] private string modelName = "NC-1";

        [Tooltip("Local position offset applied to the instantiated model.")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        [Tooltip("Local rotation applied to the instantiated model.")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        [Tooltip("Uniform scale applied to the instantiated model.")]
        [SerializeField] private float scale = 1f;

        private GameObject loadedModelRoot;

        // ─────────────────────────────────────────────────────────────
        #region Unity

        private void Start()
        {
            _ = LoadModelAsync();
        }

        private void OnDestroy()
        {
            DestroyLoadedModel();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>
        /// Loads and instantiates the GLB model specified by <see cref="modelName"/> using glTFast.
        /// Destroys any previously loaded model first.
        /// </summary>
        public async Task LoadModelAsync()
        {
            DestroyLoadedModel();

            string resourcePath = ResourcesBasePath + modelName;
            Debug.Log($"[ModelLoader] Loading TextAsset at Resources/'{resourcePath}'");

            TextAsset glbAsset = Resources.Load<TextAsset>(resourcePath);

            if (glbAsset == null)
            {
                Debug.LogError($"[ModelLoader] TextAsset not found at Resources/'{resourcePath}'. " +
                               $"Ensure the file is renamed to '{modelName}.bytes' inside Assets/Resources/3DModels/.");
                return;
            }

            Debug.Log($"[ModelLoader] TextAsset loaded — byte length: {glbAsset.bytes.Length}. Starting glTFast import...");

            GltfImport gltfImport = new GltfImport();
            bool success = await gltfImport.LoadGltfBinary(glbAsset.bytes);

            if (!success)
            {
                Debug.LogError($"[ModelLoader] glTFast failed to parse '{modelName}.bytes'. " +
                               $"Ensure the file is a valid GLB binary.");
                gltfImport.Dispose();
                return;
            }

            // Create a root to hold the instantiated scene so transform offsets apply cleanly
            loadedModelRoot = new GameObject(modelName);
            loadedModelRoot.transform.SetParent(transform, false);
            loadedModelRoot.transform.SetLocalPositionAndRotation(positionOffset, Quaternion.Euler(rotationOffset));
            loadedModelRoot.transform.localScale = Vector3.one * scale;

            bool instantiated = await gltfImport.InstantiateMainSceneAsync(loadedModelRoot.transform);

            if (!instantiated)
            {
                Debug.LogError($"[ModelLoader] glTFast parsed '{modelName}' but failed to instantiate the scene.");
                gltfImport.Dispose();
                return;
            }

            Debug.Log($"[ModelLoader] Successfully loaded and instantiated '{modelName}' " +
                      $"with {loadedModelRoot.transform.childCount} child(ren).");
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Private

        private void DestroyLoadedModel()
        {
            if (loadedModelRoot != null)
            {
                Destroy(loadedModelRoot);
                loadedModelRoot = null;
            }
        }

        #endregion
    }
}
