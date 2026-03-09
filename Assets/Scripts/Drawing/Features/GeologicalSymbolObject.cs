using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRDrawing.Data;

namespace VRDrawing.Features
{
    /// <summary>
    /// Attached to each spawned symbol GameObject inside the SymbolLayer.
    /// Handles billboard rotation, hover highlighting and stores annotation data.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GeologicalSymbolObject : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshPro label;

        [Header("Visual")]
        [SerializeField] private float labelFontSize = 0.06f;

        private PlacedSymbolData data;
        private GeologicalSymbolDefinition definition;
        private Camera mainCamera;
        private BoxCollider symbolCollider;

        // Highlight state
        private Color baseColor;
        private bool isHighlighted;

        // Events consumed by SymbolLayer / EditMenu
        public System.Action<GeologicalSymbolObject> OnHoverEnter;
        public System.Action<GeologicalSymbolObject> OnHoverExit;
        public System.Action<GeologicalSymbolObject> OnSelected;

        /// <summary>Normalised board position stored with this symbol.</summary>
        public PlacedSymbolData Data => data;
        public GeologicalSymbolDefinition Definition => definition;

        private void Awake()
        {
            symbolCollider = GetComponent<BoxCollider>();
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            // Billboard: always face the main camera
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
        }

        /// <summary>
        /// Initialises this symbol from persistent data and a symbol definition.
        /// Must be called immediately after Instantiate.
        /// </summary>
        public void Initialise(PlacedSymbolData symbolData, GeologicalSymbolDefinition def)
        {
            data = symbolData;
            definition = def;
            mainCamera = Camera.main;

            if (label == null)
            {
                label = CreateLabel();
            }

            label.text = def.code;
            label.color = def.color;
            label.fontSize = labelFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;

            baseColor = def.color;

            // Size collider to match text bounds roughly
            if (symbolCollider != null)
            {
                symbolCollider.size = new Vector3(0.07f, 0.04f, 0.01f);
                symbolCollider.isTrigger = true;
            }
        }

        /// <summary>Updates the symbol type at runtime (e.g. from edit menu).</summary>
        public void ChangeSymbolType(string newCode)
        {
            var def = GeologicalSymbolRegistry.Find(newCode);
            if (def == null) return;

            definition = def;
            data.type = newCode;

            label.text = def.code;
            label.color = def.color;
            baseColor = def.color;
        }

        /// <summary>Updates the free-text note attached to this symbol.</summary>
        public void SetNote(string note)
        {
            if (data != null) data.note = note;
        }

        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            if (label != null)
            {
                label.color = highlight ? Color.white : baseColor;
                label.fontStyle = highlight ? FontStyles.Bold | FontStyles.Underline : FontStyles.Bold;
            }
        }

        // ── Pointer events called by SymbolInteractionHandler ───────────────

        public void NotifyHoverEnter()
        {
            SetHighlight(true);
            OnHoverEnter?.Invoke(this);
        }

        public void NotifyHoverExit()
        {
            SetHighlight(false);
            OnHoverExit?.Invoke(this);
        }

        public void NotifySelected()
        {
            OnSelected?.Invoke(this);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private TextMeshPro CreateLabel()
        {
            TextMeshPro tmp = gameObject.AddComponent<TextMeshPro>();
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }
    }
}
