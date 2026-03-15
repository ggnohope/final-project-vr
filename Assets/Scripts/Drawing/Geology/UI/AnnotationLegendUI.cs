using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace VRDrawing.Geology.UI
{
    /// <summary>
    /// Legend Canvas — a World Space canvas placed to the RIGHT of the drawing board.
    ///
    /// Layout (designed for ~500×800 world-space canvas at 1000 ppu):
    ///
    ///   ┌─────────────────────────────┐
    ///   │  📋  Legend             [X] │  ← header + close
    ///   ├──────────────── Layer Control┤
    ///   │  ● Soils            [👁  ▐] │
    ///   │  ● Mixed Soils      [👁  ▐] │
    ///   │  ● Rocks            [👁  ▐] │
    ///   ├──────────────── Placed ──────┤
    ///   │  █  SC   Sandy Clay    [3]  │
    ///   │  █  CL   Low Plast…    [1]  │
    ///   │  ...                        │
    ///   ├─────────────────────────────┤
    ///   │          [Clear All]        │
    ///   └─────────────────────────────┘
    ///
    /// Attach to the root of the AnnotationLegend World Space Canvas GameObject.
    /// </summary>
    public class AnnotationLegendUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Layer Control Section")]
        [SerializeField] private Transform layerControlContainer;
        [SerializeField] private GameObject layerToggleRowPrefab;

        [Header("Symbol List Section")]
        [SerializeField] private Transform legendRowContainer;
        [SerializeField] private GameObject legendRowPrefab;

        [Header("Footer")]
        [SerializeField] private Button clearAllButton;

        [Header("Header")]
        [SerializeField] private Button closeButton;

        [Header("Positioning (relative to drawing board)")]
        [SerializeField] private float offsetX = 0.55f;
        [SerializeField] private float offsetY = 0f;

        // ── State ─────────────────────────────────────────────────────────────

        private GeologicalAnnotationManager manager;

        // symbolId → legend row
        private readonly Dictionary<string, LegendRowUI> legendRows =
            new Dictionary<string, LegendRowUI>();

        public static AnnotationLegendUI Instance { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // Fix the root "Canvas (Environment)" wrapper to WorldSpace.
            Canvas rootCanvas = GetComponentInParent<Canvas>(true);
            if (rootCanvas != null && rootCanvas.isRootCanvas)
            {
                rootCanvas.renderMode       = RenderMode.WorldSpace;
                rootCanvas.sortingOrder     = 50;
                rootCanvas.sortingLayerName = "UI";
                Debug.Log($"[AnnotationLegendUI] Root canvas '{rootCanvas.name}' set to WorldSpace.");
            }

            // Ensure correct world-space scale on this transform (500×800 px at 1000 ppu = 0.5×0.8 m).
            transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Do NOT resolve manager here — GeologicalAnnotationManager.Awake may not have run yet.
        }

        private void Start()
        {
            // Resolve manager after all Awake() calls are complete.
            manager = GeologicalAnnotationManager.Instance
                   ?? FindFirstObjectByType<GeologicalAnnotationManager>();

            if (manager == null)
            {
                Debug.LogError("[AnnotationLegendUI] GeologicalAnnotationManager not found in scene.");
            }
            else
            {
                // OnEnable fired before Start — manager was null then. Subscribe now.
                manager.OnSymbolPlaced  -= HandleSymbolPlaced;
                manager.OnSymbolRemoved -= HandleSymbolRemoved;
                manager.OnAllCleared    -= HandleAllCleared;
                manager.OnSymbolPlaced  += HandleSymbolPlaced;
                manager.OnSymbolRemoved += HandleSymbolRemoved;
                manager.OnAllCleared    += HandleAllCleared;
            }

            BuildLayerControlSection();
            SetupFooter();
            SetupCloseButton();
        }

        private void OnEnable()
        {
            PositionNextToBoard();

            // Subscribe to events — re-resolve manager in case Start() hasn't run yet
            // (OnEnable fires before Start on first enable).
            if (manager == null)
                manager = GeologicalAnnotationManager.Instance
                       ?? FindFirstObjectByType<GeologicalAnnotationManager>();

            if (manager != null)
            {
                manager.OnSymbolPlaced  += HandleSymbolPlaced;
                manager.OnSymbolRemoved += HandleSymbolRemoved;
                manager.OnAllCleared    += HandleAllCleared;
            }
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.OnSymbolPlaced  -= HandleSymbolPlaced;
                manager.OnSymbolRemoved -= HandleSymbolRemoved;
                manager.OnAllCleared    -= HandleAllCleared;
            }
        }

        /// <summary>Positions this canvas to the right side of the active drawing board.</summary>
        public void PositionNextToBoard()
        {
            if (VRDrawing.Mode.DrawingModeManager.Instance == null) return;
            GameObject board = VRDrawing.Mode.DrawingModeManager.Instance.ActiveDrawingBoard;
            if (board == null) return;

            Transform root = transform.root;
            Transform boardTransform = board.transform;
            root.position = boardTransform.position
                + boardTransform.right * offsetX
                + boardTransform.up * offsetY;
            root.rotation = boardTransform.rotation;
        }

        // ── Layer Control ─────────────────────────────────────────────────────

        private void BuildLayerControlSection()
        {
            if (manager == null || layerControlContainer == null || layerToggleRowPrefab == null)
                return;

            foreach (var kvp in manager.Layers)
            {
                GameObject rowObj = Instantiate(layerToggleRowPrefab, layerControlContainer);
                LayerToggleRowUI row = rowObj.GetComponent<LayerToggleRowUI>();
                row?.Bind(kvp.Value);
            }
        }

        // ── Symbol placement events ───────────────────────────────────────────

        private void HandleSymbolPlaced(SymbolInstance instance)
        {
            GeologicalSymbolDefinition def = manager.Resolve(instance.symbolId);
            if (def == null) return;

            EnsureLegendRow(def);
            RefreshCount(def.id);
        }

        private void HandleSymbolRemoved(SymbolInstance instance)
        {
            GeologicalSymbolDefinition def = manager.Resolve(instance.symbolId);
            if (def == null) return;

            int remaining = manager.GetInstanceCountById(def.id);
            if (remaining > 0)
            {
                RefreshCount(def.id);
            }
            else if (legendRows.TryGetValue(def.id, out LegendRowUI row))
            {
                Destroy(row.gameObject);
                legendRows.Remove(def.id);
            }
        }

        private void HandleAllCleared()
        {
            foreach (var row in legendRows.Values)
                if (row != null) Destroy(row.gameObject);

            legendRows.Clear();
        }

        // ── Legend row management ─────────────────────────────────────────────

        private void EnsureLegendRow(GeologicalSymbolDefinition def)
        {
            if (legendRows.ContainsKey(def.id)) return;
            if (legendRowContainer == null || legendRowPrefab == null) return;

            GameObject rowObj = Instantiate(legendRowPrefab, legendRowContainer);
            LegendRowUI row = rowObj.GetComponent<LegendRowUI>();
            if (row == null) return;

            row.Bind(def);
            legendRows[def.id] = row;
        }

        private void RefreshCount(string symbolId)
        {
            if (!legendRows.TryGetValue(symbolId, out LegendRowUI row)) return;
            row.SetCount(manager.GetInstanceCountById(symbolId));
        }

        // ── Footer ────────────────────────────────────────────────────────────

        private void SetupFooter()
        {
            if (clearAllButton != null)
                clearAllButton.onClick.AddListener(() => manager?.ClearAll());
        }

        private void SetupCloseButton()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}
