using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRDrawing.UI
{
    public class DrawingSurfaceControls : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private DrawingSurface targetSurface;

        [Header("Buttons")]
        [SerializeField] private Button clearButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;

        [Header("Info Display")]
        [SerializeField] private TextMeshProUGUI strokeCountText;
        [SerializeField] private TextMeshProUGUI historyInfoText;

        [Header("Save/Load")]
        [SerializeField] private string saveDirectory = "Drawings";
        [SerializeField] private string defaultFilename = "drawing.json";

        private void OnEnable()
        {
            if (targetSurface != null)
            {
                targetSurface.OnStrokeAdded += OnDrawingChanged;
                targetSurface.OnStrokeRemoved += OnDrawingChanged;
                targetSurface.OnCleared += OnDrawingChanged;
                targetSurface.OnHistoryChanged += UpdateHistoryDisplay;
            }

            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearClicked);
            
            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndoClicked);
            
            if (redoButton != null)
                redoButton.onClick.AddListener(OnRedoClicked);
            
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadClicked);

            UpdateDisplay();
        }

        private void OnDisable()
        {
            if (targetSurface != null)
            {
                targetSurface.OnStrokeAdded -= OnDrawingChanged;
                targetSurface.OnStrokeRemoved -= OnDrawingChanged;
                targetSurface.OnCleared -= OnDrawingChanged;
                targetSurface.OnHistoryChanged -= UpdateHistoryDisplay;
            }

            if (clearButton != null)
                clearButton.onClick.RemoveListener(OnClearClicked);
            
            if (undoButton != null)
                undoButton.onClick.RemoveListener(OnUndoClicked);
            
            if (redoButton != null)
                redoButton.onClick.RemoveListener(OnRedoClicked);
            
            if (saveButton != null)
                saveButton.onClick.RemoveListener(OnSaveClicked);
            
            if (loadButton != null)
                loadButton.onClick.RemoveListener(OnLoadClicked);
        }

        private void Update()
        {
            UpdateButtonStates();
        }

        private void OnClearClicked()
        {
            if (targetSurface != null)
            {
                targetSurface.Clear();
            }
        }

        private void OnUndoClicked()
        {
            if (targetSurface != null)
            {
                targetSurface.Undo();
            }
        }

        private void OnRedoClicked()
        {
            if (targetSurface != null)
            {
                targetSurface.Redo();
            }
        }

        private void OnSaveClicked()
        {
            if (targetSurface == null) return;

            string directory = System.IO.Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            string filename = $"drawing_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string fullPath = System.IO.Path.Combine(directory, filename);

            if (targetSurface.SaveToFile(fullPath))
            {
                Debug.Log($"Drawing saved to: {fullPath}");
            }
        }

        private void OnLoadClicked()
        {
            if (targetSurface == null) return;

            string directory = System.IO.Path.Combine(Application.persistentDataPath, saveDirectory);
            string fullPath = System.IO.Path.Combine(directory, defaultFilename);

            if (targetSurface.LoadFromFile(fullPath))
            {
                Debug.Log($"Drawing loaded from: {fullPath}");
            }
        }

        private void OnDrawingChanged(Data.Stroke stroke = null)
        {
            UpdateDisplay();
        }

        private void OnDrawingChanged()
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (strokeCountText != null && targetSurface != null)
            {
                strokeCountText.text = $"Strokes: {targetSurface.StrokeCount}";
            }

            UpdateHistoryDisplay();
        }

        private void UpdateHistoryDisplay()
        {
            if (historyInfoText != null && targetSurface != null)
            {
                bool canUndo = targetSurface.CanUndo();
                bool canRedo = targetSurface.CanRedo();
                historyInfoText.text = $"Undo: {(canUndo ? "✓" : "✗")} | Redo: {(canRedo ? "✓" : "✗")}";
            }
        }

        private void UpdateButtonStates()
        {
            if (targetSurface == null) return;

            if (undoButton != null)
            {
                undoButton.interactable = targetSurface.CanUndo();
            }

            if (redoButton != null)
            {
                redoButton.interactable = targetSurface.CanRedo();
            }

            if (clearButton != null)
            {
                clearButton.interactable = targetSurface.StrokeCount > 0;
            }
        }

        public void SetTargetSurface(DrawingSurface surface)
        {
            if (targetSurface != null)
            {
                targetSurface.OnStrokeAdded -= OnDrawingChanged;
                targetSurface.OnStrokeRemoved -= OnDrawingChanged;
                targetSurface.OnCleared -= OnDrawingChanged;
                targetSurface.OnHistoryChanged -= UpdateHistoryDisplay;
            }

            targetSurface = surface;

            if (targetSurface != null)
            {
                targetSurface.OnStrokeAdded += OnDrawingChanged;
                targetSurface.OnStrokeRemoved += OnDrawingChanged;
                targetSurface.OnCleared += OnDrawingChanged;
                targetSurface.OnHistoryChanged += UpdateHistoryDisplay;
            }

            UpdateDisplay();
        }
    }
}
