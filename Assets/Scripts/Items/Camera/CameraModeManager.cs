using UnityEngine;
using UnityEngine.InputSystem;
using VRDrawing.Setup;

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

            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            
            if (screenshot == null)
            {
                Debug.LogError("[CameraModeManager] Failed to capture screenshot!");
                yield break;
            }
            
            Texture2D photo = null;
            
            if (screenshot.width != photoWidth || screenshot.height != photoHeight)
            {
                RenderTexture rt = RenderTexture.GetTemporary(photoWidth, photoHeight, 0, RenderTextureFormat.ARGB32);
                RenderTexture currentRT = RenderTexture.active;
                
                Graphics.Blit(screenshot, rt);
                RenderTexture.active = rt;
                
                photo = new Texture2D(photoWidth, photoHeight, TextureFormat.RGB24, false);
                photo.ReadPixels(new Rect(0, 0, photoWidth, photoHeight), 0, 0);
                photo.Apply();
                
                RenderTexture.active = currentRT;
                RenderTexture.ReleaseTemporary(rt);
                
                Destroy(screenshot);
            }
            else
            {
                photo = screenshot;
            }
            
            if (PhotoAttachmentManager.Instance != null)
            {
                PhotoAttachmentManager.Instance.SavePhoto(photo);
            }
            
            if (shutterSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shutterSound);
            }
            
            OnPhotoCaptured?.Invoke(photo);

            // Store the captured photo as the pending board image and enter drawing mode
            PendingCapturePayload.Set(photo);
            if (VRDrawing.Mode.DrawingModeManager.Instance != null)
            {
                ExitCameraMode();
                VRDrawing.Mode.DrawingModeManager.Instance.EnterDrawingMode();
            }
            
            Debug.Log($"[CameraModeManager] Photo captured! {photo.width}x{photo.height}");

            if (viewfinderUI != null && restoreUIState)
            {
                viewfinderUI.SetActive(true);
            }
            
            // Do NOT destroy photo here: PendingCapturePayload holds the reference
            // and PhotoPlacementManager will use it once the board spawns.
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
