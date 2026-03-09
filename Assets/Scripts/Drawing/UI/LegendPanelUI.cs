using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRDrawing.Data;
using VRDrawing.Features;

namespace VRDrawing.UI
{
    /// <summary>
    /// Displays a legend panel listing all geological symbols with their full names.
    /// Highlights the entry that corresponds to the currently hovered symbol on the board.
    /// </summary>
    public class LegendPanelUI : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private Transform legendContainer;
        [SerializeField] private GameObject legendEntryPrefab;  // Image (color swatch) + TMP label

        private System.Collections.Generic.Dictionary<string, Image> swatchByCode =
            new System.Collections.Generic.Dictionary<string, Image>();

        private void Start()
        {
            BuildLegend();
            WireHoverEvents();
        }

        private void BuildLegend()
        {
            if (legendContainer == null || legendEntryPrefab == null) return;

            foreach (var def in GeologicalSymbolRegistry.GetAll())
            {
                GameObject entry = Instantiate(legendEntryPrefab, legendContainer);

                // Expect: child[0] = Image swatch, child[1] = TMP label
                Image swatch = entry.GetComponentInChildren<Image>();
                if (swatch != null)
                {
                    swatch.color = def.color;
                    swatchByCode[def.code] = swatch;
                }

                TextMeshProUGUI label = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = $"<b>{def.code}</b>  {def.fullName}";
                }
            }
        }

        private void WireHoverEvents()
        {
            if (SymbolLayerManager.Instance == null) return;

            SymbolLayerManager.Instance.OnSymbolHovered  += OnSymbolHover;
            SymbolLayerManager.Instance.OnSymbolHoverExit += OnSymbolHoverExit;
        }

        private void OnDestroy()
        {
            if (SymbolLayerManager.Instance != null)
            {
                SymbolLayerManager.Instance.OnSymbolHovered  -= OnSymbolHover;
                SymbolLayerManager.Instance.OnSymbolHoverExit -= OnSymbolHoverExit;
            }
        }

        private void OnSymbolHover(GeologicalSymbolObject sym)
        {
            if (sym?.Definition == null) return;

            string code = sym.Definition.code;
            foreach (var kvp in swatchByCode)
            {
                bool highlight = kvp.Key == code;
                kvp.Value.color = highlight
                    ? Color.white
                    : GeologicalSymbolRegistry.Find(kvp.Key)?.color ?? Color.white;
            }
        }

        private void OnSymbolHoverExit(GeologicalSymbolObject sym)
        {
            // Reset all swatches to their canonical colour
            foreach (var kvp in swatchByCode)
            {
                var def = GeologicalSymbolRegistry.Find(kvp.Key);
                if (def != null) kvp.Value.color = def.color;
            }
        }
    }
}
