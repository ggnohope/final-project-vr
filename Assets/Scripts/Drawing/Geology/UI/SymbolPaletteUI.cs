using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace VRDrawing.Geology.UI
{
    /// <summary>
    /// Symbol Canvas — a World Space canvas placed to the LEFT of the drawing board.
    ///
    /// Layout (designed for ~600×800 world-space canvas at 1000 ppu):
    ///
    ///   ┌─────────────────────────────────┐
    ///   │  🪨  Geological Symbols     [X] │  ← header + close button
    ///   ├─────────────────────────────────┤
    ///   │  [ Soil ]  [ Mixed ]  [ Rock ]  │  ← category tabs
    ///   ├─────────────────────────────────┤
    ///   │  ┌──────┐ ┌──────┐ ┌──────┐    │
    ///   │  │ █ S  │ │ █ C  │ │ █ Si │    │  ← symbol grid
    ///   │  │ Sand │ │ Clay │ │ Silt │    │
    ///   │  └──────┘ └──────┘ └──────┘    │
    ///   │  ...                            │
    ///   ├─────────────────────────────────┤
    ///   │  Placing: Sandy Clay  [Cancel]  │  ← placement status bar
    ///   └─────────────────────────────────┘
    ///
    /// Attach to the root of the SymbolPalette World Space Canvas GameObject.
    /// </summary>
    public class SymbolPaletteUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Tab Buttons")]
        [SerializeField] private Button tabSoil;
        [SerializeField] private Button tabMixed;
        [SerializeField] private Button tabRock;
        [SerializeField] private Color tabActiveColor = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] private Color tabInactiveColor = new Color(0.15f, 0.15f, 0.15f);

        [Header("Grid")]
        [SerializeField] private Transform gridContent;
        [SerializeField] private GameObject symbolButtonPrefab;

        [Header("Status Bar")]
        [SerializeField] private GameObject statusBar;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Button cancelButton;

        [Header("Header")]
        [SerializeField] private Button closeButton;

        [Header("Positioning (relative to drawing board)")]
        [SerializeField] private float offsetX = -0.55f;
        [SerializeField] private float offsetY = 0f;

        // ── State ─────────────────────────────────────────────────────────────

        private GeologicalAnnotationManager manager;
        private SymbolCategory activeTab = SymbolCategory.Soil;
        private GeologicalSymbolDefinition selectedSymbol;
        private readonly List<SymbolPaletteButtonUI> spawnedButtons = new List<SymbolPaletteButtonUI>();

        public static SymbolPaletteUI Instance { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // The prefab may be wrapped in a "Canvas (Environment)" root by Unity Editor.
            // That root Canvas must be WorldSpace — nested Canvas renderMode cannot override it.
            // Fix the root Canvas, then set our own scale.
            Canvas rootCanvas = GetComponentInParent<Canvas>(true);
            if (rootCanvas != null && rootCanvas.isRootCanvas)
            {
                rootCanvas.renderMode   = RenderMode.WorldSpace;
                rootCanvas.sortingOrder = 50;
                rootCanvas.sortingLayerName = "UI";
                Debug.Log($"[SymbolPaletteUI] Root canvas '{rootCanvas.name}' set to WorldSpace.");
            }

            // Also fix our own Canvas (the child one with correct sizeDelta / scale).
            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas != null && !ownCanvas.isRootCanvas)
            {
                // Child canvas: keep scale so world size is correct.
                // renderMode on a nested canvas is ignored; what matters is the root.
            }

            // Ensure correct world-space scale on this transform (600×800 px at 1000 ppu = 0.6×0.8 m).
            transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Do NOT resolve manager here — GeologicalAnnotationManager.Awake may not have run yet.
        }

        private void Start()
        {
            // Resolve manager after all Awake() calls are complete.
            manager = GeologicalAnnotationManager.Instance
                   ?? FindFirstObjectByType<GeologicalAnnotationManager>();

            Debug.Log($"[SymbolPaletteUI] Start — manager={manager != null}, " +
                      $"database={manager?.Database != null}, " +
                      $"symbols={manager?.Database?.Symbols?.Count}, " +
                      $"gridContent={gridContent != null}, " +
                      $"symbolButtonPrefab={symbolButtonPrefab != null}");

            if (manager == null)
                Debug.LogError("[SymbolPaletteUI] GeologicalAnnotationManager not found in scene.");
            else if (manager.Database == null)
                Debug.LogError("[SymbolPaletteUI] GeologicalAnnotationManager.Database is null — check that GeologicalSymbolDatabase is assigned.");

            if (gridContent == null)
                Debug.LogError("[SymbolPaletteUI] gridContent (Content RectTransform) is not assigned.");

            if (symbolButtonPrefab == null)
                Debug.LogError("[SymbolPaletteUI] symbolButtonPrefab is not assigned.");

            SetupTabButtons();
            SetupCancelButton();
            SetupCloseButton();
            ShowTab(SymbolCategory.Soil);
            RefreshStatusBar();
        }

        private void OnEnable()
        {
            PositionNextToBoard();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Positions this canvas to the left side of the active drawing board.</summary>
        public void PositionNextToBoard()
        {
            if (VRDrawing.Mode.DrawingModeManager.Instance == null) return;
            GameObject board = VRDrawing.Mode.DrawingModeManager.Instance.ActiveDrawingBoard;
            if (board == null) return;

            // Move the root prefab GameObject (parent of the Canvas child).
            // Using transform.root ensures we move the outermost object regardless
            // of where this script lives in the hierarchy.
            Transform root = transform.root;
            Transform boardTransform = board.transform;
            root.position = boardTransform.position
                + boardTransform.right * offsetX
                + boardTransform.up * offsetY;
            root.rotation = boardTransform.rotation;
        }

        private Canvas GetRootCanvas()
        {
            Canvas c = GetComponentInParent<Canvas>(true);
            return (c != null && c.isRootCanvas) ? c : GetComponent<Canvas>() ?? GetComponentInParent<Canvas>(true);
        }

        // ── Tab management ────────────────────────────────────────────────────

        private void SetupTabButtons()
        {
            if (tabSoil  != null) tabSoil .onClick.AddListener(() => ShowTab(SymbolCategory.Soil));
            if (tabMixed != null) tabMixed.onClick.AddListener(() => ShowTab(SymbolCategory.MixedSoil));
            if (tabRock  != null) tabRock .onClick.AddListener(() => ShowTab(SymbolCategory.Rock));
        }

        private void ShowTab(SymbolCategory category)
        {
            activeTab = category;
            RebuildGrid();
            RefreshTabColors();
        }

        private void RefreshTabColors()
        {
            SetTabColor(tabSoil,  activeTab == SymbolCategory.Soil);
            SetTabColor(tabMixed, activeTab == SymbolCategory.MixedSoil);
            SetTabColor(tabRock,  activeTab == SymbolCategory.Rock);
        }

        private void SetTabColor(Button tab, bool active)
        {
            if (tab == null) return;
            Image img = tab.GetComponent<Image>();
            if (img != null)
                img.color = active ? tabActiveColor : tabInactiveColor;
        }

        // ── Grid ──────────────────────────────────────────────────────────────

        private void RebuildGrid()
        {
            foreach (var btn in spawnedButtons)
                if (btn != null) Destroy(btn.gameObject);
            spawnedButtons.Clear();

            if (manager == null)       { Debug.LogError("[SymbolPaletteUI] RebuildGrid: manager is null"); return; }
            if (manager.Database == null) { Debug.LogError("[SymbolPaletteUI] RebuildGrid: Database is null"); return; }
            if (gridContent == null)   { Debug.LogError("[SymbolPaletteUI] RebuildGrid: gridContent is null"); return; }
            if (symbolButtonPrefab == null) { Debug.LogError("[SymbolPaletteUI] RebuildGrid: symbolButtonPrefab is null"); return; }

            IReadOnlyList<GeologicalSymbolDefinition> defs = manager.Database.GetByCategory(activeTab);
            Debug.Log($"[SymbolPaletteUI] RebuildGrid tab={activeTab}, defs={defs.Count}, gridContent={gridContent.name}");

            foreach (var def in defs)
            {
                GameObject btnObj = Instantiate(symbolButtonPrefab, gridContent);

                // Handle case where the prefab is wrapped in a "Canvas (Environment)" root
                // (Unity Editor artifact) — script lives on the actual button child.
                SymbolPaletteButtonUI btn = btnObj.GetComponent<SymbolPaletteButtonUI>()
                                         ?? btnObj.GetComponentInChildren<SymbolPaletteButtonUI>(true);

                Debug.Log($"[SymbolPaletteUI] Spawned '{btnObj.name}' (root children: {btnObj.transform.childCount}), btn={btn != null}, def={def.id}");

                if (btn == null)
                {
                    Debug.LogError($"[SymbolPaletteUI] SymbolPaletteButtonUI NOT found on '{btnObj.name}' or any child — check SymbolPaletteButton prefab.");
                    continue;
                }

                btn.Bind(def, OnSymbolButtonClicked);
                btn.SetSelected(selectedSymbol != null && selectedSymbol.id == def.id);
                spawnedButtons.Add(btn);
            }

            Debug.Log($"[SymbolPaletteUI] RebuildGrid done — {spawnedButtons.Count} buttons added.");
        }

        // ── Symbol selection ──────────────────────────────────────────────────

        private void OnSymbolButtonClicked(GeologicalSymbolDefinition def)
        {
            selectedSymbol = def;
            manager?.SelectSymbol(def);

            // Update highlight
            foreach (var btn in spawnedButtons)
                btn.SetSelected(btn != null && IsButtonForDef(btn, def));

            RefreshStatusBar();
        }

        private bool IsButtonForDef(SymbolPaletteButtonUI btn, GeologicalSymbolDefinition def)
        {
            return btn.BoundDefinition != null && btn.BoundDefinition.id == def.id;
        }

        // ── Status bar ────────────────────────────────────────────────────────

        private void SetupCancelButton()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnCancelClicked()
        {
            selectedSymbol = null;
            manager?.CancelAnnotationMode();
            RefreshStatusBar();
            foreach (var btn in spawnedButtons) btn?.SetSelected(false);
        }

        private void RefreshStatusBar()
        {
            bool placing = selectedSymbol != null;
            if (statusBar != null)
                statusBar.SetActive(placing);

            if (statusLabel != null && placing)
                statusLabel.text = $"Placing: <b>{selectedSymbol.fullName}</b>  ({selectedSymbol.id})";
        }

        // ── Close ─────────────────────────────────────────────────────────────

        private void SetupCloseButton()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() =>
                {
                    OnCancelClicked();
                    gameObject.SetActive(false);
                });
        }
    }
}
