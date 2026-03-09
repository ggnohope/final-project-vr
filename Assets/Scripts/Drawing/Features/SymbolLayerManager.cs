using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRDrawing.Data;

namespace VRDrawing.Features
{
    /// <summary>
    /// Manages the SymbolLayer above a DrawingSurface.
    /// Responsible for spawning, removing, querying, and persisting
    /// geological symbol annotations in normalised board coordinates.
    /// </summary>
    public class SymbolLayerManager : MonoBehaviour
    {
        [Header("Layer Offsets")]
        [SerializeField] private float symbolLayerZOffset = -0.003f;   // in front of BaseImage

        [Header("Symbol Scale")]
        [SerializeField] private float symbolWorldScale = 0.05f;

        // Runtime state
        private Transform symbolLayer;
        private DrawingSurface attachedSurface;
        private readonly List<GeologicalSymbolObject> symbols = new List<GeologicalSymbolObject>();
        private AnnotationBoardData boardData = new AnnotationBoardData();

        // Layer visibility flags
        private bool soilLayerVisible = true;
        private bool rockLayerVisible = true;
        private bool noteLayerVisible = true;

        public static SymbolLayerManager Instance { get; private set; }

        // Events
        public System.Action<GeologicalSymbolObject> OnSymbolPlaced;
        public System.Action<GeologicalSymbolObject> OnSymbolRemoved;
        public System.Action<GeologicalSymbolObject> OnSymbolHovered;
        public System.Action<GeologicalSymbolObject> OnSymbolHoverExit;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>
        /// Binds this manager to a specific DrawingSurface and creates the SymbolLayer child.
        /// Call this once after the drawing board is spawned.
        /// </summary>
        public void AttachToSurface(DrawingSurface surface)
        {
            attachedSurface = surface;
            boardData = new AnnotationBoardData();

            // Build the SymbolLayer as a child of the board root (same level as DrawingSurface)
            Transform boardRoot = surface.transform.parent != null
                ? surface.transform.parent
                : surface.transform;

            GameObject layerObj = new GameObject("SymbolLayer");
            layerObj.transform.SetParent(boardRoot, false);
            layerObj.transform.localPosition = new Vector3(0f, 0f, symbolLayerZOffset);
            layerObj.transform.localRotation = Quaternion.identity;
            layerObj.transform.localScale = Vector3.one;
            symbolLayer = layerObj.transform;
        }

        // ── Placement ───────────────────────────────────────────────────────

        /// <summary>
        /// Places a symbol at the given world-space hit point on the board.
        /// </summary>
        public GeologicalSymbolObject PlaceSymbolAtWorldPoint(string symbolCode, Vector3 worldPoint)
        {
            if (attachedSurface == null)
            {
                Debug.LogWarning("[SymbolLayerManager] No surface attached.");
                return null;
            }

            Vector2 uv = attachedSurface.WorldToSurfaceUV(worldPoint);
            return PlaceSymbolAtNormalised(symbolCode, uv.x, uv.y);
        }

        /// <summary>
        /// Places a symbol at normalised coordinates (0–1 on each axis).
        /// </summary>
        public GeologicalSymbolObject PlaceSymbolAtNormalised(string symbolCode, float normX, float normY)
        {
            var def = GeologicalSymbolRegistry.Find(symbolCode);
            if (def == null)
            {
                Debug.LogWarning($"[SymbolLayerManager] Unknown symbol code: '{symbolCode}'");
                return null;
            }

            if (symbolLayer == null)
            {
                Debug.LogWarning("[SymbolLayerManager] SymbolLayer not initialised. Call AttachToSurface first.");
                return null;
            }

            var placedData = new PlacedSymbolData(symbolCode, normX, normY);
            boardData.symbols.Add(placedData);

            return SpawnSymbolObject(placedData, def);
        }

        private GeologicalSymbolObject SpawnSymbolObject(PlacedSymbolData placedData, GeologicalSymbolDefinition def)
        {
            GameObject symbolObj = new GameObject($"Symbol_{def.code}_{placedData.id.Substring(0, 4)}");
            symbolObj.transform.SetParent(symbolLayer, false);

            // Convert normalised coords back to local position on the board
            Vector3 localPos = NormalisedToLocalPosition(placedData.x, placedData.y);
            symbolObj.transform.localPosition = localPos;
            symbolObj.transform.localScale = Vector3.one * symbolWorldScale;

            var symbolComp = symbolObj.AddComponent<GeologicalSymbolObject>();
            symbolComp.Initialise(placedData, def);

            // Wire hover/select events
            symbolComp.OnHoverEnter += sym => OnSymbolHovered?.Invoke(sym);
            symbolComp.OnHoverExit  += sym => OnSymbolHoverExit?.Invoke(sym);

            // Apply current visibility
            ApplyVisibilityToSymbol(symbolComp);

            symbols.Add(symbolComp);
            OnSymbolPlaced?.Invoke(symbolComp);

            return symbolComp;
        }

