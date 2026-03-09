using UnityEngine;
using VRDrawing.Mode;

namespace VRDrawing.Photo
{
    public class PhotoPlacementManager : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private float photoOffsetFromSurface = 0.001f;
        [SerializeField] private Material photoMaterial;

        public static PhotoPlacementManager Instance { get; private set; }

        /// <summary>
        /// Always false — placement is now instantaneous, no waiting state exists.
        /// Kept for backwards compatibility with UIRayDrawingTool.
        /// </summary>
        public bool IsInPlacementMode => false;

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

            if (photoMaterial == null)
            {
                photoMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                photoMaterial.SetFloat("_Surface", 1);
                photoMaterial.SetFloat("_Blend", 0);
            }
        }

        /// <summary>
        /// Immediately places the photo covering the full BaseImage layer of the active drawing board.
        /// Called as soon as the user selects a photo from the gallery.
        /// </summary>
        public void EnterPlacementMode(Texture2D photo)
        {
            if (photo == null)
            {
                Debug.LogError("[PhotoPlacementManager] Photo is null — cannot place.");
                return;
            }

            Debug.Log($"[PhotoPlacementManager] EnterPlacementMode called. Photo='{photo.name}' ({photo.width}x{photo.height})");
            Debug.Log($"[PhotoPlacementManager] DrawingModeManager.Instance={DrawingModeManager.Instance}, ActiveBoard={DrawingModeManager.Instance?.ActiveDrawingBoard}");

            DrawingSurface surface = FindActiveSurface();
            if (surface == null)
            {
                Debug.LogError("[PhotoPlacementManager] No active DrawingSurface found. " +
                               "Drawing mode must be entered before selecting a photo.");
                return;
            }

            Debug.Log($"[PhotoPlacementManager] Found surface '{surface.name}' on '{surface.transform.parent?.name}'. SurfaceSize={surface.GetSurfaceSize()}, localScale={surface.transform.localScale}");

            PlacePhotoOnBoard(surface, photo);
        }

        /// <summary>
        /// Finds the DrawingSurface on the currently active drawing board.
        /// Falls back to any DrawingSurface in the scene.
        /// </summary>
        private DrawingSurface FindActiveSurface()
        {
            if (DrawingModeManager.Instance != null)
            {
                GameObject board = DrawingModeManager.Instance.ActiveDrawingBoard;
                if (board != null)
                {
                    DrawingSurface surface = board.GetComponentInChildren<DrawingSurface>();
                    if (surface != null)
                    {
                        Debug.Log($"[PhotoPlacementManager] Found surface on active board '{board.name}'.");
                        return surface;
                    }
                    Debug.LogWarning($"[PhotoPlacementManager] Active board '{board.name}' has no DrawingSurface child.");
                }
                else
                {
                    Debug.LogWarning("[PhotoPlacementManager] DrawingModeManager.ActiveDrawingBoard is null — drawing mode may not be active.");
                }
            }
            else
            {
                Debug.LogWarning("[PhotoPlacementManager] DrawingModeManager.Instance is null.");
            }

            // Fallback
            DrawingSurface fallback = FindFirstObjectByType<DrawingSurface>();
            Debug.Log($"[PhotoPlacementManager] Fallback FindFirstObjectByType<DrawingSurface>() = {fallback}");
            return fallback;
        }

        /// <summary>
        /// Places the photo so it covers the entire board surface.
        /// Calculates world-space size from the DrawingSurface local scale and surfaceSize.
        /// </summary>
        private void PlacePhotoOnBoard(DrawingSurface surface, Texture2D photo)
        {
            // Board root = parent of DrawingSurface (i.e. the DrawingBoard GameObject)
            Transform boardRoot = surface.transform.parent != null
                ? surface.transform.parent
                : surface.transform;

            Debug.Log($"[PhotoPlacementManager] boardRoot='{boardRoot.name}', localScale={boardRoot.localScale}");

            // Find or create BaseImage layer at the board root level
            Transform baseImageLayer = boardRoot.Find("BaseImage");
            if (baseImageLayer == null)
            {
                GameObject baseImageObj = new GameObject("BaseImage");
                baseImageObj.transform.SetParent(boardRoot, false);
                // Slightly in front of the board surface (negative Z = toward the camera when board faces forward)
                baseImageObj.transform.localPosition = new Vector3(0f, 0f, -0.005f);
                baseImageObj.transform.localRotation = Quaternion.identity;
                baseImageLayer = baseImageObj.transform;
                Debug.Log($"[PhotoPlacementManager] Created BaseImage layer at local pos {baseImageObj.transform.localPosition}.");
            }

            // Remove any previous photo
            for (int i = baseImageLayer.childCount - 1; i >= 0; i--)
                Destroy(baseImageLayer.GetChild(i).gameObject);

            // Calculate the world-space dimensions of the DrawingSurface so the photo fills it exactly.
            // surfaceSize is in local units; the actual size in world space = surfaceSize * localScale * boardRoot.scale.
            Vector2 surfaceLocalSize = surface.GetSurfaceSize();
            Vector3 surfaceScale     = surface.transform.localScale;
            // World size relative to boardRoot (exclude boardRoot's own world scale — the photo is a child of boardRoot)
            float photoWidth  = surfaceLocalSize.x * surfaceScale.x;
            float photoHeight = surfaceLocalSize.y * surfaceScale.y;

            Debug.Log($"[PhotoPlacementManager] surfaceLocalSize={surfaceLocalSize}, surfaceScale={surfaceScale} → photoSize=({photoWidth}, {photoHeight})");

            GameObject photoObj = new GameObject($"Photo_{photo.name}");
            photoObj.transform.SetParent(baseImageLayer, false);
            photoObj.transform.localPosition = Vector3.zero;
            photoObj.transform.localRotation = Quaternion.identity;
            photoObj.transform.localScale    = new Vector3(photoWidth, photoHeight, 1f);

            MeshFilter   mf = photoObj.AddComponent<MeshFilter>();
            MeshRenderer mr = photoObj.AddComponent<MeshRenderer>();
            mf.mesh = CreateQuadMesh();

            Material mat = new Material(photoMaterial) { mainTexture = photo };
            mr.material = mat;

            Debug.Log($"[PhotoPlacementManager] Photo quad created. worldPos={photoObj.transform.position}, worldScale={photoObj.transform.lossyScale}");
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();

            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };

            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}

