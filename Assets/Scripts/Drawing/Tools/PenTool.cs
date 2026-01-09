using UnityEngine;

namespace VRDrawing.Tools
{
    public class PenTool : DrawingToolBase
    {
        [Header("Pen Settings")]
        [SerializeField] private Color penColor = Color.blue;
        [SerializeField] private float penWidth = 0.005f;
        [SerializeField] private Renderer penRenderer;
        [SerializeField] private string colorPropertyName = "_BaseColor";

        public override ToolType Type => ToolType.Pen;
        public override Color Color => penColor;
        public override float Width => penWidth;
        public override string ToolId => "pen";

        protected override void Awake()
        {
            base.Awake();

            if (penRenderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                penRenderer.GetPropertyBlock(block);
                block.SetColor(colorPropertyName, penColor);
                penRenderer.SetPropertyBlock(block);
            }
        }

        public void SetColor(Color newColor)
        {
            penColor = newColor;
            
            if (penRenderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                penRenderer.GetPropertyBlock(block);
                block.SetColor(colorPropertyName, penColor);
                penRenderer.SetPropertyBlock(block);
            }
        }

        public void SetWidth(float newWidth)
        {
            penWidth = Mathf.Max(0.001f, newWidth);
        }
    }
}
