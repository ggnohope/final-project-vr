using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRDrawing.Geology.UI
{
    /// <summary>
    /// Drives a single symbol button in the palette grid.
    /// Expected prefab hierarchy:
    ///   Root (Button)
    ///     ColorSwatch (Image)          ← left strip with displayColor
    ///     LabelText (TextMeshProUGUI)  ← symbol id, e.g. "SC"
    ///     FullNameText (TextMeshProUGUI) ← e.g. "Sandy Clay"
    ///     SelectedIndicator (GameObject) ← highlight border, toggled by manager
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SymbolPaletteButtonUI : MonoBehaviour
    {
        [SerializeField] private Image colorSwatch;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI fullNameText;
        [SerializeField] private GameObject selectedIndicator;

        private GeologicalSymbolDefinition definition;
        private System.Action<GeologicalSymbolDefinition> onSelected;

        /// <summary>The definition this button is currently bound to.</summary>
        public GeologicalSymbolDefinition BoundDefinition => definition;

        /// <summary>Binds this button to a symbol definition.</summary>
        public void Bind(GeologicalSymbolDefinition def, System.Action<GeologicalSymbolDefinition> callback)
        {
            definition = def;
            onSelected = callback;

            if (colorSwatch != null)
                colorSwatch.color = def.displayColor;

            if (labelText != null)
                labelText.text = def.label;

            if (fullNameText != null)
                fullNameText.text = def.fullName;

            SetSelected(false);

            GetComponent<Button>().onClick.RemoveAllListeners();
            GetComponent<Button>().onClick.AddListener(OnClicked);
        }

        /// <summary>Highlights or un-highlights this button.</summary>
        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(selected);
        }

        private void OnClicked() => onSelected?.Invoke(definition);
    }
}
