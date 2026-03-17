using UnityEngine;
using TMPro;
using System.Collections.Generic;
using VRDrawing.UI;

namespace VRDrawing.Geology
{
    [RequireComponent(typeof(DrawingSurface))]
    public class SymbolOverlayRenderer : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private Material overlayMaterialTemplate;
        [SerializeField] private TMP_FontAsset labelFont;

        [Header("Symbol Appearance")]
        [SerializeField] [Range(0f,1f)] private float overlayAlpha = 0.55f;
        [SerializeField] [Range(0f,1f)] private float labelAlpha = 0.55f;

        [Header("Size Mapping")]
        [SerializeField] private float thicknessToSizeMultiplier = 4f;
        [SerializeField] private float minSymbolSize = 0.004f;
        [SerializeField] private float maxSymbolSize = 0.04f;

        [Header("Shape")]
        [Tooltip("Segments for the circle mesh — 32 gives a smooth disc.")]
        [SerializeField] private int circleSegments = 32;

        [Header("Draw Behaviour")]
        [Tooltip("Minimum UV-space distance between consecutive symbols (0–1 surface range). " +
                 "Lower = denser fill. Decoupled from viewer distance.")]
        [SerializeField] private float drawSpacing = 0.1f;

        private void Reset()
        {
            // Called by Unity when component is first added or user clicks Reset in Inspector.
            // Forces drawSpacing back to the intended default, bypassing stale serialized values.
            drawSpacing = 0.008f;
        }

        private const float LabelFontSizePerUnit = 8f;

        private const string OverlayRootName = "AnnotationOverlayRoot";
        private const float LabelZOffset = -0.01f;
        private const float OverlayZOffset = -0.008f;

        private readonly Dictionary<string, GameObject> renderedObjects =
            new Dictionary<string, GameObject>();

        // Maps instanceId → symbolId so HandleSymbolVisibilityChanged can filter by symbol type.
        private readonly Dictionary<string, string> symbolIdByInstanceId =
            new Dictionary<string, string>();

        private readonly Dictionary<SymbolCategory, GameObject> layerRoots =
            new Dictionary<SymbolCategory, GameObject>();

        private DrawingSurface surface;
        private Transform overlayRoot;
        private GeologicalAnnotationManager manager;

        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor cachedRayInteractor;
        private VRDrawing.Tools.UIRayDrawingTool cachedDrawingTool;
        private DrawingToolPanel cachedToolPanel;

        private Vector2 lastDrawUV;
        private bool hasLastPoint = false;

        // Pushed by DrawingToolPanel.NotifyThicknessChange() on every slider change.
        // -1 means "not yet received from panel — fall back to midpoint".
        private float cachedThickness = -1f;

        /// <summary>Called by DrawingToolPanel to keep symbol size in sync with the thickness slider.</summary>
        public void SetSymbolSize(float rawThickness) => cachedThickness = rawThickness;

        private static Mesh circleMesh;
        private static int builtCircleSegments = 0;

        private int ignoreRaycastLayer;

        private void Awake()
        {
            surface = GetComponent<DrawingSurface>();
            ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            EnsureTransparentMaterialTemplate();
            EnsureOverlayRoot();
        }

        private void Start()
        {
            cachedRayInteractor =
                FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();

            cachedToolPanel = FindFirstObjectByType<DrawingToolPanel>();

            cachedDrawingTool = FindFirstObjectByType<VRDrawing.Tools.UIRayDrawingTool>();

            // OnEnable fires before Start; if manager was null at that point, subscribe now.
            if (manager == null)
            {
                manager = GeologicalAnnotationManager.Instance
                       ?? FindFirstObjectByType<GeologicalAnnotationManager>();

                if (manager != null)
                {
                    Debug.Log("[SymbolOverlayRenderer] Start: manager resolved late — subscribing now.");
                    SubscribeToManager();
                }
                else
                {
                    Debug.LogError("[SymbolOverlayRenderer] Start: GeologicalAnnotationManager still not found. Symbol visibility toggle will not work.");
                }
            }
        }

