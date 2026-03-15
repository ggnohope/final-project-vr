using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRDrawing.Geology.UI
{
    /// <summary>
    /// One row in the Layer Control section of the Legend Canvas.
    /// Expected prefab hierarchy:
    ///   Root
    ///     CategoryLabel (TextMeshProUGUI) ← e.g. "Mixed Soils"
    ///     VisibilityToggle (Toggle)        ← isOn drives layer.SetVisible()
    ///     EyeIcon (Image)                 ← swaps sprite on toggle
    /// </summary>
    public class LayerToggleRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI categoryLabel;
        [SerializeField] private Toggle visibilityToggle;
        [SerializeField] private Image eyeIcon;
        [SerializeField] private Sprite eyeOnSprite;
        [SerializeField] private Sprite eyeOffSprite;
        [SerializeField] private Image toggleBackground;

        private static readonly Color ToggleOnColor  = new Color(0.25f, 0.75f, 0.45f, 1f);
        private static readonly Color ToggleOffColor = new Color(0.45f, 0.45f, 0.48f, 1f);

        private AnnotationLayerState layerState;

        /// <summary>Binds this row to an annotation layer.</summary>
        public void Bind(AnnotationLayerState state)
        {
            layerState = state;

            if (categoryLabel != null)
                categoryLabel.text = state.DisplayName;

            if (visibilityToggle != null)
            {
                visibilityToggle.SetIsOnWithoutNotify(state.IsVisible);
                visibilityToggle.onValueChanged.RemoveAllListeners();
                visibilityToggle.onValueChanged.AddListener(OnToggleChanged);
            }

            RefreshVisuals(state.IsVisible);

            state.OnVisibilityChanged += OnLayerVisibilityChanged;
        }

        private void OnDestroy()
        {
            if (layerState != null)
                layerState.OnVisibilityChanged -= OnLayerVisibilityChanged;
        }

        private void OnToggleChanged(bool isOn)
        {
            layerState?.SetVisible(isOn);
            RefreshVisuals(isOn);
        }

        private void OnLayerVisibilityChanged(AnnotationLayerState state, bool visible)
        {
            if (visibilityToggle != null)
                visibilityToggle.SetIsOnWithoutNotify(visible);

            RefreshVisuals(visible);
        }

        private void RefreshVisuals(bool visible)
        {
            if (eyeIcon != null)
                eyeIcon.sprite = visible ? eyeOnSprite : eyeOffSprite;

            if (toggleBackground != null)
                toggleBackground.color = visible ? ToggleOnColor : ToggleOffColor;
        }
    }
}
