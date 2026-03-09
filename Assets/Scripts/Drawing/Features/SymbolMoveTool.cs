using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRDrawing.Features
{
    /// <summary>
    /// Handles moving an existing GeologicalSymbolObject by raycasting back onto the board.
    /// Activated by SymbolEditMenu; the user points at the board to reposition the symbol
    /// and releases the trigger to confirm.
    /// </summary>
    public class SymbolMoveTool : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionProperty confirmMoveAction;  // Trigger release confirms

        [Header("Ray Interactor")]
        [SerializeField] private XRRayInteractor rayInteractor;

        [Header("Layer Mask")]
        [SerializeField] private LayerMask boardLayerMask = 1 << 3;

        public static SymbolMoveTool Instance { get; private set; }

        private GeologicalSymbolObject movingSymbol;
        private bool isMoving = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (rayInteractor == null)
                rayInteractor = GetComponent<XRRayInteractor>();
        }

        private void OnEnable()
        {
            if (confirmMoveAction.action != null) confirmMoveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (confirmMoveAction.action != null) confirmMoveAction.action.Disable();
        }

        /// <summary>Starts a move operation for the given symbol.</summary>
        public void BeginMove(GeologicalSymbolObject symbol)
        {
            movingSymbol = symbol;
            isMoving = true;
            Debug.Log($"[SymbolMoveTool] Moving symbol: {symbol?.Data?.type}");
        }

        private void Update()
        {
            if (!isMoving || movingSymbol == null || rayInteractor == null) return;

            // Preview position while trigger is held
            bool hit = rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit rayHit);
            if (hit)
            {
                bool onBoard = ((1 << rayHit.collider.gameObject.layer) & boardLayerMask) != 0;
                if (onBoard)
                {
                    // Live preview: move the symbol to follow the ray
                    SymbolLayerManager.Instance?.MoveSymbol(movingSymbol, rayHit.point);
                }
            }

            // Confirm when trigger released
            bool released = confirmMoveAction.action != null && !confirmMoveAction.action.IsPressed();
            if (released)
            {
                isMoving = false;
                movingSymbol = null;
                Debug.Log("[SymbolMoveTool] Move confirmed.");
            }
        }
    }
}
