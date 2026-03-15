using UnityEngine;
using VRDrawing.Mode;

namespace VRDrawing.Photo
{
    public class PhotoPlacementManager : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private float photoOffsetFromSurface = 0.001f;
        [SerializeField] private Material photoMaterial;

        private const string PhotoObjectName = "BoardPhoto";

        public static PhotoPlacementManager Instance { get; private set; }

        /// <summary>
        /// Always false — placement is instantaneous. Kept for backwards compatibility.
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
        /// Places the photo covering the entire drawing surface.
        /// Replaces any previously placed photo and clears all ink strokes.
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

            RemoveExistingPhoto(surface);
            surface.Clear();
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

        /// <summary>
        /// Destroys the existing board photo child object if one exists.
        /// </summary>
        private void RemoveExistingPhoto(DrawingSurface surface)
        {
            Transform existing = surface.transform.Find(PhotoObjectName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }

        private void PlacePhotoOnSurface(DrawingSurface surface, Texture2D photo)
        {
            GameObject photoObj = new GameObject(PhotoObjectName);
            photoObj.transform.SetParent(surface.transform);

            // Put the photo on the "Ignore Raycast" layer so the XRRayInteractor
            // passes straight through it and continues to hit the DrawingSurface collider.
            photoObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            Vector3 localSize = GetSurfaceLocalSize(surface);
            float surfaceWidth = localSize.x;
            float surfaceHeight = localSize.y;

            float photoAspect = (float)photo.width / photo.height;
            float boardAspect = surfaceWidth / surfaceHeight;

            float quadWidth, quadHeight;
            if (photoAspect >= boardAspect)
            {
                quadWidth = surfaceWidth;
                quadHeight = surfaceWidth / photoAspect;
            }
            else
            {
                quadHeight = surfaceHeight;
                quadWidth = surfaceHeight * photoAspect;
            }

            photoObj.transform.localPosition = new Vector3(0f, 0f, -photoOffsetFromSurface);
            photoObj.transform.localRotation = Quaternion.identity;
            photoObj.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);

            MeshFilter meshFilter = photoObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = photoObj.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateQuadMesh();

            Material photoMatInstance = new Material(photoMaterial);
            photoMatInstance.mainTexture = photo;
            meshRenderer.material = photoMatInstance;

            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 100;
        }

        /// <summary>
        /// Derives the surface's local width/height from its collider bounds.
        /// </summary>
        private Vector3 GetSurfaceLocalSize(DrawingSurface surface)
        {
            Collider col = surface.GetComponent<Collider>();
            if (col != null)
            {
                // Convert world-space bounds to local scale
                Vector3 worldSize = col.bounds.size;
                Vector3 localScale = surface.transform.lossyScale;
                return new Vector3(
                    localScale.x != 0f ? worldSize.x / Mathf.Abs(localScale.x) : worldSize.x,
                    localScale.y != 0f ? worldSize.y / Mathf.Abs(localScale.y) : worldSize.y,
                    1f
                );
            }

            // Fallback: use a sensible default
            return new Vector3(0.4f, 0.3f, 1f);
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

