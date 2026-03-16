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
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button closeButton;

        [Header("Delete Mode UI")]
        [SerializeField] private Button confirmDeleteButton;
        [SerializeField] private Button cancelDeleteButton;
        [SerializeField] private GameObject deleteActionsBar;

        [Header("Settings")]
        [SerializeField] private Vector2 thumbnailSize = new Vector2(100, 100);
        [SerializeField] private int maxPhotosToDisplay = 20;

        [Header("Positioning Settings")]
        [SerializeField] private float panelDistance = 1.0f;
        [SerializeField] private float panelHeight = 0.2f;
        [SerializeField] private bool repositionOnOpen = true;

        [Header("Selection Visuals")]
        [SerializeField] private Color selectedOverlayColor = new Color(1f, 0.3f, 0.3f, 0.5f);

        private List<GameObject> photoButtons = new List<GameObject>();
        private List<Texture2D> currentPhotos = new List<Texture2D>();
        private HashSet<int> selectedIndices = new HashSet<int>();
        private bool isDeleteMode = false;

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
                galleryPanel.SetActive(false);

            SetDeleteActionsBarVisible(false);
        }

        private void OnEnable()
        {
            if (VRItems.Camera.PhotoAttachmentManager.Instance != null)
                VRItems.Camera.PhotoAttachmentManager.Instance.OnPhotosUpdated += RefreshGallery;
        }

        private void OnDisable()
        {
            if (VRItems.Camera.PhotoAttachmentManager.Instance != null)
                VRItems.Camera.PhotoAttachmentManager.Instance.OnPhotosUpdated -= RefreshGallery;
        }

        private void SetupButtons()
        {
            if (toggleGalleryButton != null)
                toggleGalleryButton.onClick.AddListener(ToggleGallery);

            if (deleteButton != null)
                deleteButton.onClick.AddListener(EnterDeleteMode);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseGallery);

            if (confirmDeleteButton != null)
                confirmDeleteButton.onClick.AddListener(ConfirmDelete);

            if (cancelDeleteButton != null)
                cancelDeleteButton.onClick.AddListener(ExitDeleteMode);
        }

        /// <summary>Toggles the gallery panel open or closed.</summary>
        public void ToggleGallery()
        {
            if (isGalleryOpen)
                CloseGallery();
            else
                OpenGallery();
        }

        private void OpenGallery()
        {
            isGalleryOpen = true;
            SetDrawingToolEnabled(false);

            if (galleryPanel != null)
            {
                PositionPanelInFrontOfCamera();
                galleryPanel.SetActive(true);
            }

            ExitDeleteMode();
            RefreshGallery();
        }

        private void CloseGallery()
        {
            isGalleryOpen = false;
            ExitDeleteMode();

            if (galleryPanel != null)
                galleryPanel.SetActive(false);

            SetDrawingToolEnabled(true);
        }

        /// <summary>Enters delete-selection mode — photos become selectable for deletion.</summary>
        private void EnterDeleteMode()
        {
            isDeleteMode = true;
            selectedIndices.Clear();
            SetDeleteActionsBarVisible(true);
            RefreshSelectionVisuals();
        }

        /// <summary>Exits delete-selection mode without deleting anything.</summary>
        private void ExitDeleteMode()
        {
            isDeleteMode = false;
            selectedIndices.Clear();
            SetDeleteActionsBarVisible(false);
            RefreshSelectionVisuals();
        }

        private void ConfirmDelete()
        {
            if (selectedIndices.Count == 0)
            {
                ExitDeleteMode();
                return;
            }

            List<Texture2D> toDelete = new List<Texture2D>();
            foreach (int idx in selectedIndices)
            {
                if (idx >= 0 && idx < currentPhotos.Count)
                    toDelete.Add(currentPhotos[idx]);
            }

            VRItems.Camera.PhotoAttachmentManager.Instance?.DeletePhotos(toDelete);

            ExitDeleteMode();
            RefreshGallery();
        }

        private void OnPhotoButtonClicked(int index, Texture2D photo)
        {
            if (isDeleteMode)
            {
                TogglePhotoSelection(index);
            }
            else
            {
                if (VRDrawing.Photo.PhotoPlacementManager.Instance != null)
                    VRDrawing.Photo.PhotoPlacementManager.Instance.EnterPlacementMode(photo);

                CloseGallery();
            }
        }

        private void TogglePhotoSelection(int index)
        {
            if (selectedIndices.Contains(index))
                selectedIndices.Remove(index);
            else
                selectedIndices.Add(index);

            RefreshSelectionVisuals();
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < photoButtons.Count; i++)
            {
                if (photoButtons[i] == null) continue;

                Transform overlayTransform = photoButtons[i].transform.Find("SelectionOverlay");
                if (overlayTransform != null)
                    overlayTransform.gameObject.SetActive(isDeleteMode && selectedIndices.Contains(i));
            }
        }

        private void SetDeleteActionsBarVisible(bool visible)
        {
            if (deleteActionsBar != null)
                deleteActionsBar.SetActive(visible);

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(!visible);
        }

        private void RefreshGallery()
        {
            ClearPhotoButtons();

            if (VRItems.Camera.PhotoAttachmentManager.Instance == null)
                return;

            currentPhotos = VRItems.Camera.PhotoAttachmentManager.Instance.GetAllPhotos();
            int count = Mathf.Min(currentPhotos.Count, maxPhotosToDisplay);

            for (int i = 0; i < count; i++)
                CreatePhotoButton(i, currentPhotos[i]);
        }

        private void CreatePhotoButton(int index, Texture2D photo)
        {
            if (photoButtonPrefab == null || photoGridContent == null || photo == null)
                return;

            GameObject buttonObj = Instantiate(photoButtonPrefab, photoGridContent);
            photoButtons.Add(buttonObj);

            RawImage thumbnail = buttonObj.GetComponentInChildren<RawImage>();
            if (thumbnail != null)
            {
                thumbnail.texture = photo;

                // RawImage uses stretch anchors (0,0)-(1,1) in the prefab, so sizeDelta must
                // remain zero to fill the GridLayoutGroup cell correctly.
                RectTransform rt = thumbnail.GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = Vector2.zero;
            }

            EnsureSelectionOverlay(buttonObj);

            int capturedIndex = index;
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => OnPhotoButtonClicked(capturedIndex, photo));
        }

        private void EnsureSelectionOverlay(GameObject buttonObj)
        {
            Transform existing = buttonObj.transform.Find("SelectionOverlay");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                return;
            }

            GameObject overlay = new GameObject("SelectionOverlay");
            overlay.transform.SetParent(buttonObj.transform, false);

            RectTransform overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = selectedOverlayColor;
            overlayImage.raycastTarget = false;

            overlay.SetActive(false);
        }

        private void PositionPanelInFrontOfCamera()
        {
            if (!repositionOnOpen || galleryPanel == null)
                return;

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
                return;

            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 targetPosition = playerCamera.transform.position + forward * panelDistance + Vector3.up * panelHeight;

            galleryPanel.transform.position = targetPosition;
            galleryPanel.transform.LookAt(playerCamera.transform.position);
            galleryPanel.transform.Rotate(0, 180, 0);
        }

        /// <summary>Enables or disables the UIRayDrawingTool so it does not draw while the gallery is open.</summary>
        private void SetDrawingToolEnabled(bool enabled)
        {
            VRDrawing.Tools.UIRayDrawingTool drawingTool = FindFirstObjectByType<VRDrawing.Tools.UIRayDrawingTool>();
            if (drawingTool != null)
                drawingTool.SetEnabled(enabled);
        }

        private void ClearPhotoButtons()
        {
            foreach (GameObject button in photoButtons)
            {
                if (button != null)
                    Destroy(button);
            }

            photoButtons.Clear();
            currentPhotos.Clear();
            selectedIndices.Clear();
        }
    }
}