        // ── Removal ─────────────────────────────────────────────────────────

        /// <summary>Removes a specific symbol from the board.</summary>
        public void RemoveSymbol(GeologicalSymbolObject symbol)
        {
            if (symbol == null) return;

            boardData.symbols.Remove(symbol.Data);
            symbols.Remove(symbol);
            OnSymbolRemoved?.Invoke(symbol);
            Destroy(symbol.gameObject);
        }

        /// <summary>Removes all symbols from the board.</summary>
        public void ClearAll()
        {
            foreach (var sym in symbols)
            {
                if (sym != null) Destroy(sym.gameObject);
            }
            symbols.Clear();
            boardData.symbols.Clear();
        }

        // ── Layer Visibility ────────────────────────────────────────────────

        /// <summary>Toggles visibility of all symbols in a given category.</summary>
        public void SetLayerVisible(SymbolCategory category, bool visible)
        {
            switch (category)
            {
                case SymbolCategory.Soil: soilLayerVisible = visible; break;
                case SymbolCategory.Rock: rockLayerVisible = visible; break;
                case SymbolCategory.Note: noteLayerVisible = visible; break;
            }

            foreach (var sym in symbols)
                ApplyVisibilityToSymbol(sym);
        }

        private void ApplyVisibilityToSymbol(GeologicalSymbolObject sym)
        {
            if (sym == null || sym.Definition == null) return;
            bool visible = sym.Definition.category switch
            {
                SymbolCategory.Soil => soilLayerVisible,
                SymbolCategory.Rock => rockLayerVisible,
                SymbolCategory.Note => noteLayerVisible,
                _ => true
            };
            sym.gameObject.SetActive(visible);
        }

        // ── Move ────────────────────────────────────────────────────────────

        /// <summary>Moves an existing symbol to a new world-space position on the board.</summary>
        public void MoveSymbol(GeologicalSymbolObject symbol, Vector3 newWorldPoint)
        {
            if (symbol == null || attachedSurface == null) return;

            Vector2 uv = attachedSurface.WorldToSurfaceUV(newWorldPoint);
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);

            symbol.Data.x = uv.x;
            symbol.Data.y = uv.y;
            symbol.transform.localPosition = NormalisedToLocalPosition(uv.x, uv.y);
        }

        // ── Persistence ─────────────────────────────────────────────────────

        /// <summary>Saves annotations to a JSON file at the given path.</summary>
        public void SaveAnnotations(string filePath)
        {
            try
            {
                string json = JsonUtility.ToJson(boardData, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"[SymbolLayerManager] Annotations saved to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SymbolLayerManager] Save failed: {e.Message}");
            }
        }

        /// <summary>Loads annotations from a JSON file and re-spawns all symbols.</summary>
        public void LoadAnnotations(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SymbolLayerManager] File not found: {filePath}");
                return;
            }

            try
            {
                ClearAll();
                string json = File.ReadAllText(filePath);
                boardData = JsonUtility.FromJson<AnnotationBoardData>(json);

                foreach (var placed in boardData.symbols)
                {
                    var def = GeologicalSymbolRegistry.Find(placed.type);
                    if (def != null) SpawnSymbolObject(placed, def);
                }

                Debug.Log($"[SymbolLayerManager] Loaded {boardData.symbols.Count} symbols from {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SymbolLayerManager] Load failed: {e.Message}");
            }
        }

        // ── Queries ─────────────────────────────────────────────────────────

        public IReadOnlyList<GeologicalSymbolObject> GetAllSymbols() => symbols;

        // ── Utility ─────────────────────────────────────────────────────────

        private Vector3 NormalisedToLocalPosition(float normX, float normY)
        {
            if (attachedSurface == null) return Vector3.zero;

            Vector2 size = attachedSurface.GetSurfaceSize();
            float x = (normX - 0.5f) * size.x;
            float y = (normY - 0.5f) * size.y;
            return new Vector3(x, y, 0f);
        }
    }
}
