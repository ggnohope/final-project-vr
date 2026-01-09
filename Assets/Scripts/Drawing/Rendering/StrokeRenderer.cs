using UnityEngine;
using VRDrawing.Data;

namespace VRDrawing.Rendering
{
    public abstract class StrokeRenderer : MonoBehaviour
    {
        public abstract void Initialize(DrawingSurface surface);
        public abstract void RebuildAllStrokes(DrawingData data);
        public abstract void UpdateStroke(Stroke stroke);
        public abstract void ClearAllStrokes();
    }
}
