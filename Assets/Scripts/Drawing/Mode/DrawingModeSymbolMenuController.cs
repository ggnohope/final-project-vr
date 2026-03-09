using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.UI;
using VRDrawing.Features;
using VRDrawing.UI;

namespace VRDrawing.Mode
{
    /// <summary>
    /// Connects DrawingModeManager lifecycle events to the world-space symbol menus.
    /// - Positions SymbolToolMenuCanvas LEFT of the drawing board and
    ///   LegendPanelCanvas RIGHT of it (no overlap with the board).
    /// - Replaces any standard GraphicRaycaster on those canvases with
    ///   TrackedDeviceGraphicRaycaster so the XR UI Ray Interactor can hit them.
    /// - Triggers SymbolToolMenuUI.BuildSymbolButtons() on first show so that
    ///   buttons are built even when the canvas starts inactive.
    /// Attach to the DrawingModeManager GameObject.
    /// </summary>
    public class DrawingModeSymbolMenuController : MonoBehaviour
    {
        [Header("Menus to show/hide with drawing mode")]
        [SerializeField] private GameObject symbolToolMenuCanvas;
        [SerializeField] private GameObject legendPanelCanvas;

        [Header("Positioning — relative to player camera")]
        [Tooltip("Distance in front of the camera where the menus appear (match board distance).")]
        [SerializeField] private float menuDistance = 1.5f;
        [Tooltip("How far left the symbol tool menu is offset (negative = left). Canvas is 0.6m wide.")]
        [SerializeField] private float symbolMenuSideOffset = -1.1f;
        [Tooltip("How far right the legend panel is offset (positive = right). Canvas is 0.32m wide.")]
        [SerializeField] private float legendSideOffset = 0.9f;
        [Tooltip("Vertical offset relative to the camera height.")]
        [SerializeField] private float menuHeight = 0.0f;

        [Header("Symbol Placement Tool")]
        [SerializeField] private SymbolPlacementTool symbolPlacementTool;

        private DrawingModeManager drawingModeManager;
        private bool buttonsBuilt = false;

        private void Awake()
        {
            drawingModeManager = GetComponent<DrawingModeManager>();
            if (drawingModeManager == null)
                drawingModeManager = FindFirstObjectByType<DrawingModeManager>();

            // Start hidden — controller activates them on drawing mode entry
            if (symbolToolMenuCanvas != null) symbolToolMenuCanvas.SetActive(false);
            if (legendPanelCanvas != null) legendPanelCanvas.SetActive(false);
        }

        private void OnEnable()
        {
            if (drawingModeManager != null)
            {
                drawingModeManager.OnDrawingModeEntered += OnDrawingModeEntered;
                drawingModeManager.OnDrawingModeExited += OnDrawingModeExited;
            }
        }

        private void OnDisable()
        {
            if (drawingModeManager != null)
            {
                drawingModeManager.OnDrawingModeEntered -= OnDrawingModeEntered;
                drawingModeManager.OnDrawingModeExited -= OnDrawingModeExited;
            }
        }

        private void OnDrawingModeEntered()
        {
            PositionAndShowMenus();

            if (symbolPlacementTool != null)
                symbolPlacementTool.SetEnabled(true);
        }

        private void OnDrawingModeExited()
        {
            if (symbolToolMenuCanvas != null) symbolToolMenuCanvas.SetActive(false);
            if (legendPanelCanvas != null) legendPanelCanvas.SetActive(false);

            if (symbolPlacementTool != null)
                symbolPlacementTool.SetEnabled(false);
        }

        private void PositionAndShowMenus()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[DrawingModeSymbolMenuController] No main camera found.");
                return;
            }

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            // Use the board's actual world position to derive correct coplanar depth,
            // so menus are never at a different Z than the board regardless of boardDistance.
            float depth = menuDistance;
            if (drawingModeManager != null && drawingModeManager.ActiveDrawingBoard != null)
            {
                Vector3 boardPos = drawingModeManager.ActiveDrawingBoard.transform.position;
                depth = Vector3.Dot(boardPos - cam.transform.position, forward);
                Debug.Log($"[SymbolMenuCtrl] Board at {boardPos}, projected depth={depth:F2}m");
            }