        private void OnEnable()
        {
            manager = GeologicalAnnotationManager.Instance
                   ?? FindFirstObjectByType<GeologicalAnnotationManager>();

            if (manager == null)
            {
                Debug.LogWarning("[SymbolOverlayRenderer] OnEnable: GeologicalAnnotationManager not found yet — will retry in Start().");
                return;
            }

            Debug.Log("[SymbolOverlayRenderer] OnEnable: subscribing to manager events.");
            SubscribeToManager();
        }

        private void OnDisable()
        {
            if (manager == null) return;

            UnsubscribeFromManager();
        }

        /// <summary>Subscribes to all manager and layer events. Safe to call multiple times (removes before adding).</summary>
        private void SubscribeToManager()
        {
            // Remove first to avoid duplicate subscriptions on re-enable.
            manager.OnSymbolPlaced            -= HandleSymbolPlaced;
            manager.OnSymbolRemoved           -= HandleSymbolRemoved;
            manager.OnAllCleared              -= HandleAllCleared;
            manager.OnSymbolVisibilityChanged -= HandleSymbolVisibilityChanged;

            manager.OnSymbolPlaced            += HandleSymbolPlaced;
            manager.OnSymbolRemoved           += HandleSymbolRemoved;
            manager.OnAllCleared              += HandleAllCleared;
            manager.OnSymbolVisibilityChanged += HandleSymbolVisibilityChanged;

            foreach (var kvp in manager.Layers)
            {
                kvp.Value.OnVisibilityChanged -= HandleLayerVisibilityChanged;
                kvp.Value.OnVisibilityChanged += HandleLayerVisibilityChanged;
            }
        }

        /// <summary>Unsubscribes from all manager and layer events.</summary>
        private void UnsubscribeFromManager()
        {
            manager.OnSymbolPlaced            -= HandleSymbolPlaced;
            manager.OnSymbolRemoved           -= HandleSymbolRemoved;
            manager.OnAllCleared              -= HandleAllCleared;
            manager.OnSymbolVisibilityChanged -= HandleSymbolVisibilityChanged;

            foreach (var kvp in manager.Layers)
                kvp.Value.OnVisibilityChanged -= HandleLayerVisibilityChanged;
        }

