using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace VRDrawing.Mode
{
    public class DrawingModeManager : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject drawingBoardPrefab;
        [SerializeField] private GameObject toolPanelPrefab;

        [Header("Drawing Board Settings")]
        [SerializeField] private float boardDistance = 1.5f;
        [SerializeField] private float boardHeight = 0.5f;
        [SerializeField] private Vector3 boardScale = Vector3.one;

        [Header("Tool Panel Settings")]
        [SerializeField] private Transform toolPanelParent;
        [SerializeField] private float panelDistance = 0.8f;
        [SerializeField] private float panelHeight = 0.3f;

        [Header("Locomotion")]
        [SerializeField] private TeleportationProvider teleportationProvider;
        [SerializeField] private ContinuousMoveProvider continuousMoveProvider;
        [SerializeField] private ContinuousTurnProvider continuousTurnProvider;
        [SerializeField] private Transform xrOrigin;

        [Header("UI Ray Settings")]
        [SerializeField] private XRRayInteractor uiRayInteractor;
        [SerializeField] private bool autoFindUIRay = true;

        [Header("Other References")]
        [SerializeField] private Transform playerCamera;

        [Header("Cached Components")]
        private VRDrawing.Tools.UIRayDrawingTool cachedUIRayDrawingTool;
        private VRDrawing.DrawingSystemManager cachedDrawingSystemManager;

        private GameObject activeDrawingBoard;
        private GameObject activeToolPanel;
        private bool isInDrawingMode = false;
        private Vector3 lockedPosition;
        private Quaternion lockedRotation;
        private XRInteractorLineVisual uiRayLineVisual;

        public static DrawingModeManager Instance { get; private set; }

        public bool IsInDrawingMode => isInDrawingMode;
        public GameObject ActiveDrawingBoard => activeDrawingBoard;

        public System.Action OnDrawingModeEntered;
        public System.Action OnDrawingModeExited;

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

            if (playerCamera == null)
            {
                playerCamera = Camera.main.transform;
            }

            if (xrOrigin == null)
            {
                xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>()?.transform;
            }

            AutoFindLocomotionComponents();

            if (autoFindUIRay && uiRayInteractor == null)
            {
                GameObject uiRayObj = GameObject.Find("UI Ray Interactor");
                if (uiRayObj != null)
                {
                    uiRayInteractor = uiRayObj.GetComponent<XRRayInteractor>();
                }
            }

            if (uiRayInteractor != null)
            {
                uiRayLineVisual = uiRayInteractor.GetComponent<XRInteractorLineVisual>();
                
                // CACHE UIRayDrawingTool
                cachedUIRayDrawingTool = uiRayInteractor.GetComponent<VRDrawing.Tools.UIRayDrawingTool>();
            }
            
            // CACHE DrawingSystemManager
            cachedDrawingSystemManager = FindFirstObjectByType<VRDrawing.DrawingSystemManager>();

            DisableAllDrawingComponents();
        }

        private void AutoFindLocomotionComponents()
        {
            if (teleportationProvider == null)
            {
                teleportationProvider = FindFirstObjectByType<TeleportationProvider>();
            }

            if (continuousMoveProvider == null)
            {
                continuousMoveProvider = FindFirstObjectByType<ContinuousMoveProvider>();
            }

            if (continuousTurnProvider == null)
            {
                continuousTurnProvider = FindFirstObjectByType<ContinuousTurnProvider>();
            }
        }

        public void EnterDrawingMode()
        {
            Debug.Log("[DrawingModeManager] Enter Drawing Mode");
            if (isInDrawingMode) return;

            isInDrawingMode = true;

            LockUserPosition();
            DisableLocomotion();
            SpawnDrawingBoard();
            ShowToolPanel();

            EnableAllDrawingComponents();

            OnDrawingModeEntered?.Invoke();
        }

        public void ExitDrawingMode()
        {
            Debug.Log("[DrawingModeManager] Exit Drawing Mode");
            if (!isInDrawingMode) return;

            isInDrawingMode = false;

            HideToolPanel();
            DespawnDrawingBoard();
            EnableLocomotion();
            UnlockUserPosition();

            DisableAllDrawingComponents();

            OnDrawingModeExited?.Invoke();
        }

        private void DisableAllDrawingComponents()
        {
            Debug.Log("[DrawingModeManager] Disabling all drawing components...");

            if (uiRayInteractor != null)
            {
                uiRayInteractor.enabled = false;
                Debug.Log("[DrawingModeManager] ✓ UI Ray Interactor disabled");
            }

            if (uiRayLineVisual != null)
            {
                uiRayLineVisual.enabled = false;
                Debug.Log("[DrawingModeManager] ✓ Line Visual disabled");
            }

            // SỬ DỤNG CACHED REFERENCE thay vì FindObjectsByType
            if (cachedUIRayDrawingTool != null)
            {
                cachedUIRayDrawingTool.enabled = false;
                Debug.Log($"[DrawingModeManager] ✓ UIRayDrawingTool disabled");
            }

            if (cachedDrawingSystemManager != null)
            {
                cachedDrawingSystemManager.enabled = false;
                Debug.Log("[DrawingModeManager] ✓ DrawingSystemManager disabled");
            }

            Debug.Log("[DrawingModeManager] All drawing components disabled");
        }

        private void EnableAllDrawingComponents()
        {
            Debug.Log("[DrawingModeManager] Enabling all drawing components...");

            if (uiRayInteractor != null)
            {
                uiRayInteractor.enabled = true;
                Debug.Log("[DrawingModeManager] ✓ UI Ray Interactor enabled");
            }

            if (uiRayLineVisual != null)
            {
                uiRayLineVisual.enabled = true;
                Debug.Log("[DrawingModeManager] ✓ Line Visual enabled");
            }

            if (cachedUIRayDrawingTool != null)
            {
                cachedUIRayDrawingTool.enabled = true;
                Debug.Log($"[DrawingModeManager] ✓ UIRayDrawingTool enabled");
            }

            if (cachedDrawingSystemManager != null)
            {
                cachedDrawingSystemManager.enabled = true;
                Debug.Log("[DrawingModeManager] ✓ DrawingSystemManager enabled");
            }

            Debug.Log("[DrawingModeManager] All drawing components enabled");
        }

        private void LockUserPosition()
        {
            if (xrOrigin != null)
            {
                lockedPosition = xrOrigin.position;
                lockedRotation = xrOrigin.rotation;
            }
        }

        private void UnlockUserPosition()
        {
        }

        private void Update()
        {
            if (isInDrawingMode && xrOrigin != null)
            {
                xrOrigin.position = lockedPosition;
                xrOrigin.rotation = lockedRotation;
            }
        }

        private void DisableLocomotion()
        {
            if (teleportationProvider != null)
            {
                teleportationProvider.enabled = false;
            }

            if (continuousMoveProvider != null)
            {
                continuousMoveProvider.enabled = false;
            }

            if (continuousTurnProvider != null)
            {
                continuousTurnProvider.enabled = false;
            }
        }

        private void EnableLocomotion()
        {
            if (teleportationProvider != null)
            {
                teleportationProvider.enabled = true;
            }

            if (continuousMoveProvider != null)
            {
                continuousMoveProvider.enabled = true;
            }

            if (continuousTurnProvider != null)
            {
                continuousTurnProvider.enabled = true;
            }
        }

        private void SpawnDrawingBoard()
        {
            if (drawingBoardPrefab == null)
            {
                Debug.LogError("DrawingModeManager: Drawing board prefab not assigned");
                return;
            }

            if (activeDrawingBoard != null)
            {
                Destroy(activeDrawingBoard);
            }

            Vector3 spawnPosition = CalculateBoardPosition();
            Quaternion spawnRotation = CalculateBoardRotation();

            activeDrawingBoard = Instantiate(drawingBoardPrefab, spawnPosition, spawnRotation);
            activeDrawingBoard.transform.localScale = boardScale;
            activeDrawingBoard.SetActive(true);
            
            Debug.Log($"[DrawingModeManager] Drawing board spawned at {spawnPosition}");
        }

        private void DespawnDrawingBoard()
        {
            if (activeDrawingBoard != null)
            {
                Destroy(activeDrawingBoard);
                activeDrawingBoard = null;
                Debug.Log("[DrawingModeManager] Drawing board despawned");
            }
        }

        private Vector3 CalculateBoardPosition()
        {
            Vector3 forward = playerCamera.forward;
            forward.y = 0f;
            forward.Normalize();

            return playerCamera.position + forward * boardDistance + Vector3.up * boardHeight;
        }

        private Quaternion CalculateBoardRotation()
        {
            Vector3 forward = playerCamera.forward;
            forward.y = 0f;
            forward.Normalize();

            return Quaternion.LookRotation(forward);
        }

        public void ShowToolPanel()
        {
            if (!isInDrawingMode) return;

            if (toolPanelPrefab == null)
            {
                Debug.LogError("DrawingModeManager: Tool panel prefab not assigned");
                return;
            }

            if (activeToolPanel == null)
            {
                Transform parent = toolPanelParent != null ? toolPanelParent : null;
                activeToolPanel = Instantiate(toolPanelPrefab, parent);
            }

            Vector3 panelPosition = CalculatePanelPosition();
            Quaternion panelRotation = CalculatePanelRotation();
            
            activeToolPanel.transform.position = panelPosition;
            activeToolPanel.transform.rotation = panelRotation;
            activeToolPanel.SetActive(true);
            
            Debug.Log($"[DrawingModeManager] Tool panel shown at {panelPosition}");
        }

        public void HideToolPanel()
        {
            if (activeToolPanel != null)
            {
                activeToolPanel.SetActive(false);
                Debug.Log("[DrawingModeManager] Tool panel hidden");
            }
        }

        private Vector3 CalculatePanelPosition()
        {
            Vector3 forward = playerCamera.forward;
            forward.y = 0f;
            forward.Normalize();

            return playerCamera.position + forward * panelDistance + Vector3.up * panelHeight;
        }

        private Quaternion CalculatePanelRotation()
        {
            Vector3 forward = playerCamera.forward;
            forward.y = 0f;
            forward.Normalize();

            return Quaternion.LookRotation(forward);
        }
    }
}
