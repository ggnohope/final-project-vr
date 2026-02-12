using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRItems.Camera
{
    public class VRCameraController : MonoBehaviour
    {
        [Header("Camera Prefab")]
        [SerializeField] private GameObject cameraPrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnDistance = 0.5f;
        
        [Header("Input")]
        [SerializeField] private InputActionProperty toggleCameraAction;
        
        private GameObject activeCameraObject;
        private bool isCameraActive = false;
        
        private void OnEnable()
        {
            if (toggleCameraAction.action != null)
            {
                toggleCameraAction.action.Enable();
                toggleCameraAction.action.performed += OnToggleCamera;
            }
        }
        
        private void OnDisable()
        {
            if (toggleCameraAction.action != null)
            {
                toggleCameraAction.action.performed -= OnToggleCamera;
                toggleCameraAction.action.Disable();
            }
        }
        
        private void OnToggleCamera(InputAction.CallbackContext context)
        {
            if (isCameraActive)
            {
                DespawnCamera();
            }
            else
            {
                SpawnCamera();
            }
        }
        
        private void SpawnCamera()
        {
            if (cameraPrefab == null)
            {
                Debug.LogError("[VRCameraController] Camera prefab is not assigned!");
                return;
            }
            
            if (activeCameraObject != null)
            {
                Debug.LogWarning("[VRCameraController] Camera already spawned!");
                return;
            }
            
            Vector3 spawnPosition = spawnPoint != null 
                ? spawnPoint.position + spawnPoint.forward * spawnDistance
                : transform.position + transform.forward * spawnDistance;
            
            Quaternion spawnRotation = spawnPoint != null 
                ? spawnPoint.rotation 
                : transform.rotation;
            
            activeCameraObject = Instantiate(cameraPrefab, spawnPosition, spawnRotation);
            isCameraActive = true;
            
            Debug.Log("[VRCameraController] Camera spawned!");
        }
        
        private void DespawnCamera()
        {
            if (activeCameraObject != null)
            {
                Destroy(activeCameraObject);
                activeCameraObject = null;
                isCameraActive = false;
                
                Debug.Log("[VRCameraController] Camera despawned!");
            }
        }
        
        public bool IsCameraActive => isCameraActive;
        public GameObject ActiveCamera => activeCameraObject;
    }
}
