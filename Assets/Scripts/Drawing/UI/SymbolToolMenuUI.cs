using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRDrawing.Data;
using VRDrawing.Features;

namespace VRDrawing.UI
{
    /// <summary>
    /// World-space canvas that shows all geological symbols grouped by category.
    /// Selecting a symbol button sets the active code on SymbolPlacementTool.
    /// Toggling a layer checkbox calls SymbolLayerManager.SetLayerVisible.
    /// </summary>
    public class SymbolToolMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private Transform soilButtonContainer;
        [SerializeField] private Transform rockButtonContainer;
        [SerializeField] private Button symbolToolButton;        // Button on the DrawingToolPanel that opens this menu

        [Header("Layer Toggles")]
        [SerializeField] private Toggle soilLayerToggle;
        [SerializeField] private Toggle rockLayerToggle;
        [SerializeField] private Toggle noteLayerToggle;

        [Header("Symbol Button Prefab")]
        [SerializeField] private GameObject symbolButtonPrefab;   // Button + TMP child

        [Header("Save Button")]
        [SerializeField] private Button saveButton;
        [SerializeField] private string saveDirectory = "AnnotationSaves";

        [Header("Placement Tool Reference")]
        [SerializeField] private SymbolPlacementTool placementTool;

        private bool menuOpen = false;

        private void Awake()
        {
            // Auto-find containers by name if not wired in Inspector
            if (soilButtonContainer == null)
                soilButtonContainer = FindChildByName(transform, "SoilButtonContainer");
            if (rockButtonContainer == null)
                rockButtonContainer = FindChildByName(transform, "RockButtonContainer");

            Debug.Log($"[SymbolToolMenuUI] Awake — soilContainer={soilButtonContainer?.name ?? "NULL"}, " +
                      $"rockContainer={rockButtonContainer?.name ?? "NULL"}, prefab={symbolButtonPrefab?.name ?? "NULL"}");
        }

        private void Start()
        {
            if (symbolToolButton != null)
                symbolToolButton.onClick.AddListener(ToggleMenu);

            if (placementTool == null)
                placementTool = FindFirstObjectByType<SymbolPlacementTool>();

            if (saveButton != null)
                saveButton.onClick.AddListener(SaveAnnotations);

            SetupLayerToggles();

            // If the canvas is already active at Start (e.g. in Scene view), build now.
            // Otherwise BuildAllButtons() will be called by DrawingModeSymbolMenuController.
            if (gameObject.activeInHierarchy)
                BuildAllButtons();
        }

