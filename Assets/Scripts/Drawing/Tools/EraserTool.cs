using UnityEngine;

namespace VRDrawing.Tools
{
    public class EraserTool : DrawingToolBase
    {
        [Header("Eraser Settings")]
        [SerializeField] private float eraserWidth = 0.02f;

        public override ToolType Type => ToolType.Eraser;
        public override Color Color => Color.white;
        public override float Width => eraserWidth;
        public override string ToolId => "eraser";

        public void SetWidth(float newWidth)
        {
            eraserWidth = Mathf.Max(0.005f, newWidth);
        }
    }
}
