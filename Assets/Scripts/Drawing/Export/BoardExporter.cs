using UnityEngine;
using System.Collections;
using System.IO;
using VRDrawing.Mode;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRDrawing.Export
{
    /// <summary>
    /// Captures the full contents of the active drawing board (background photo, ink strokes,
    /// and geological symbol overlays) into a PNG file saved in the same folder as the photo
    /// gallery, so the exported image can be reopened and edited.
    /// </summary>
    public class BoardExporter : MonoBehaviour
    {
        [Header("Capture Settings")]
        [SerializeField] private int captureResolutionWidth = 2048;
        [SerializeField] private int captureResolutionHeight = 2048;
        [SerializeField] private Color backgroundClearColor = Color.white;

        [Header("Save Settings")]
        [SerializeField] private string exportFolderName = "CapturedPhotos";
        [SerializeField] private string exportFilePrefix = "Export";

        public static BoardExporter Instance { get; private set; }

        /// <summary>Fired when an export finishes — passes the saved file path.</summary>
        public System.Action<string> OnExportCompleted;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>Triggers a board capture on the next frame so all renderers are up to date.</summary>
        public void ExportBoard()
        {
            DrawingSurface surface = FindActiveSurface();
            if (surface == null)
            {
                Debug.LogWarning("[BoardExporter] No active DrawingSurface found.");
                return;
            }

            StartCoroutine(CaptureRoutine(surface));
        }

        private IEnumerator CaptureRoutine(DrawingSurface surface)
        {
            // Wait for end-of-frame so every renderer has submitted its draw calls.
            yield return new WaitForEndOfFrame();

            Texture2D captured = CaptureSurface(surface);
            if (captured == null)
                yield break;

            string savedPath = SaveTexture(captured);
            Destroy(captured);

            if (!string.IsNullOrEmpty(savedPath))
            {
#if UNITY_EDITOR
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () =>
                {
                    VRItems.Camera.PhotoAttachmentManager.Instance?.OnPhotosUpdated?.Invoke();
                    OnExportCompleted?.Invoke(savedPath);
                };
#else
                VRItems.Camera.PhotoAttachmentManager.Instance?.OnPhotosUpdated?.Invoke();
                OnExportCompleted?.Invoke(savedPath);
#endif
            }
        }

        private Texture2D CaptureSurface(DrawingSurface surface)
        {
            // Determine capture region from the placed photo's actual world size,
            // falling back to full board size when no photo is present.
            Vector2 captureWorldSize = GetPhotoWorldSize(surface);

            float aspect = captureWorldSize.x / captureWorldSize.y;
            int height   = captureResolutionHeight;
            int width    = Mathf.Max(1, Mathf.RoundToInt(height * aspect));

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;

            // Temporary orthographic camera aligned flush with the board face.
            GameObject camObj = new GameObject("_BoardExportCamera");
            Camera cam = camObj.AddComponent<Camera>();

            // Position camera in front of the board centre, looking straight at it.
            cam.transform.position = surface.transform.position - surface.transform.forward * 0.5f;
            cam.transform.rotation = surface.transform.rotation;

            cam.orthographic      = true;
            cam.orthographicSize  = captureWorldSize.y * 0.5f;
            cam.aspect            = aspect;
            cam.nearClipPlane     = 0.01f;
            cam.farClipPlane      = 2.0f;
            cam.clearFlags        = CameraClearFlags.SolidColor;
            cam.backgroundColor   = backgroundClearColor;
            cam.targetTexture     = rt;
            cam.cullingMask       = ~0;

            cam.Render();

            // Read pixels back to CPU.
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = prev;

            Destroy(camObj);
            rt.Release();
            Destroy(rt);

            return result;
        }

        /// <summary>
        /// Returns the world-space width and height of the photo quad placed on the board.
        /// If no photo is found, falls back to the full board collider size.
        /// </summary>
        private Vector2 GetPhotoWorldSize(DrawingSurface surface)
        {
            // PhotoPlacementManager names the quad "BoardPhoto" and parents it to the DrawingSurface.
            Transform photoTransform = surface.transform.Find("BoardPhoto");
            if (photoTransform != null)
            {
                // The quad uses localScale (quadWidth, quadHeight, 1) in local surface space.
                // lossyScale gives world-space size directly.
                Vector3 ws = photoTransform.lossyScale;
                float w = Mathf.Abs(ws.x);
                float h = Mathf.Abs(ws.y);
                if (w > 0.001f && h > 0.001f)
                    return new Vector2(w, h);
            }

            // Fallback: use full board size.
            return GetBoardWorldSize(surface, surface.GetComponent<Collider>());
        }

        private string SaveTexture(Texture2D texture)
        {
            string folderPath = Path.Combine(Application.dataPath, exportFolderName);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename  = $"{exportFilePrefix}_{timestamp}.png";
            string fullPath  = Path.Combine(folderPath, filename);

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, png);

            // Also copy to desktop folder if PhotoAttachmentManager has it configured.
            string desktopPath = VRItems.Camera.PhotoAttachmentManager.Instance?.GetDesktopPhotosPath();
            if (!string.IsNullOrEmpty(desktopPath) && Directory.Exists(desktopPath))
            {
                string desktopFile = Path.Combine(desktopPath, filename);
                File.WriteAllBytes(desktopFile, png);
            }

            return fullPath;
        }

        private Vector2 GetBoardWorldSize(DrawingSurface surface, Collider col)
        {
            if (col != null)
            {
                // bounds.size is world-space — project onto the board's local X/Y axes.
                Vector3 right = surface.transform.right;
                Vector3 up    = surface.transform.up;
                Vector3 size  = col.bounds.size;

                float w = Mathf.Abs(Vector3.Dot(size, right));
                float h = Mathf.Abs(Vector3.Dot(size, up));

                if (w > 0.001f && h > 0.001f)
                    return new Vector2(w, h);
            }

            // Fallback default.
            return new Vector2(0.4f, 0.3f);
        }

        private DrawingSurface FindActiveSurface()
        {
            if (DrawingModeManager.Instance != null)
            {
                GameObject board = DrawingModeManager.Instance.ActiveDrawingBoard;
                if (board != null)
                {
                    DrawingSurface s = board.GetComponentInChildren<DrawingSurface>();
                    if (s != null)
                        return s;
                }
            }

            return FindFirstObjectByType<DrawingSurface>();
        }
    }
}