        /// <summary>Recursively searches children for a Transform with the given name.</summary>
        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }

        /// <summary>
        /// Builds symbol buttons for all categories.
        /// Safe to call multiple times — clears existing buttons first.
        /// Called by DrawingModeSymbolMenuController after the canvas is activated,
        /// because GridLayoutGroup requires the canvas to be active for layout.
        /// </summary>
        public void BuildAllButtons()
        {
            Debug.Log($"[SymbolToolMenuUI] BuildAllButtons() called. soilContainer={soilButtonContainer}, rockContainer={rockButtonContainer}, prefab={symbolButtonPrefab}");

            if (soilButtonContainer == null) Debug.LogError("[SymbolToolMenuUI] soilButtonContainer is NULL — assign it in the Inspector.");
            if (rockButtonContainer == null) Debug.LogError("[SymbolToolMenuUI] rockButtonContainer is NULL — assign it in the Inspector.");
            if (symbolButtonPrefab == null)  Debug.LogError("[SymbolToolMenuUI] symbolButtonPrefab is NULL — assign it in the Inspector.");

            // Clear any previous children first (avoid duplicates on re-enter)
            ClearContainer(soilButtonContainer);
            ClearContainer(rockButtonContainer);

            int soilCount = BuildSymbolButtons(soilButtonContainer, SymbolCategory.Soil);
            int rockCount = BuildSymbolButtons(rockButtonContainer, SymbolCategory.Rock);
            Debug.Log($"[SymbolToolMenuUI] Built {soilCount} soil buttons, {rockCount} rock buttons.");

            // Force immediate layout rebuild so GridLayoutGroup sizes correctly
            if (soilButtonContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(soilButtonContainer as RectTransform);
            if (rockButtonContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rockButtonContainer as RectTransform);

            // Wire save button if not already done
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(SaveAnnotations);
                saveButton.onClick.AddListener(SaveAnnotations);
            }

            SetupLayerToggles();
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        public void ToggleMenu()
        {
            menuOpen = !menuOpen;
            // Toggle content visibility rather than the root canvas,
            // so TrackedDeviceGraphicRaycaster stays alive.
            SetContentVisible(menuOpen);

            if (placementTool != null)
                placementTool.SetEnabled(menuOpen);
        }

        public void OpenMenu()
        {
            if (menuOpen) return;
            menuOpen = true;
            SetContentVisible(true);
            if (placementTool != null) placementTool.SetEnabled(true);
        }

        public void CloseMenu()
        {
            if (!menuOpen) return;
            menuOpen = false;
            SetContentVisible(false);
            if (placementTool != null) placementTool.SetEnabled(false);
        }

        /// <summary>Shows or hides all direct children of this canvas.</summary>
        private void SetContentVisible(bool visible)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(visible);
        }

        private int BuildSymbolButtons(Transform container, SymbolCategory category)
        {
            if (container == null || symbolButtonPrefab == null) return 0;

            var defs = GeologicalSymbolRegistry.GetByCategory(category);
            if (defs == null || defs.Count == 0)
            {
                Debug.LogWarning($"[SymbolToolMenuUI] No symbols found for category: {category}");
                return 0;
            }

            int count = 0;
            foreach (var def in defs)
            {
                var local = def;
                GameObject btnObj = Instantiate(symbolButtonPrefab, container);
                btnObj.SetActive(true);

                TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text  = local.code;
                    tmp.color = local.color;
                }

                TextMeshProUGUI[] labels = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (labels.Length >= 2) labels[1].text = local.fullName;

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    ColorBlock cb = btn.colors;
                    cb.normalColor = local.color * 0.3f + Color.white * 0.7f;
                    btn.colors = cb;
                    btn.onClick.AddListener(() => OnSymbolButtonClicked(local.code));
                }
                count++;
            }
            return count;
        }

        private void SetupLayerToggles()
        {
            if (soilLayerToggle != null)
            {
                soilLayerToggle.isOn = true;
                soilLayerToggle.onValueChanged.AddListener(v =>
                    SymbolLayerManager.Instance?.SetLayerVisible(SymbolCategory.Soil, v));
            }

            if (rockLayerToggle != null)
            {
                rockLayerToggle.isOn = true;
                rockLayerToggle.onValueChanged.AddListener(v =>
                    SymbolLayerManager.Instance?.SetLayerVisible(SymbolCategory.Rock, v));
            }

            if (noteLayerToggle != null)
            {
                noteLayerToggle.isOn = true;
                noteLayerToggle.onValueChanged.AddListener(v =>
                    SymbolLayerManager.Instance?.SetLayerVisible(SymbolCategory.Note, v));
            }
        }

        private void OnSymbolButtonClicked(string code)
        {
            if (placementTool != null)
                placementTool.SelectSymbol(code);

            Debug.Log($"[SymbolToolMenuUI] Selected symbol: {code}");
        }

        private void SaveAnnotations()
        {
            // Prefer the dedicated controller if present
            if (VRDrawing.Features.AnnotationSaveController.Instance != null)
            {
                VRDrawing.Features.AnnotationSaveController.Instance.SaveCurrentAnnotations();
                return;
            }

            // Fallback: inline save
            if (SymbolLayerManager.Instance == null) return;

            string dir = System.IO.Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            string filename = $"annotation_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = System.IO.Path.Combine(dir, filename);
            SymbolLayerManager.Instance.SaveAnnotations(path);
            Debug.Log($"[SymbolToolMenuUI] Saved to {path}");
        }
    }
}
