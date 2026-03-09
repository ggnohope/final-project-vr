using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRDrawing.Data;

namespace VRDrawing.Features
{
    /// <summary>
    /// Listens to the XR ray interactor and places the currently selected geological symbol
    /// when the user pulls the trigger while pointing at the drawing board.
    /// Hold-trigger opens the edit menu for a hovered symbol.
    /// </summary>
    [RequireComponent(typeof(XRRayInteractor))]
    public class SymbolPlacementTool : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionProperty placeAction;      // Trigger click
        [SerializeField] private InputActionProperty holdEditAction;    // Hold trigger → edit menu

        [Header("Layer Mask")]
        [SerializeField] private LayerMask boardLayerMask = 1 << 3;   // "Drawing Surface" layer

        [Header("Edit Menu")]
        [SerializeField] private SymbolEditMenu editMenu;

        [Header("Hold Threshold")]
        [SerializeField] private float holdThresholdSeconds = 0.6f;

        private XRRayInteractor rayInteractor;
        private string selectedSymbolCode = "SC";
        private bool isEnabled = false;
        private bool wasPressed = false;
        private float pressStartTime = -1f;
        private GeologicalSymbolObject hoveredSymbol;

        private void Awake()
        {
            rayInteractor = GetComponent<XRRayInteractor>();

            // Auto-find edit menu if not assigned in Inspector
            if (editMenu == null)
                editMenu = FindFirstObjectByType<SymbolEditMenu>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            if (placeAction.action != null) placeAction.action.Enable();
            if (holdEditAction.action != null) holdEditAction.action.Enable();
            Debug.Log($"[SymbolPlacementTool] OnEnable — placeAction wired={placeAction.action != null}, isEnabled={isEnabled}");
        }

        private void OnDisable()
        {
            if (placeAction.action != null) placeAction.action.Disable();
            if (holdEditAction.action != null) holdEditAction.action.Disable();
        }

        /// <summary>Enables or disables symbol placement. Call after entering drawing mode.</summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            Debug.Log($"[SymbolPlacementTool] SetEnabled({enabled}) — selectedCode='{selectedSymbolCode}'");
        }

        /// <summary>Sets the symbol code that will be placed on the next trigger click.</summary>
        public void SelectSymbol(string code) => selectedSymbolCode = code;

        private void Update()
        {
            if (!isEnabled || rayInteractor == null) return;

            // Fallback: if placeAction is not wired, use the ray interactor's select state
            bool pressed = IsPlacePressed();

            // Track hover for highlight
            UpdateHover();

            if (pressed && !wasPressed)
            {
                // Button just pressed
                pressStartTime = Time.time;
            }
            else if (!pressed && wasPressed)
            {
                // Button just released
                float held = Time.time - pressStartTime;
                if (held >= holdThresholdSeconds)
                {
                    OpenEditMenu();
                }
                else
                {
                    TryPlaceSymbol();
                }
                pressStartTime = -1f;
            }

            wasPressed = pressed;
        }

        /// <summary>
        /// Returns true when the place input is pressed.
        /// Uses the assigned InputAction if wired; otherwise falls back to the
        /// XRRayInteractor's current selection state (trigger).
        /// </summary>
        private bool IsPlacePressed()
        {
            if (placeAction.action != null)
            {
                // Ensure action is enabled before reading
                if (!placeAction.action.enabled) placeAction.action.Enable();
                return placeAction.action.IsPressed();
            }

            // Fallback: mirror the ray interactor's select (trigger) state
            return rayInteractor.isSelectActive;
        }

        private void UpdateHover()
        {
            bool hit = rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit rayHit);

            GeologicalSymbolObject newHover = null;
            if (hit)
            {
                newHover = rayHit.collider.GetComponent<GeologicalSymbolObject>();
            }

            if (newHover != hoveredSymbol)
            {
                hoveredSymbol?.NotifyHoverExit();
                hoveredSymbol = newHover;
                hoveredSymbol?.NotifyHoverEnter();
            }
        }

        private void TryPlaceSymbol()
        {
            if (SymbolLayerManager.Instance == null)
            {
                Debug.LogWarning("[SymbolPlacementTool] TryPlaceSymbol — SymbolLayerManager.Instance is NULL.");
                return;
            }

            bool hit = rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit rayHit);
            if (!hit)
            {
                Debug.Log("[SymbolPlacementTool] TryPlaceSymbol — no 3D raycast hit.");
                return;
            }

            int hitLayer = rayHit.collider.gameObject.layer;
            bool onBoard = ((1 << hitLayer) & boardLayerMask) != 0;
            Debug.Log($"[SymbolPlacementTool] TryPlaceSymbol — hit '{rayHit.collider.gameObject.name}' layer={hitLayer} ({LayerMask.LayerToName(hitLayer)}), onBoard={onBoard}, mask={boardLayerMask.value}");

            if (!onBoard) return;

            SymbolLayerManager.Instance.PlaceSymbolAtWorldPoint(selectedSymbolCode, rayHit.point);
            Debug.Log($"[SymbolPlacementTool] Placed '{selectedSymbolCode}' at {rayHit.point}");
        }

        private void OpenEditMenu()
        {
            if (editMenu == null || hoveredSymbol == null) return;
            editMenu.Open(hoveredSymbol);
        }
    }
}
