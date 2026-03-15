using UnityEngine;
using System.Collections.Generic;
using VRDrawing.Tools;

namespace VRDrawing.Geology
{
    /// <summary>
    /// Central manager for geological symbol annotations on the drawing board.
    /// Owns all SymbolInstances and AnnotationLayerStates.
    /// Notifies SymbolOverlayRenderer and UI components via events.
    /// </summary>
    public class GeologicalAnnotationManager : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private GeologicalSymbolDatabase database;

        [Header("Annotation Settings")]
        [SerializeField] private float defaultSymbolScale = 0.012f;

        public static GeologicalAnnotationManager Instance { get; private set; }

        // ── State ────────────────────────────────────────────────────────────

        private readonly List<SymbolInstance> instances = new List<SymbolInstance>();
        private readonly Dictionary<SymbolCategory, AnnotationLayerState> layers =
            new Dictionary<SymbolCategory, AnnotationLayerState>();

        private GeologicalSymbolDefinition pendingSymbol;
        private bool isInAnnotationMode;

        // ── Public accessors ─────────────────────────────────────────────────

        public GeologicalSymbolDatabase Database => database;
        public IReadOnlyList<SymbolInstance> Instances => instances;
        public bool IsInAnnotationMode => isInAnnotationMode;
        public GeologicalSymbolDefinition PendingSymbol => pendingSymbol;

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Raised after a symbol is placed on the surface.</summary>
        public System.Action<SymbolInstance> OnSymbolPlaced;

        /// <summary>Raised after a symbol instance is removed.</summary>
        public System.Action<SymbolInstance> OnSymbolRemoved;

        /// <summary>Raised when all instances are cleared.</summary>
        public System.Action OnAllCleared;

        /// <summary>
        /// Raised when per-symbol visibility changes.
        /// Parameters: symbolId, isVisible.
        /// </summary>
        public System.Action<string, bool> OnSymbolVisibilityChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            if (database == null)
                database = Resources.Load<GeologicalSymbolDatabase>("Geology/GeologicalSymbolDatabase");

            if (database == null)
                Debug.LogError("[GeologicalAnnotationManager] GeologicalSymbolDatabase not found in Resources/Geology/.");

            InitLayers();
        }

        private void InitLayers()
        {
            foreach (SymbolCategory cat in System.Enum.GetValues(typeof(SymbolCategory)))
            {
                var layer = new AnnotationLayerState(cat);
                layer.OnVisibilityChanged += HandleLayerVisibilityChanged;
                layers[cat] = layer;
            }
        }

        // ── Annotation mode ───────────────────────────────────────────────────

        /// <summary>
        /// Selects a symbol definition and enters annotation mode.
        /// UIRayDrawingTool is disabled while annotation mode is active.
        /// </summary>
        public void SelectSymbol(GeologicalSymbolDefinition symbol)
        {
            pendingSymbol = symbol;
            isInAnnotationMode = symbol != null;
            SetDrawingEnabled(!isInAnnotationMode);
        }

        /// <summary>Cancels annotation mode without placing a symbol.</summary>
        public void CancelAnnotationMode()
        {
            pendingSymbol = null;
            isInAnnotationMode = false;
            SetDrawingEnabled(true);
        }

        // ── Placement ─────────────────────────────────────────────────────────

        /// <summary>
        /// Places the pending symbol at the given surface UV coordinate.
        /// Called by SymbolOverlayRenderer when the user clicks on the board.
        /// </summary>
        public SymbolInstance PlaceSymbol(Vector2 surfaceUV)
        {
            if (pendingSymbol == null)
            {
                Debug.LogWarning("[GeologicalAnnotationManager] No symbol selected.");
                return null;
            }

            var instance = new SymbolInstance(pendingSymbol.id, surfaceUV, defaultSymbolScale);
            instances.Add(instance);

            OnSymbolPlaced?.Invoke(instance);

            // Keep annotation mode active so the user can place multiple instances.
            return instance;
        }

        /// <summary>
        /// Removes a specific symbol instance by its instanceId.
        /// </summary>
        public void RemoveSymbol(string instanceId)
        {
            SymbolInstance target = instances.Find(i => i.instanceId == instanceId);
            if (target == null) return;

            instances.Remove(target);
            OnSymbolRemoved?.Invoke(target);
        }

        /// <summary>Clears all annotation instances.</summary>
        public void ClearAll()
        {
            instances.Clear();
            OnAllCleared?.Invoke();
        }

        // ── Per-symbol visibility ─────────────────────────────────────────────

        private readonly HashSet<string> hiddenSymbolIds = new HashSet<string>();

        /// <summary>
        /// Shows or hides all placed instances of a specific symbol id on the board.
        /// Raises OnSymbolVisibilityChanged.
        /// </summary>
        public void SetSymbolVisible(string symbolId, bool visible)
        {
            if (visible)
                hiddenSymbolIds.Remove(symbolId);
            else
                hiddenSymbolIds.Add(symbolId);

            int listenerCount = OnSymbolVisibilityChanged?.GetInvocationList().Length ?? 0;
            Debug.Log($"[GeologicalAnnotationManager] SetSymbolVisible: symbolId='{symbolId}' visible={visible} — {listenerCount} listener(s) on OnSymbolVisibilityChanged.");

            OnSymbolVisibilityChanged?.Invoke(symbolId, visible);
        }

        /// <summary>Returns whether the given symbol id is currently visible.</summary>
        public bool IsSymbolVisible(string symbolId) => !hiddenSymbolIds.Contains(symbolId);

        // ── Layer management ──────────────────────────────────────────────────

        /// <summary>Returns the layer state for the given category.</summary>
        public AnnotationLayerState GetLayer(SymbolCategory category)
        {
            return layers.TryGetValue(category, out var layer) ? layer : null;
        }

        public IReadOnlyDictionary<SymbolCategory, AnnotationLayerState> Layers => layers;

        private void HandleLayerVisibilityChanged(AnnotationLayerState layer, bool visible)
        {
            // SymbolOverlayRenderer subscribes to this via the layer's own event.
            // No extra work needed here — just propagation.
            Debug.Log($"[GeologicalAnnotationManager] Layer '{layer.DisplayName}' visibility → {visible}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Resolves a GeologicalSymbolDefinition by its id string.</summary>
        public GeologicalSymbolDefinition Resolve(string symbolId)
        {
            return database != null ? database.FindById(symbolId) : null;
        }

        /// <summary>Returns all instances belonging to a given category.</summary>
        public List<SymbolInstance> GetInstancesByCategory(SymbolCategory category)
        {
            return instances.FindAll(i =>
            {
                GeologicalSymbolDefinition def = Resolve(i.symbolId);
                return def != null && def.category == category;
            });
        }

        /// <summary>Returns the number of placed instances for a specific symbol id.</summary>
        public int GetInstanceCountById(string symbolId)
        {
            int count = 0;
            foreach (var inst in instances)
                if (inst.symbolId == symbolId) count++;
            return count;
        }

        private void SetDrawingEnabled(bool enabled)
        {
            UIRayDrawingTool tool = FindFirstObjectByType<UIRayDrawingTool>();
            tool?.SetEnabled(enabled);
        }
    }
}
