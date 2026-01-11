using UnityEngine;
using UnityEngine.UI;
using VRDrawing.Mode;
using VRDrawing.Tools;

namespace VRDrawing.UI
{
    public class DrawingToolPanel : MonoBehaviour
    {
        [Header("Tool Buttons")]
        [SerializeField] private Button penButton;
        [SerializeField] private Button eraserButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button clearButton;

        [Header("Color Buttons")]
        [SerializeField] private Button[] colorButtons;
        [SerializeField] private Color[] colorPalette = new Color[]
        {
            Color.black,
            Color.white,
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta
        };

        [Header("Thickness Control")]
        [SerializeField] private Slider thicknessSlider;
        [SerializeField] private float minThickness = 0.002f;
        [SerializeField] private float maxThickness = 0.02f;

        [Header("Current Tool Display")]
        [SerializeField] private Image currentToolIcon;
        [SerializeField] private Image currentColorDisplay;

        private ToolType currentToolType = ToolType.Pen;
        private Color currentColor = Color.black;
        private float currentThickness = 0.005f;

        private void Awake()
        {
            SetupButtons();
            SetupColorButtons();
            SetupThicknessSlider();
            
            UpdateCurrentToolDisplay();
        }

        private void SetupButtons()
        {
            if (penButton != null)
            {
                penButton.onClick.AddListener(() => SetTool(ToolType.Pen));
            }

            if (eraserButton != null)
            {
                eraserButton.onClick.AddListener(() => SetTool(ToolType.Eraser));
            }

            if (undoButton != null)
            {
                undoButton.onClick.AddListener(Undo);
            }

            if (clearButton != null)
            {
                clearButton.onClick.AddListener(Clear);
            }
        }

        private void SetupColorButtons()
        {
            if (colorButtons == null || colorButtons.Length == 0) return;

            for (int i = 0; i < colorButtons.Length && i < colorPalette.Length; i++)
            {
                int index = i;
                Color color = colorPalette[i];

                if (colorButtons[i] != null)
                {
                    Image buttonImage = colorButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = color;
                    }

                    colorButtons[i].onClick.AddListener(() => SetColor(color));
                }
            }
        }

        private void SetupThicknessSlider()
        {
            if (thicknessSlider == null) return;

            thicknessSlider.minValue = minThickness;
            thicknessSlider.maxValue = maxThickness;
            thicknessSlider.value = currentThickness;
            thicknessSlider.onValueChanged.AddListener(SetThickness);
        }

        private void SetTool(ToolType toolType)
        {
            currentToolType = toolType;
            UpdateCurrentToolDisplay();
            NotifyToolChange();
            AutoHidePanel();
        }

        private void SetColor(Color color)
        {
            currentColor = color;
            UpdateCurrentToolDisplay();
            NotifyColorChange();
            AutoHidePanel();
        }

        private void SetThickness(float thickness)
        {
            currentThickness = thickness;
            NotifyThicknessChange();
        }

        private void Undo()
        {
            DrawingSurface surface = GetActiveDrawingSurface();
            if (surface != null)
            {
                surface.Undo();
            }
        }

        private void Clear()
        {
            DrawingSurface surface = GetActiveDrawingSurface();
            if (surface != null)
            {
                surface.ClearAll();
            }
        }

        private void UpdateCurrentToolDisplay()
        {
            if (currentColorDisplay != null)
            {
                currentColorDisplay.color = currentColor;
            }
        }

        private void NotifyToolChange()
        {
            DrawingToolBase[] tools = FindObjectsByType<DrawingToolBase>(FindObjectsSortMode.None);
            foreach (var tool in tools)
            {
                tool.SetEnabled(tool.ToolType == currentToolType);
            }
        }

        private void NotifyColorChange()
        {
            DrawingToolBase[] tools = FindObjectsByType<DrawingToolBase>(FindObjectsSortMode.None);
            foreach (var tool in tools)
            {
                if (tool is PenTool penTool)
                {
                    penTool.SetColor(currentColor);
                }
            }
        }

        private void NotifyThicknessChange()
        {
            DrawingToolBase[] tools = FindObjectsByType<DrawingToolBase>(FindObjectsSortMode.None);
            foreach (var tool in tools)
            {
                tool.SetThickness(currentThickness);
            }
        }

        private DrawingSurface GetActiveDrawingSurface()
        {
            if (DrawingModeManager.Instance == null || DrawingModeManager.Instance.ActiveDrawingBoard == null)
            {
                return null;
            }

            return DrawingModeManager.Instance.ActiveDrawingBoard.GetComponentInChildren<DrawingSurface>();
        }

        private void AutoHidePanel()
        {
            if (DrawingModeManager.Instance != null)
            {
                DrawingModeManager.Instance.OnToolSelected();
            }
        }
    }
}
