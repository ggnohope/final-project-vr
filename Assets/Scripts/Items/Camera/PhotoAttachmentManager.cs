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
                return;

            string filename = $"Photo_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            byte[] bytes = photo.EncodeToPNG();

            string projectPath = Path.Combine(projectPhotosPath, filename);
            File.WriteAllBytes(projectPath, bytes);

            if (saveToDesktopAlso && !string.IsNullOrEmpty(desktopPhotosPath))
            {
                string desktopPath = Path.Combine(desktopPhotosPath, filename);
                File.WriteAllBytes(desktopPath, bytes);
            }

#if UNITY_EDITOR
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () => OnPhotosUpdated?.Invoke();
#endif
        }

        /// <summary>Deletes the given photos by name from the project photos folder (and desktop if enabled). Fires OnPhotosUpdated when done.</summary>
        public void DeletePhotos(List<Texture2D> photosToDelete)
        {
            if (photosToDelete == null || photosToDelete.Count == 0)
                return;

            foreach (Texture2D photo in photosToDelete)
            {
                if (photo == null)
                    continue;

#if UNITY_EDITOR
                string assetPath = "Assets/" + photosFolderName + "/" + photo.name + ".png";
                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    assetPath = "Assets/" + photosFolderName + "/" + photo.name + ".jpg";
                    AssetDatabase.DeleteAsset(assetPath);
                }
#else
                string pngPath = Path.Combine(projectPhotosPath, photo.name + ".png");
                string jpgPath = Path.Combine(projectPhotosPath, photo.name + ".jpg");
                if (File.Exists(pngPath)) File.Delete(pngPath);
                else if (File.Exists(jpgPath)) File.Delete(jpgPath);

                if (saveToDesktopAlso && !string.IsNullOrEmpty(desktopPhotosPath))
                {
                    string dPng = Path.Combine(desktopPhotosPath, photo.name + ".png");
                    string dJpg = Path.Combine(desktopPhotosPath, photo.name + ".jpg");
                    if (File.Exists(dPng)) File.Delete(dPng);
                    else if (File.Exists(dJpg)) File.Delete(dJpg);
                }
#endif
            }

#if UNITY_EDITOR
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () => OnPhotosUpdated?.Invoke();
#else
            OnPhotosUpdated?.Invoke();
#endif
        }

        public List<Texture2D> GetAllPhotos()
        {
            List<Texture2D> photos = new List<Texture2D>();

        #if UNITY_EDITOR
            string assetPath = "Assets/" + photosFolderName;
            
            if (!AssetDatabase.IsValidFolder(assetPath))
                return photos;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                    photos.Add(texture);
            }

            photos = photos.OrderByDescending(p => p.name).ToList();
        #else
            if (!Directory.Exists(projectPhotosPath))
                return photos;

            string[] pngFiles = Directory.GetFiles(projectPhotosPath, "*.png");
            string[] jpgFiles = Directory.GetFiles(projectPhotosPath, "*.jpg");
            string[] allFiles = pngFiles.Concat(jpgFiles).ToArray();

            foreach (string filePath in allFiles)
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                
                if (texture.LoadImage(fileData))
                {
                    texture.name = Path.GetFileNameWithoutExtension(filePath);
                    photos.Add(texture);
                }
            }

            photos = photos.OrderByDescending(p => p.name).ToList();
        #endif

            return photos;
        }


        /// <summary>Attaches a photo texture to the active drawing board, filling it entirely regardless of aspect ratio.</summary>
        public void AttachPhotoToBoard(Texture2D photo)
        {
            if (photo == null)
                return;

            if (VRDrawing.Mode.DrawingModeManager.Instance == null)
                return;

            GameObject activeBoard = VRDrawing.Mode.DrawingModeManager.Instance.ActiveDrawingBoard;

            if (activeBoard == null)
                return;

            Renderer boardRenderer = activeBoard.GetComponentInChildren<Renderer>();
            if (boardRenderer == null)
                return;

            Material boardMaterial = boardRenderer.material;
            if (boardMaterial == null)
                return;

            boardMaterial.mainTexture = photo;

            // Reset tiling and offset so the photo fills the entire board surface
            boardMaterial.mainTextureScale = Vector2.one;
            boardMaterial.mainTextureOffset = Vector2.zero;
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