        private void Update()
        {
            if (manager == null || !manager.IsInAnnotationMode) return;
            if (cachedRayInteractor == null || cachedDrawingTool == null) return;

            bool held = cachedDrawingTool.IsSelectHeld();

            if (!held)
            {
                hasLastPoint = false;
                return;
            }

            if (!cachedRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                return;

            if (hit.collider.gameObject != gameObject)
                return;

            Vector2 uv = surface.WorldToSurfaceUV(hit.point);

            float dist = hasLastPoint ? Vector2.Distance(uv, lastDrawUV) : float.MaxValue;

            if (dist < drawSpacing)
                return;

            lastDrawUV   = uv;
            hasLastPoint = true;

            manager.PlaceSymbol(uv);
        }

        private void HandleSymbolPlaced(SymbolInstance instance)
        {
            GeologicalSymbolDefinition def = manager.Resolve(instance.symbolId);
            if (def == null) return;

            SpawnSymbolObject(instance, def);
        }

        private void HandleSymbolRemoved(SymbolInstance instance)
        {
            if (!renderedObjects.TryGetValue(instance.instanceId, out GameObject obj))
                return;

            Destroy(obj);
            renderedObjects.Remove(instance.instanceId);
            symbolIdByInstanceId.Remove(instance.instanceId);
        }

        private void HandleAllCleared()
        {
            foreach (var obj in renderedObjects.Values)
                if (obj != null) Destroy(obj);

            renderedObjects.Clear();
            symbolIdByInstanceId.Clear();
        }

        private void HandleLayerVisibilityChanged(AnnotationLayerState layer, bool visible)
        {
            if (layerRoots.TryGetValue(layer.Category, out GameObject root))
                root.SetActive(visible);
        }

        /// <summary>
        /// Shows or hides all rendered objects whose symbolId matches.
        /// Called when the user toggles a specific symbol in the legend.
        /// </summary>
        private void HandleSymbolVisibilityChanged(string symbolId, bool visible)
        {
            int affected = 0;

            foreach (var kvp in renderedObjects)
            {
                if (!symbolIdByInstanceId.TryGetValue(kvp.Key, out string sid) || sid != symbolId)
                    continue;

                if (kvp.Value == null)
                {
                    Debug.LogWarning($"[SymbolOverlayRenderer] HandleSymbolVisibilityChanged: rendered object for instance '{kvp.Key}' is null.");
                    continue;
                }

                // Respect layer visibility: only show if the parent layer is also active.
                if (visible)
                {
                    GeologicalSymbolDefinition def = manager.Resolve(symbolId);
                    bool layerVisible = def == null || (manager.GetLayer(def.category)?.IsVisible ?? true);
                    kvp.Value.SetActive(layerVisible);
                }
                else
                {
                    kvp.Value.SetActive(false);
                }

                affected++;
            }

            Debug.Log($"[SymbolOverlayRenderer] HandleSymbolVisibilityChanged: symbolId='{symbolId}' visible={visible} — {affected} object(s) updated.");
        }

        private void SpawnSymbolObject(SymbolInstance instance, GeologicalSymbolDefinition def)
        {
            Transform layerRoot = GetOrCreateLayerRoot(def.category);

            string shortId = instance.instanceId.Length >= 8
                ? instance.instanceId.Substring(0, 8)
                : instance.instanceId;

            GameObject symbolRoot = new GameObject($"Symbol_{def.id}_{shortId}");
            symbolRoot.transform.SetParent(layerRoot);
            symbolRoot.layer = ignoreRaycastLayer;

            Vector3 worldPos = surface.SurfaceUVToWorld(instance.surfaceUV);

            symbolRoot.transform.position = worldPos;
            symbolRoot.transform.rotation =
                surface.transform.rotation *
                Quaternion.Euler(0f,0f,instance.rotationDegrees);

            symbolRoot.transform.localScale = Vector3.one;

            float size = ResolveSymbolSize();

            CreateOverlayQuad(symbolRoot, def, size);
            CreateLabel(symbolRoot, def, size);

            renderedObjects[instance.instanceId]       = symbolRoot;
            symbolIdByInstanceId[instance.instanceId]  = def.id;

            // Apply current per-symbol visibility immediately.
            if (!manager.IsSymbolVisible(def.id))
                symbolRoot.SetActive(false);
        }

        private void CreateOverlayQuad(GameObject parent, GeologicalSymbolDefinition def, float scale)
        {
            GameObject disc = new GameObject("Overlay");
            disc.transform.SetParent(parent.transform);
            disc.layer = ignoreRaycastLayer;
            disc.transform.localPosition = new Vector3(0, 0, OverlayZOffset);
            disc.transform.localRotation = Quaternion.identity;
            disc.transform.localScale    = new Vector3(scale, scale, 1f);

            Mesh mesh = GetCircleMesh(circleSegments);

            MeshFilter mf = disc.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            Material mat = new Material(overlayMaterialTemplate);
            Color c = def.displayColor;
            c.a = overlayAlpha;
            mat.color = c;
            mat.SetColor("_BaseColor", c);
            // Disable back-face culling — disc may face away from camera depending on surface normal.
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

            MeshRenderer mr = disc.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
        }

        /// <summary>
        /// Ensures the overlay material template is set up for alpha-blended transparency
        /// using URP Unlit shader keywords and render states.
        /// </summary>
        private void EnsureTransparentMaterialTemplate()
        {
            if (overlayMaterialTemplate == null)
                overlayMaterialTemplate = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

            // Clone to avoid mutating a shared project asset.
            overlayMaterialTemplate = new Material(overlayMaterialTemplate);

            // Surface type: 1 = Transparent
            overlayMaterialTemplate.SetFloat("_Surface", 1f);
            // Blend mode: 0 = Alpha
            overlayMaterialTemplate.SetFloat("_Blend", 0f);
            overlayMaterialTemplate.SetFloat("_AlphaClip", 0f);

            // Enable the URP transparency keyword and correct blend states.
            overlayMaterialTemplate.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            overlayMaterialTemplate.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            overlayMaterialTemplate.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            overlayMaterialTemplate.SetInt("_ZWrite", 0);
            overlayMaterialTemplate.SetInt("_Cull",   (int)UnityEngine.Rendering.CullMode.Off);

            overlayMaterialTemplate.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private void CreateLabel(GameObject parent, GeologicalSymbolDefinition def, float symbolSize)
        {
            GameObject labelObj = new GameObject("Label");

            labelObj.transform.SetParent(parent.transform);
            labelObj.layer = ignoreRaycastLayer;

            labelObj.transform.localPosition = new Vector3(0, 0, LabelZOffset);
            labelObj.transform.localRotation  = Quaternion.identity;

            TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();

            tmp.text      = def.label;
            tmp.fontSize  = symbolSize * LabelFontSizePerUnit;
            tmp.alignment = TextAlignmentOptions.Center;

            Color labelColor = GetContrastColor(def.displayColor);
            labelColor.a = labelAlpha;
            tmp.color    = labelColor;

            if (labelFont != null)
                tmp.font = labelFont;
        }

        private float ResolveSymbolSize()
        {
            float raw;

            if (cachedThickness >= 0f)
            {
                // Thickness was pushed by DrawingToolPanel — use it directly.
                raw = cachedThickness;
            }
            else if (cachedToolPanel != null)
            {
                // Fallback: pull from panel if push hasn't arrived yet.
                raw = cachedToolPanel.CurrentThickness;
            }
            else
            {
                // Last resort: use the midpoint of the inspector range.
                raw = (minSymbolSize + maxSymbolSize) * 0.5f / thicknessToSizeMultiplier;
            }

            return Mathf.Clamp(raw * thicknessToSizeMultiplier, minSymbolSize, maxSymbolSize);
        }

        private void EnsureOverlayRoot()
        {
            Transform existing = surface.transform.Find(OverlayRootName);

            if (existing != null)
            {
                overlayRoot = existing;
                return;
            }

            GameObject root = new GameObject(OverlayRootName);

            root.transform.SetParent(surface.transform);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            overlayRoot = root.transform;
        }

        private Transform GetOrCreateLayerRoot(SymbolCategory category)
        {
            if (layerRoots.TryGetValue(category, out GameObject existing))
                return existing.transform;

            GameObject layer = new GameObject($"Layer_{category}");

            layer.transform.SetParent(overlayRoot);

            AnnotationLayerState state = manager?.GetLayer(category);

            if (state != null)
                layer.SetActive(state.IsVisible);

            layerRoots[category] = layer;

            return layer.transform;
        }

        /// <summary>
        /// Returns a cached unit-circle disc mesh centred at the origin, radius 0.5.
        /// The mesh is rebuilt if the segment count changes.
        /// </summary>
        private static Mesh GetCircleMesh(int segments)
        {
            if (circleMesh != null && builtCircleSegments == segments)
                return circleMesh;

            segments = Mathf.Max(6, segments);
            builtCircleSegments = segments;

            circleMesh = new Mesh { name = "SymbolCircle" };

            // Centre vertex + one vertex per segment edge.
            Vector3[] verts = new Vector3[segments + 1];
            Vector2[] uvs   = new Vector2[segments + 1];
            int[]   tris  = new int[segments * 3];

            verts[0] = Vector3.zero;
            uvs[0]   = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float x = 0.5f * Mathf.Cos(angle);
                float y = 0.5f * Mathf.Sin(angle);
                verts[i + 1] = new Vector3(x, y, 0f);
                uvs[i + 1]   = new Vector2(x + 0.5f, y + 0.5f);

                int next = (i + 1) % segments + 1;
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = next;
            }

            circleMesh.vertices  = verts;
            circleMesh.uv        = uvs;
            circleMesh.triangles = tris;
            circleMesh.RecalculateNormals();
            circleMesh.RecalculateBounds();

            return circleMesh;
        }

        private static Color GetContrastColor(Color bg)
        {
            float luminance =
                0.299f * bg.r +
                0.587f * bg.g +
                0.114f * bg.b;

            return luminance > 0.5f ? Color.black : Color.white;
        }
    }
}