using UnityEngine;
using UnityEngine.InputSystem;

namespace VRItems.Camera
{
    public class CameraModeManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject viewfinderUI;
        
        [Header("Input")]
        [SerializeField] private InputActionProperty capturePhotoAction;
        
        [Header("Capture Settings")]
        [SerializeField] private int photoWidth = 1920;
        [SerializeField] private int photoHeight = 1080;
        [SerializeField] private AudioClip shutterSound;
        
        private AudioSource audioSource;
        private bool isInCameraMode = false;
        
        public static CameraModeManager Instance { get; private set; }
        
        public bool IsInCameraMode => isInCameraMode;

        [Header("Zoom Settings")]
        [SerializeField] private InputActionProperty zoomAction;
        [SerializeField] private float minFOV = 10f;
        [SerializeField] private float maxFOV = 120f;
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float zoomSmoothness = 5f;

        private float targetFOV;
        private float currentFOV;
        
        public System.Action<Texture2D> OnPhotoCaptured;
        public System.Action OnCameraModeEntered;
        public System.Action OnCameraModeExited;
        
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
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            if (viewfinderUI != null)
            {
                viewfinderUI.SetActive(false);
            }
        }

        private void Start()
        {
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam != null)
            {
                defaultFOV = mainCam.fieldOfView;
                currentFOV = defaultFOV;
                targetFOV = defaultFOV;
                Debug.Log($"[CameraModeManager] Initialized with camera FOV: {defaultFOV}°");
            }
        }
        
        public void EnterCameraMode()
        {
            if (isInCameraMode) return;
            
            isInCameraMode = true;
            
            // ENABLE input actions CHỈ KHI VÀO MODE
            EnableCameraInputs();
            
            if (VRDrawing.Mode.DrawingModeManager.Instance != null && 
                VRDrawing.Mode.DrawingModeManager.Instance.IsInDrawingMode)
            {
                VRDrawing.Mode.DrawingModeManager.Instance.ExitDrawingMode();
                Debug.Log("[CameraModeManager] Exited Drawing Mode to enter Camera Mode");
            }

            ResetZoom();
            
            if (viewfinderUI != null)
            {
                viewfinderUI.SetActive(true);
            }
            
            OnCameraModeEntered?.Invoke();
            
            Debug.Log("[CameraModeManager] ✅ Entered Camera Mode - Inputs ENABLED");
        }
        
        public void ExitCameraMode()
        {
            if (!isInCameraMode) return;
            
            isInCameraMode = false;
            
            // DISABLE input actions KHI THOÁT MODE
            DisableCameraInputs();
            
            if (viewfinderUI != null)
            {
                viewfinderUI.SetActive(false);
            }
            ResetZoom();
            OnCameraModeExited?.Invoke();
            
            Debug.Log("[CameraModeManager] ❌ Exited Camera Mode - Inputs DISABLED");
        }

        private void EnableCameraInputs()
        {
            if (capturePhotoAction.action != null)
            {
                capturePhotoAction.action.Enable();
                capturePhotoAction.action.performed += OnCapturePhoto;
            }

            if (zoomAction.action != null)
            {
                zoomAction.action.Enable();
            }
        }

        private void DisableCameraInputs()
        {
            if (capturePhotoAction.action != null)
            {
                capturePhotoAction.action.performed -= OnCapturePhoto;
                capturePhotoAction.action.Disable();
            }
            
            if (zoomAction.action != null)
            {
                zoomAction.action.Disable();
            }
        }

        private void ResetZoom()
        {
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null) return;
            
            targetFOV = defaultFOV;
            currentFOV = defaultFOV;
            mainCam.fieldOfView = defaultFOV;
        }
        
        private void OnCapturePhoto(InputAction.CallbackContext context)
        {
            CapturePhoto();
        }
        
        private void CapturePhoto()
        {
            UnityEngine.Camera captureCamera = UnityEngine.Camera.main;
            if (captureCamera == null)
            {
                Debug.LogError("[CameraModeManager] Main Camera not found!");
                return;
            }

            bool wasUIActive = false;
            if (viewfinderUI != null)
            {
                wasUIActive = viewfinderUI.activeSelf;
                viewfinderUI.SetActive(false);
            }

            StartCoroutine(CapturePhotoCoroutine(captureCamera, wasUIActive));
        }

        private System.Collections.IEnumerator CapturePhotoCoroutine(UnityEngine.Camera captureCamera, bool restoreUIState)
        {
            yield return new WaitForEndOfFrame();

            // Render trực tiếp từ camera vào RenderTexture với độ phân giải cao
            RenderTexture renderRT = new RenderTexture(photoWidth, photoHeight, 24, RenderTextureFormat.ARGB32);
            renderRT.antiAliasing = 4;
            renderRT.filterMode = FilterMode.Trilinear;
            renderRT.anisoLevel = 8;

            RenderTexture previousRT = captureCamera.targetTexture;
            captureCamera.targetTexture = renderRT;
            captureCamera.Render();
            captureCamera.targetTexture = previousRT;

            RenderTexture currentActive = RenderTexture.active;
            RenderTexture.active = renderRT;

            Texture2D photo = new Texture2D(photoWidth, photoHeight, TextureFormat.RGB24, false);
            photo.ReadPixels(new Rect(0, 0, photoWidth, photoHeight), 0, 0);
            photo.Apply();

            RenderTexture.active = currentActive;
            renderRT.Release();
            Destroy(renderRT);

            if (PhotoAttachmentManager.Instance != null)
            {
                PhotoAttachmentManager.Instance.SavePhoto(photo);
            }

            if (shutterSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shutterSound);
            }

            OnPhotoCaptured?.Invoke(photo);

            Debug.Log($"[CameraModeManager] Photo captured! {photo.width}x{photo.height}");

            if (viewfinderUI != null && restoreUIState)
            {
                viewfinderUI.SetActive(true);
            }

            Destroy(photo);
        }

        // CHỈ Update KHI ĐANG Ở CAMERA MODE
        private void Update()
        {
            if (!isInCameraMode) return;
            
            HandleZoom();
        }

        private void HandleZoom()
        {
            if (zoomAction.action == null) return;
            
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null) return;
            
            float zoomInput = zoomAction.action.ReadValue<Vector2>().y;
            
            targetFOV -= zoomInput * zoomSpeed * Time.deltaTime;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
            
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSmoothness);
            mainCam.fieldOfView = currentFOV;
        }
        
        private void OnDestroy()
        {
            DisableCameraInputs();
        }
    }
}
