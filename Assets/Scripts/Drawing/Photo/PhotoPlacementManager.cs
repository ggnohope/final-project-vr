using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRDrawing.Photo
{
    public class PhotoPlacementManager : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private float photoDefaultWidth = 0.2f;
        [SerializeField] private float photoOffsetFromSurface = 0.001f;
        [SerializeField] private Material photoMaterial;
        
        [Header("References")]
        [SerializeField] private XRRayInteractor uiRayInteractor;
        
        [Header("Input")]
        [SerializeField] private InputActionProperty placePhotoAction;
        
        private Texture2D selectedPhoto;
        private bool isInPlacementMode = false;
        
        public static PhotoPlacementManager Instance { get; private set; }
        
        public bool IsInPlacementMode => isInPlacementMode;
        
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
        
        public void EnterPlacementMode(Texture2D photo)
        {
            if (photo == null)
            {
                Debug.LogWarning("[PhotoPlacementManager] Photo is null!");
                return;
            }
            
            selectedPhoto = photo;
            isInPlacementMode = true;
            
            // ENABLE input CHỈ KHI VÀO PLACEMENT MODE
            EnablePlacementInput();
            
            Debug.Log($"[PhotoPlacementManager] ✅ Entered placement mode - Input ENABLED");
            Debug.Log("[PhotoPlacementManager] 👉 CLICK on the board to place the photo!");
        }
        
        public void ExitPlacementMode()
        {
            selectedPhoto = null;
            isInPlacementMode = false;
            
            // DISABLE input KHI THOÁT PLACEMENT MODE
            DisablePlacementInput();
            
            Debug.Log("[PhotoPlacementManager] ❌ Exited placement mode - Input DISABLED");
        }

        private void EnablePlacementInput()
        {
            if (placePhotoAction.action != null)
            {
                placePhotoAction.action.Enable();
                placePhotoAction.action.performed += OnPlacePhotoInput;
            }
        }

        private void DisablePlacementInput()
        {
            if (placePhotoAction.action != null)
            {
                placePhotoAction.action.performed -= OnPlacePhotoInput;
                placePhotoAction.action.Disable();
            }
        }
        
        private void OnPlacePhotoInput(InputAction.CallbackContext context)
        {
            if (!isInPlacementMode || selectedPhoto == null)
                return;
            
            if (uiRayInteractor == null)
            {
                Debug.LogError("[PhotoPlacementManager] UI Ray Interactor is null!");
                return;
            }
            
            if (uiRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                DrawingSurface surface = hit.collider.GetComponent<DrawingSurface>();
                
                if (surface != null)
                {
                    Debug.Log($"[PhotoPlacementManager] 🎯 Hit DrawingSurface! Placing photo...");
                    PlacePhotoOnSurface(surface, hit.point, hit.normal);
                }
                else
                {
                    Debug.LogWarning($"[PhotoPlacementManager] ❌ Hit {hit.collider.name} but no DrawingSurface!");
                }
            }
            else
            {
                Debug.LogWarning("[PhotoPlacementManager] ❌ No raycast hit detected!");
            }
        }
        
        private void PlacePhotoOnSurface(DrawingSurface surface, Vector3 worldPosition, Vector3 normal)
        {
            GameObject photoObj = new GameObject($"Photo_{selectedPhoto.name}");
            photoObj.transform.SetParent(surface.transform);
            
            float aspectRatio = (float)selectedPhoto.width / selectedPhoto.height;
            float photoWidth = photoDefaultWidth;
            float photoHeight = photoWidth / aspectRatio;
            
            Vector3 localPos = surface.transform.InverseTransformPoint(worldPosition);
            localPos.z = -photoOffsetFromSurface;
            
            photoObj.transform.localPosition = localPos;
            photoObj.transform.localRotation = Quaternion.identity;
            photoObj.transform.localScale = new Vector3(photoWidth, photoHeight, 1f);
            
            MeshFilter meshFilter = photoObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = photoObj.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = CreateQuadMesh();
            
            Material photoMatInstance = new Material(photoMaterial);
            photoMatInstance.mainTexture = selectedPhoto;
            meshRenderer.material = photoMatInstance;
            
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 100;
            
            Debug.Log($"[PhotoPlacementManager] ✅ Photo placed at {localPos}");
            
            ExitPlacementMode();
        }
        
        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0),
                new Vector3(0.5f, 0.5f, 0)
            };
            
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            
            int[] triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }

        private void OnDestroy()
        {
            DisablePlacementInput();
        }
    }
}
