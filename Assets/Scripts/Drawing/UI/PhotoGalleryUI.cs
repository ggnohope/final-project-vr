using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace VRDrawing.UI
{
    public class PhotoGalleryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject galleryPanel;
        [SerializeField] private Button toggleGalleryButton;
        [SerializeField] private Transform photoGridContent;
        [SerializeField] private GameObject photoButtonPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [SerializeField] private Vector2 thumbnailSize = new Vector2(100, 100);
        [SerializeField] private int maxPhotosToDisplay = 20;

        [Header("Positioning Settings")]
        [SerializeField] private float panelDistance = 1.0f;
        [SerializeField] private float panelHeight = 0.2f;
        [SerializeField] private bool repositionOnOpen = true;
        

        private List<GameObject> photoButtons = new List<GameObject>();
        public static PhotoGalleryUI Instance { get; private set; }

        private bool isGalleryOpen = false;

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
            
            SetupButtons();
            
            if (galleryPanel != null)
            {
                galleryPanel.SetActive(false);
            }
        }


        private void OnEnable()
        {
            if (VRItems.Camera.PhotoAttachmentManager.Instance != null)
            {
                VRItems.Camera.PhotoAttachmentManager.Instance.OnPhotosUpdated += RefreshGallery;
            }
        }

        private void OnDisable()
        {
            if (VRItems.Camera.PhotoAttachmentManager.Instance != null)
            {
                VRItems.Camera.PhotoAttachmentManager.Instance.OnPhotosUpdated -= RefreshGallery;
            }
        }

        private void SetupButtons()
        {
            if (toggleGalleryButton != null)
            {
                toggleGalleryButton.onClick.AddListener(ToggleGallery);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(RefreshGallery);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseGallery);
            }
        }

        public void ToggleGallery()
        {
            if (isGalleryOpen)
            {
                CloseGallery();
            }
            else
            {
                OpenGallery();
            }
        }

        private void OpenGallery()
        {
            isGalleryOpen = true;
            
            if (galleryPanel != null)
            {
                PositionPanelInFrontOfCamera();
                galleryPanel.SetActive(true);
            }

            RefreshGallery();
            
            Debug.Log("[PhotoGalleryUI] Gallery opened");
        }

        private void CloseGallery()
        {
            isGalleryOpen = false;
            
            if (galleryPanel != null)
            {
                galleryPanel.SetActive(false);
            }

            Debug.Log("[PhotoGalleryUI] Gallery closed");
        }

        private void PositionPanelInFrontOfCamera()
        {
            if (!repositionOnOpen || galleryPanel == null)
                return;

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogWarning("[PhotoGalleryUI] Main camera not found");
                return;
            }

            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 targetPosition = playerCamera.transform.position + forward * panelDistance + Vector3.up * panelHeight;
            
            galleryPanel.transform.position = targetPosition;
            galleryPanel.transform.LookAt(playerCamera.transform.position);
            galleryPanel.transform.Rotate(0, 180, 0);
            
            Debug.Log($"[PhotoGalleryUI] Positioned panel at {targetPosition}, facing camera");
        }

        private void RefreshGallery()
        {
            ClearPhotoButtons();

            if (VRItems.Camera.PhotoAttachmentManager.Instance == null)
            {
                Debug.LogWarning("[PhotoGalleryUI] PhotoAttachmentManager not found");
                return;
            }

            List<Texture2D> photos = VRItems.Camera.PhotoAttachmentManager.Instance.GetAllPhotos();
            
            Debug.Log($"[PhotoGalleryUI] Retrieved {photos.Count} photos from PhotoAttachmentManager");
            
            int count = Mathf.Min(photos.Count, maxPhotosToDisplay);
            
            for (int i = 0; i < count; i++)
            {
                CreatePhotoButton(photos[i]);
            }

            Debug.Log($"[PhotoGalleryUI] Refreshed gallery with {count} photos");
        }


        private void CreatePhotoButton(Texture2D photo)
        {
            if (photoButtonPrefab == null || photoGridContent == null || photo == null)
            {
                return;
            }

            GameObject buttonObj = Instantiate(photoButtonPrefab, photoGridContent);
            photoButtons.Add(buttonObj);

            RawImage thumbnail = buttonObj.GetComponentInChildren<RawImage>();
            if (thumbnail != null)
            {
                thumbnail.texture = photo;

                // RawImage uses stretch anchors (0,0)-(1,1) in the prefab, so sizeDelta must
                // remain zero to fill the GridLayoutGroup cell correctly. Setting a non-zero
                // sizeDelta would make the image larger than its cell, causing overlap.
                RectTransform rt = thumbnail.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = Vector2.zero;
                }
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnPhotoSelected(photo));
            }
        }

        private void OnPhotoSelected(Texture2D photo)
        {
            if (VRDrawing.Photo.PhotoPlacementManager.Instance != null)
            {
                VRDrawing.Photo.PhotoPlacementManager.Instance.EnterPlacementMode(photo);
                Debug.Log($"[PhotoGalleryUI] Photo '{photo.name}' selected - Click on board to place");
            }

            CloseGallery();
        }

        private void ClearPhotoButtons()
        {
            foreach (GameObject button in photoButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            
            photoButtons.Clear();
        }
    }
}