            Vector3 basePos  = cam.transform.position + forward * depth + Vector3.up * menuHeight;
            Quaternion faceCam = Quaternion.LookRotation(forward);

            // SymbolToolMenu: canvas 600px × scale 0.001 = 0.6m wide (half=0.3m)
            // Board: boardScale.x=1 → 1m wide (half=0.5m)
            // Minimum clear offset = 0.5 + 0.3 + 0.05 pad = 0.85m  → use -1.1 by default
            Vector3 symbolPos = basePos + right * symbolMenuSideOffset;
            PlaceMenu(symbolToolMenuCanvas, symbolPos, faceCam);
            Debug.Log($"[SymbolMenuCtrl] SymbolToolMenu → {symbolPos} (offset={symbolMenuSideOffset}m)");

            // LegendPanel: canvas ~320px × 0.001 = 0.32m wide (half=0.16m)
            // Minimum clear offset = 0.5 + 0.16 + 0.05 pad = 0.71m  → use +0.9 by default
            Vector3 legendPos = basePos + right * legendSideOffset;
            PlaceMenu(legendPanelCanvas, legendPos, faceCam);
            Debug.Log($"[SymbolMenuCtrl] LegendPanel → {legendPos} (offset={legendSideOffset}m)");

            // Upgrade raycasters AFTER SetActive so DestroyImmediate is safe
            EnsureTrackedRaycaster(symbolToolMenuCanvas);
            EnsureTrackedRaycaster(legendPanelCanvas);

            // Build symbol buttons on first entry — canvas must be active for layout
            if (!buttonsBuilt && symbolToolMenuCanvas != null)
            {
                // Use GetComponentInChildren so it works even if SymbolToolMenuUI
                // is on a child panel rather than the canvas root itself.
                SymbolToolMenuUI menuUI = symbolToolMenuCanvas.GetComponentInChildren<SymbolToolMenuUI>(true);
                if (menuUI != null)
                {
                    menuUI.BuildAllButtons();
                    buttonsBuilt = true;
                    Debug.Log("[SymbolMenuCtrl] BuildAllButtons() called successfully.");
                }
                else
                {
                    Debug.LogError("[SymbolMenuCtrl] SymbolToolMenuUI not found on SymbolToolMenuCanvas or its children!");
                }
            }
        }

        private static void PlaceMenu(GameObject canvas, Vector3 position, Quaternion rotation)
        {
            if (canvas == null) return;
            canvas.transform.position = position;
            canvas.transform.rotation = rotation;
            canvas.SetActive(true);
        }

        /// <summary>
        /// Ensures the canvas has TrackedDeviceGraphicRaycaster (required by XR UI Ray Interactor).
        /// Uses DestroyImmediate so the old GraphicRaycaster is removed synchronously.
        /// Must be called AFTER SetActive(true) on the canvas.
        /// </summary>
        private static void EnsureTrackedRaycaster(GameObject canvas)
        {
            if (canvas == null) return;

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() != null)
            {
                Debug.Log($"[DrawingModeSymbolMenuController] {canvas.name}: TrackedDeviceGraphicRaycaster already present.");
                return;
            }

            // DestroyImmediate required — Destroy() is deferred and leaves duplicate alive this frame
            GraphicRaycaster old = canvas.GetComponent<GraphicRaycaster>();
            if (old != null)
            {
                Object.DestroyImmediate(old);
                Debug.Log($"[DrawingModeSymbolMenuController] {canvas.name}: Removed GraphicRaycaster.");
            }

            canvas.AddComponent<TrackedDeviceGraphicRaycaster>();
            Debug.Log($"[DrawingModeSymbolMenuController] {canvas.name}: Added TrackedDeviceGraphicRaycaster.");
        }
    }
}
