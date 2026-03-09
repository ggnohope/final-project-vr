using UnityEngine;
using UnityEngine.UI;

namespace VRDrawing.UI
{
    /// <summary>
    /// Button on the DrawingToolPanel that toggles the SymbolToolMenuUI canvas.
    /// Wire this component's Button reference in the Inspector, or call Toggle() directly.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SymbolMenuToggleButton : MonoBehaviour
    {
        [Header("Target Canvas")]
        [SerializeField] private SymbolToolMenuUI symbolToolMenuUI;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Toggle);

            if (symbolToolMenuUI == null)
                symbolToolMenuUI = FindFirstObjectByType<SymbolToolMenuUI>(FindObjectsInactive.Include);
        }

        /// <summary>Toggles the symbol tool menu open/closed.</summary>
        public void Toggle()
        {
            if (symbolToolMenuUI == null) return;
            symbolToolMenuUI.ToggleMenu();
        }
    }
}
