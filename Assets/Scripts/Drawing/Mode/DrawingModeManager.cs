using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace VRDrawing.Mode
{
    public class DrawingModeManager : MonoBehaviour
    {
        [Header("Drawing Board")]
        [SerializeField] private GameObject drawingBoardPrefab;
        [SerializeField] private float boardDistance = 1.2f;
        [SerializeField] private float boardHeight = 0.3f;
        [SerializeField] private Vector3 boardScale = new Vector3(1f, 0.7f, 0.1f);

        [Header("Tool Panel")]
        [SerializeField] private GameObject toolPanelPrefab;
        [SerializeField] private Transform toolPanelParent;
        [SerializeField] private float panelDistance = 0.8f;
        [SerializeField] private float panelHeight = -0.2f;

        [Header("Input")]
        [SerializeField] private InputActionProperty toggleToolPanelAction;

        [Header("Locomotion")]
        [SerializeField] private ContinuousMoveProvider continuousMoveProvider;
        [SerializeField] private SnapTurnProvider snapTurnProvider;
        [SerializeField] private ContinuousTurnProvider continuousTurnProvider;
        [SerializeField] private Transform xrOrigin;

        [Header("UI Ray")]
        [SerializeField] private XRRayInteractor uiRayInteractor;
        [SerializeField] private bool autoFindUIRay = true;

        [Header("References")]
        [SerializeField] private Transform playerCamera;

        private GameObject activeDrawingBoard;
        private GameObject activeToolPanel;
        private XRInteractorLineVisual uiRayLineVisual;
        
        private bool isInDrawingMode = false;
        private bool isToolPanelVisible = false;
        private Vector3 lockedPosition;
        private Quaternion lockedRotation;

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
            }
        }

        private void AutoFindLocomotionComponents()
        {
            if (continuousMoveProvider == null)
            {
                continuousMoveProvider = FindFirstObjectByType<ContinuousMoveProvider>();
            }

            if (snapTurnProvider == null)
            {
                snapTurnProvider = FindFirstObjectByType<SnapTurnProvider>();
            }

            if (continuousTurnProvider == null)
            {
                continuousTurnProvider = FindFirstObjectByType<ContinuousTurnProvider>();
            }
        }

        private void OnEnable()
        {
            if (toggleToolPanelAction.action != null)
            {
                toggleToolPanelAction.action.Enable();
                toggleToolPanelAction.action.performed += OnToggleToolPanel;
            }
        }

        private void OnDisable()
        {
            if (toggleToolPanelAction.action != null)
            {
                toggleToolPanelAction.action.performed -= OnToggleToolPanel;
                toggleToolPanelAction.action.Disable();
            }
        }

        private void OnToggleToolPanel(InputAction.CallbackContext context)
        {
            if (!isInDrawingMode) return;
            
            ToggleToolPanel();
        }

        public void EnterDrawingMode()
        {
            if (isInDrawingMode) return;

            isInDrawingMode = true;

            LockUserPosition();
            DisableLocomotion();
            SpawnDrawingBoard();

            OnDrawingModeEntered?.Invoke();
        }

        public void ExitDrawingMode()
        {
            if (!isInDrawingMode) return;

            isInDrawingMode = false;

            HideToolPanel();
            DespawnDrawingBoard();
            EnableLocomotion();
            UnlockUserPosition();

            OnDrawingModeExited?.Invoke();
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
            if (continuousMoveProvider != null)
            {
                continuousMoveProvider.enabled = false;
            }

            if (snapTurnProvider != null)
            {
                snapTurnProvider.enabled = false;
            }

            if (continuousTurnProvider != null)
            {
                continuousTurnProvider.enabled = false;
            }
        }

        private void EnableLocomotion()
        {
            if (continuousMoveProvider != null)
            {
                continuousMoveProvider.enabled = true;
            }

            if (snapTurnProvider != null)
            {
                snapTurnProvider.enabled = true;
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
        }

        private void DespawnDrawingBoard()
        {
            if (activeDrawingBoard != null)
            {
                Destroy(activeDrawingBoard);
                activeDrawingBoard = null;
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

        public void ToggleToolPanel()
        {
            if (isToolPanelVisible)
            {
                HideToolPanel();
            }
            else
            {
                ShowToolPanel();
            }
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
                Vector3 panelPosition = CalculatePanelPosition();
                Quaternion panelRotation = CalculatePanelRotation();

                Transform parent = toolPanelParent != null ? toolPanelParent : null;
                activeToolPanel = Instantiate(toolPanelPrefab, panelPosition, panelRotation, parent);
            }

            activeToolPanel.SetActive(true);
            isToolPanelVisible = true;
            UpdateUIRayVisibility(true);
        }

        public void HideToolPanel()
        {
            if (activeToolPanel != null)
            {
                activeToolPanel.SetActive(false);
            }

            isToolPanelVisible = false;
            UpdateUIRayVisibility(false);
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

        private void UpdateUIRayVisibility(bool visible)
        {
            if (uiRayInteractor != null)
            {
                uiRayInteractor.enabled = visible;

                if (uiRayLineVisual == null)
                {
                    uiRayLineVisual = uiRayInteractor.GetComponent<XRInteractorLineVisual>();
                }

                if (uiRayLineVisual != null)
                {
                    uiRayLineVisual.enabled = visible;
                }
            }
        }

        public void OnToolSelected()
        {
            HideToolPanel();
        }
    }
}
