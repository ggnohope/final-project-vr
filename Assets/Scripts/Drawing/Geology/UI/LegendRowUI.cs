using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRDrawing.Geology.UI
{
    /// <summary>
    /// Drives one row in the legend canvas.
    /// Expected prefab hierarchy:
    ///   Root
    ///     ColorSwatch (Image)
    ///     LabelText (TextMeshProUGUI)       ← symbol short label, e.g. "Si"
    ///     FullNameText (TextMeshProUGUI)    ← full name, e.g. "Silt"
    ///     DescriptionText (TextMeshProUGUI) ← description from database
    ///     CountBadge (TextMeshProUGUI)      ← number of placed instances
    ///     VisibilityToggle (Toggle)         ← shows/hides this symbol on the board
    /// </summary>
    public class LegendRowUI : MonoBehaviour
    {
        [SerializeField] private Image colorSwatch;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI fullNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI countBadge;
        [SerializeField] private Toggle visibilityToggle;
        [SerializeField] private Image toggleBackground;

        private static readonly Color ToggleOnColor  = new Color(0.25f, 0.75f, 0.45f, 1f);
        private static readonly Color ToggleOffColor = new Color(0.45f, 0.45f, 0.48f, 1f);

        private GeologicalSymbolDefinition definition;

        public string SymbolId => definition != null ? definition.id : string.Empty;

        /// <summary>Binds the row to a definition. Count starts at 0.</summary>
        public void Bind(GeologicalSymbolDefinition def)
        {
            definition = def;

            if (colorSwatch != null)
                colorSwatch.color = def.displayColor;

            if (labelText != null)
                labelText.text = def.label;

            if (fullNameText != null)
                fullNameText.text = def.fullName;

            if (descriptionText != null)
            {
                descriptionText.text = def.description;
                descriptionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(def.description));
            }

            if (visibilityToggle != null)
            {
                bool currentlyVisible = GeologicalAnnotationManager.Instance == null ||
                                        GeologicalAnnotationManager.Instance.IsSymbolVisible(def.id);
                visibilityToggle.SetIsOnWithoutNotify(currentlyVisible);
                visibilityToggle.onValueChanged.RemoveAllListeners();
                visibilityToggle.onValueChanged.AddListener(OnVisibilityToggleChanged);
            }

            RefreshToggleBackground(visibilityToggle == null || visibilityToggle.isOn);
            SetCount(0);
        }

        /// <summary>Updates the instance count badge.</summary>
        public void SetCount(int count)
        {
            if (countBadge == null) return;
            countBadge.text = count > 0 ? count.ToString() : string.Empty;
            countBadge.gameObject.SetActive(count > 0);
        }

        private void OnVisibilityToggleChanged(bool isOn)
        {
            if (definition == null)
            {
                Debug.LogError("[LegendRowUI] OnVisibilityToggleChanged: definition is null — Bind() was not called.");
                return;
            }

            if (GeologicalAnnotationManager.Instance == null)
            {
                Debug.LogError($"[LegendRowUI] OnVisibilityToggleChanged: GeologicalAnnotationManager.Instance is null. Cannot set visibility for '{definition.id}'.");
                return;
            }

            Debug.Log($"[LegendRowUI] Toggle changed — symbolId='{definition.id}' isOn={isOn}");
            GeologicalAnnotationManager.Instance.SetSymbolVisible(definition.id, isOn);
            RefreshToggleBackground(isOn);
        }

        private void RefreshToggleBackground(bool isOn)
        {
            if (toggleBackground != null)
                toggleBackground.color = isOn ? ToggleOnColor : ToggleOffColor;
        }
    }
}
