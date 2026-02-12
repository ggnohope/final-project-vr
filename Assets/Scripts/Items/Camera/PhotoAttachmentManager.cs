using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRItems.Camera
{
    public class PhotoAttachmentManager : MonoBehaviour
    {
        [Header("Save Settings")]
        [SerializeField] private string photosFolderName = "CapturedPhotos";
        [SerializeField] private bool saveToDesktopAlso = true;

        private string projectPhotosPath;
        private string desktopPhotosPath;

        public static PhotoAttachmentManager Instance { get; private set; }

        public System.Action OnPhotosUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePaths();
        }

        private void InitializePaths()
        {
            projectPhotosPath = Path.Combine(Application.dataPath, photosFolderName);
            
            if (!Directory.Exists(projectPhotosPath))
            {
                Directory.CreateDirectory(projectPhotosPath);
                Debug.Log($"[PhotoAttachmentManager] Created photos folder: {projectPhotosPath}");
            }

            if (saveToDesktopAlso)
            {
                string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                desktopPhotosPath = Path.Combine(desktopPath, "VR Photos");
                
                if (!Directory.Exists(desktopPhotosPath))
                {
                    Directory.CreateDirectory(desktopPhotosPath);
                }
            }
        }

        public void SavePhoto(Texture2D photo)
        {
            if (photo == null)
            {
                Debug.LogError("[PhotoAttachmentManager] Photo is null!");
                return;
            }

            string filename = $"Photo_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            byte[] bytes = photo.EncodeToPNG();

            string projectPath = Path.Combine(projectPhotosPath, filename);
            File.WriteAllBytes(projectPath, bytes);
            Debug.Log($"[PhotoAttachmentManager] Photo saved to project: {projectPath}");

            if (saveToDesktopAlso && !string.IsNullOrEmpty(desktopPhotosPath))
            {
                string desktopPath = Path.Combine(desktopPhotosPath, filename);
                File.WriteAllBytes(desktopPath, bytes);
                Debug.Log($"[PhotoAttachmentManager] Photo also saved to desktop: {desktopPath}");
            }

#if UNITY_EDITOR
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () => OnPhotosUpdated?.Invoke();
#endif
        }

        public List<Texture2D> GetAllPhotos()
        {
            List<Texture2D> photos = new List<Texture2D>();

        #if UNITY_EDITOR
            string assetPath = "Assets/" + photosFolderName;
            
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                Debug.LogWarning($"[PhotoAttachmentManager] Folder not found: {assetPath}");
                return photos;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetPath });
            
            Debug.Log($"[PhotoAttachmentManager] Found {guids.Length} images in {assetPath}");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    photos.Add(texture);
                    Debug.Log($"[PhotoAttachmentManager] Loaded: {texture.name}");
                }
            }

            photos = photos.OrderByDescending(p => p.name).ToList();
        #else
            if (!Directory.Exists(projectPhotosPath))
            {
                Debug.LogWarning($"[PhotoAttachmentManager] Photos folder not found: {projectPhotosPath}");
                return photos;
            }

            string[] pngFiles = Directory.GetFiles(projectPhotosPath, "*.png");
            string[] jpgFiles = Directory.GetFiles(projectPhotosPath, "*.jpg");
            string[] allFiles = pngFiles.Concat(jpgFiles).ToArray();

            Debug.Log($"[PhotoAttachmentManager] Found {allFiles.Length} image files in runtime");

            foreach (string filePath in allFiles)
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                
                if (texture.LoadImage(fileData))
                {
                    texture.name = Path.GetFileNameWithoutExtension(filePath);
                    photos.Add(texture);
                    Debug.Log($"[PhotoAttachmentManager] Loaded runtime photo: {texture.name}");
                }
            }

            photos = photos.OrderByDescending(p => p.name).ToList();
        #endif

            Debug.Log($"[PhotoAttachmentManager] Total photos loaded: {photos.Count}");
            return photos;
        }


        public void AttachPhotoToBoard(Texture2D photo)
        {
            if (photo == null)
            {
                Debug.LogError("[PhotoAttachmentManager] Photo is null!");
                return;
            }

            if (VRDrawing.Mode.DrawingModeManager.Instance == null)
            {
                Debug.LogWarning("[PhotoAttachmentManager] DrawingModeManager not found");
                return;
            }

            GameObject activeBoard = VRDrawing.Mode.DrawingModeManager.Instance.ActiveDrawingBoard;
            
            if (activeBoard == null)
            {
                Debug.LogWarning("[PhotoAttachmentManager] No active drawing board found");
                return;
            }

            Renderer boardRenderer = activeBoard.GetComponentInChildren<Renderer>();
            if (boardRenderer == null)
            {
                Debug.LogWarning("[PhotoAttachmentManager] No Renderer found on drawing board");
                return;
            }

            Material boardMaterial = boardRenderer.material;
            if (boardMaterial != null)
            {
                boardMaterial.mainTexture = photo;
                Debug.Log($"[PhotoAttachmentManager] ✓ Photo '{photo.name}' attached to drawing board");
            }
        }

        public string GetProjectPhotosPath()
        {
            return projectPhotosPath;
        }

        public string GetDesktopPhotosPath()
        {
            return desktopPhotosPath;
        }
    }
}
