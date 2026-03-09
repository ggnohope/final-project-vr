using System.IO;
using UnityEngine;
using VRDrawing.Data;

namespace VRDrawing.Features
{
    /// <summary>
    /// Provides a save/load API for the annotation board that automatically
    /// names the JSON file after the captured image and writes it to
    /// Application.persistentDataPath/AnnotationSaves/.
    ///
    /// Wire this to the SymbolToolMenuUI's save button via the Inspector,
    /// or call SaveCurrentAnnotations() directly.
    /// </summary>
    public class AnnotationSaveController : MonoBehaviour
    {
        private const string SaveDirectory = "AnnotationSaves";

        public static AnnotationSaveController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>
        /// Saves annotations to a timestamped JSON file.
        /// Optionally tags the annotation data with the captured image name.
        /// </summary>
        public void SaveCurrentAnnotations(string capturedImageName = "")
        {
            if (SymbolLayerManager.Instance == null)
            {
                Debug.LogWarning("[AnnotationSaveController] SymbolLayerManager not found.");
                return;
            }

            string dir = Path.Combine(Application.persistentDataPath, SaveDirectory);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string filename = $"annotation_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = Path.Combine(dir, filename);

            SymbolLayerManager.Instance.SaveAnnotations(path);
            Debug.Log($"[AnnotationSaveController] Saved to {path}");
        }

        /// <summary>
        /// Loads annotations from the most recent JSON file in the save directory.
        /// </summary>
        public void LoadLatestAnnotations()
        {
            if (SymbolLayerManager.Instance == null)
            {
                Debug.LogWarning("[AnnotationSaveController] SymbolLayerManager not found.");
                return;
            }

            string dir = Path.Combine(Application.persistentDataPath, SaveDirectory);
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[AnnotationSaveController] No save directory at {dir}");
                return;
            }

            string[] files = Directory.GetFiles(dir, "*.json");
            if (files.Length == 0)
            {
                Debug.LogWarning("[AnnotationSaveController] No annotation files found.");
                return;
            }

            // Pick the most recently written file
            string latest = files[0];
            System.DateTime latestTime = File.GetLastWriteTime(files[0]);
            foreach (string f in files)
            {
                System.DateTime t = File.GetLastWriteTime(f);
                if (t > latestTime) { latestTime = t; latest = f; }
            }

            SymbolLayerManager.Instance.LoadAnnotations(latest);
        }
    }
}
