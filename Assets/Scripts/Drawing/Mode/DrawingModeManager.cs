using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

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

        // Fallback: any LocomotionProvider-derived components found in scene
        private LocomotionProvider[] allLocomotionProviders;
        [SerializeField] private Transform xrOrigin;

        [Header("UI Ray Settings")]
        [SerializeField] private XRRayInteractor uiRayInteractor;
        [SerializeField] private bool autoFindUIRay = true;

        [Header("Other References")]
        [SerializeField] private Transform playerCamera;

        [Header("Annotation Canvases")]
        [SerializeField] private GameObject symbolPaletteCanvasPrefab;
        [SerializeField] private GameObject annotationLegendCanvasPrefab;

        private GameObject activeSymbolPaletteCanvas;
        private GameObject activeAnnotationLegendCanvas;

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
                teleportationProvider = FindFirstObjectByType<TeleportationProvider>();

            if (continuousMoveProvider == null)
                continuousMoveProvider = FindFirstObjectByType<ContinuousMoveProvider>();

            if (continuousTurnProvider == null)
                continuousTurnProvider = FindFirstObjectByType<ContinuousTurnProvider>();

            // Cache ALL locomotion providers as fallback for DynamicMoveProvider / SnapTurnProvider etc.
            allLocomotionProviders = FindObjectsByType<LocomotionProvider>(FindObjectsSortMode.None);
        }

        public void EnterDrawingMode()
        {
            if (isInDrawingMode) return;

            isInDrawingMode = true;

            LockUserPosition();
            DisableLocomotion();
            SpawnDrawingBoard();
            ShowToolPanel();
            SpawnAnnotationCanvases();
            EnableAllDrawingComponents();

            OnDrawingModeEntered?.Invoke();
        }

        public void ExitDrawingMode()
        {
            if (!isInDrawingMode) return;

            isInDrawingMode = false;

            HideToolPanel();
            DespawnAnnotationCanvases();
            DespawnDrawingBoard();
            EnableLocomotion();
            UnlockUserPosition();
            DisableAllDrawingComponents();

            OnDrawingModeExited?.Invoke();
        }

        private void DisableAllDrawingComponents()
        {
            if (uiRayInteractor != null)
                uiRayInteractor.enabled = false;

            if (uiRayLineVisual != null)
                uiRayLineVisual.enabled = false;

            if (cachedUIRayDrawingTool != null)
                cachedUIRayDrawingTool.enabled = false;

            if (cachedDrawingSystemManager != null)
                cachedDrawingSystemManager.enabled = false;
        }

        private void EnableAllDrawingComponents()
        {
            if (uiRayInteractor != null)
                uiRayInteractor.enabled = true;

            if (uiRayLineVisual != null)
                uiRayLineVisual.enabled = true;

            if (cachedUIRayDrawingTool != null)
                cachedUIRayDrawingTool.enabled = true;

            if (cachedDrawingSystemManager != null)
                cachedDrawingSystemManager.enabled = true;
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
            if (teleportationProvider != null) teleportationProvider.enabled = false;
            if (continuousMoveProvider != null) continuousMoveProvider.enabled = false;
            if (continuousTurnProvider != null) continuousTurnProvider.enabled = false;

            // Disable any additional locomotion providers (e.g. DynamicMoveProvider, SnapTurnProvider)
            if (allLocomotionProviders != null)
                foreach (var p in allLocomotionProviders)
                    if (p != null) p.enabled = false;
        }

        private void EnableLocomotion()
        {
            if (teleportationProvider != null) teleportationProvider.enabled = true;
            if (continuousMoveProvider != null) continuousMoveProvider.enabled = true;
            if (continuousTurnProvider != null) continuousTurnProvider.enabled = true;

            if (allLocomotionProviders != null)
                foreach (var p in allLocomotionProviders)
                    if (p != null) p.enabled = true;
        }

        private void SpawnAnnotationCanvases()
        {
            if (symbolPaletteCanvasPrefab != null && activeSymbolPaletteCanvas == null)
            {
                activeSymbolPaletteCanvas = Instantiate(symbolPaletteCanvasPrefab);
                activeSymbolPaletteCanvas.SetActive(true);

                // Position after board is ready. OnEnable fires before ActiveDrawingBoard is set,
                // so we call PositionNextToBoard explicitly here.
                VRDrawing.Geology.UI.SymbolPaletteUI palette =
                    activeSymbolPaletteCanvas.GetComponentInChildren<VRDrawing.Geology.UI.SymbolPaletteUI>(true);
                palette?.PositionNextToBoard();
            }

            if (annotationLegendCanvasPrefab != null && activeAnnotationLegendCanvas == null)
            {
                activeAnnotationLegendCanvas = Instantiate(annotationLegendCanvasPrefab);
                activeAnnotationLegendCanvas.SetActive(true);

                VRDrawing.Geology.UI.AnnotationLegendUI legend =
                    activeAnnotationLegendCanvas.GetComponentInChildren<VRDrawing.Geology.UI.AnnotationLegendUI>(true);
                legend?.PositionNextToBoard();
            }
        }

        private void DespawnAnnotationCanvases()
        {
            if (activeSymbolPaletteCanvas != null)
            {
                Destroy(activeSymbolPaletteCanvas);
                activeSymbolPaletteCanvas = null;
            }

            if (activeAnnotationLegendCanvas != null)
            {
                Destroy(activeAnnotationLegendCanvas);
                activeAnnotationLegendCanvas = null;
            }

            // Cancel any pending annotation so the drawing tool is re-enabled cleanly.
            VRDrawing.Geology.GeologicalAnnotationManager.Instance?.CancelAnnotationMode();
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
            // Use the XROrigin's yaw-only forward so board spawns in front of the player's
            // standing direction, unaffected by head tilt from TrackedPoseDriver.
            Vector3 forward = GetPlayerYawForward();
            return playerCamera.position + forward * boardDistance + Vector3.up * boardHeight;
        }

        private Quaternion CalculateBoardRotation()
        {
            // Board mesh normals face local -Z. LookRotation(forward) sets local +Z = player forward,
            // so local -Z faces the player — correct for visibility.
            return Quaternion.LookRotation(GetPlayerYawForward());
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
        }

        public void HideToolPanel()
        {
            if (activeToolPanel != null)
                activeToolPanel.SetActive(false);
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
            // Panel canvas also faces local -Z — same convention as the board.
            return Quaternion.LookRotation(GetPlayerYawForward());
        }

        /// <summary>
        /// Returns the player's horizontal (yaw-only) forward direction derived from the XROrigin
        /// root transform. This is stable and unaffected by head tilt from TrackedPoseDriver,
        /// and correctly reflects any yaw applied by ApplyCameraConfiguration after a map load.
        /// Falls back to flattening playerCamera.forward if xrOrigin is null.
        /// </summary>
        private Vector3 GetPlayerYawForward()
        {
            Transform source = xrOrigin != null ? xrOrigin : playerCamera;
            Vector3 forward  = source.forward;
            forward.y        = 0f;

            if (forward.sqrMagnitude < 0.001f)
                return Vector3.forward;

            return forward.normalized;
        }
    }
}
