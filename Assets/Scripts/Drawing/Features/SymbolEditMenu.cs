using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRDrawing.Data;

namespace VRDrawing.Features
{
    /// <summary>
    /// World-space UI panel that opens near a selected symbol and allows the user to:
    /// Move, Change type, Delete, or Add note.
    /// Attach this to the EditMenu Canvas GameObject in the DrawingBoard hierarchy.
    /// </summary>
    public class SymbolEditMenu : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button changeTypeButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button addNoteButton;
        [SerializeField] private Button closeButton;

        [Header("Info Label")]
        [SerializeField] private TextMeshProUGUI symbolInfoLabel;

        [Header("Type Picker")]
        [SerializeField] private GameObject typePickerPanel;

        private GeologicalSymbolObject targetSymbol;

        private void Awake()
        {
            // Keep the canvas hidden until a symbol is selected.
            // Use the Canvas component to hide, not the root GameObject,
            // so the component is accessible to SymbolPlacementTool.
            gameObject.SetActive(false);
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (moveButton != null)   moveButton.onClick.AddListener(OnMoveClicked);
            if (changeTypeButton != null) changeTypeButton.onClick.AddListener(OnChangeTypeClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (addNoteButton != null) addNoteButton.onClick.AddListener(OnAddNoteClicked);
            if (closeButton != null)  closeButton.onClick.AddListener(Close);
        }

        /// <summary>Opens the edit menu anchored near the given symbol.</summary>
        public void Open(GeologicalSymbolObject symbol)
        {
            if (symbol == null) return;

            targetSymbol = symbol;

            // Position the menu slightly above the symbol in world space
            transform.position = symbol.transform.position + Vector3.up * 0.08f;

            // Face the camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                    cam.transform.rotation * Vector3.up);
            }

            if (symbolInfoLabel != null && symbol.Definition != null)
            {
                symbolInfoLabel.text = $"{symbol.Definition.code} – {symbol.Definition.fullName}";
            }

            gameObject.SetActive(true);
        }

        public void Close()
        {
            targetSymbol = null;
            gameObject.SetActive(false);
        }

        private void OnMoveClicked()
        {
            // Move mode: the SymbolMoveTool will pick up the symbol on the next raycast
            if (SymbolMoveTool.Instance != null && targetSymbol != null)
            {
                SymbolMoveTool.Instance.BeginMove(targetSymbol);
            }
            Close();
        }

        private void OnChangeTypeClicked()
        {
            if (typePickerPanel != null)
            {
                bool show = !typePickerPanel.activeSelf;
                typePickerPanel.SetActive(show);
            }
        }

        private void OnDeleteClicked()
        {
            if (SymbolLayerManager.Instance != null && targetSymbol != null)
            {
                SymbolLayerManager.Instance.RemoveSymbol(targetSymbol);
            }
            Close();
        }

        private void OnAddNoteClicked()
        {
            // Opens the keyboard / note entry flow; simplified to a log for now.
            Debug.Log($"[SymbolEditMenu] Add note to {targetSymbol?.Data?.type}");
        }

        /// <summary>Called by a type-picker button generated at runtime.</summary>
        public void ApplyTypeChange(string newCode)
        {
            if (targetSymbol != null)
            {
                targetSymbol.ChangeSymbolType(newCode);
            }
            if (typePickerPanel != null) typePickerPanel.SetActive(false);
        }
    }
}
