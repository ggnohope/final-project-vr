using UnityEngine;
using VRDrawing.Mode;

namespace VRDrawing.Photo
{
    public class PhotoPlacementManager : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private float photoDefaultWidth = 0.2f;
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
        /// Immediately places the photo at the center of the active drawing board.
        /// Called as soon as the user selects a photo from the gallery.
        /// </summary>
        public void EnterPlacementMode(Texture2D photo)
        {
            if (photo == null)
            {
                Debug.LogWarning("[PhotoPlacementManager] Photo is null.");
                return;
            }

            DrawingSurface surface = FindActiveSurface();
            if (surface == null)
            {
                Debug.LogWarning("[PhotoPlacementManager] No active DrawingSurface found. " +
                                 "Make sure drawing mode is active before selecting a photo.");
                return;
            }

            PlacePhotoOnSurface(surface, photo);
            Debug.Log($"[PhotoPlacementManager] Photo '{photo.name}' placed on '{surface.name}'.");
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
                        return surface;
                }
            }

            return FindFirstObjectByType<DrawingSurface>();
        }

        private void PlacePhotoOnSurface(DrawingSurface surface, Texture2D photo)
        {
            GameObject photoObj = new GameObject($"Photo_{photo.name}");
            photoObj.transform.SetParent(surface.transform);

            float aspectRatio = (float)photo.width / photo.height;
            float photoWidth = photoDefaultWidth;
            float photoHeight = photoWidth / aspectRatio;

            // Place at the center of the surface, slightly in front of it
            photoObj.transform.localPosition = new Vector3(0f, 0f, -photoOffsetFromSurface);
            photoObj.transform.localRotation = Quaternion.identity;
            photoObj.transform.localScale = new Vector3(photoWidth, photoHeight, 1f);

            MeshFilter meshFilter = photoObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = photoObj.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateQuadMesh();

            Material photoMatInstance = new Material(photoMaterial);
            photoMatInstance.mainTexture = photo;
            meshRenderer.material = photoMatInstance;

            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 100;
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

