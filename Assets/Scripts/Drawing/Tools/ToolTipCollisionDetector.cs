using UnityEngine;

namespace VRDrawing.Tools
{
    public class ToolTipCollisionDetector : MonoBehaviour
    {
        private DrawingToolBase parentTool;
        private DrawingSurface currentSurface;
        private bool isInContact = false;

        private void Awake()
        {
            parentTool = GetComponentInParent<DrawingToolBase>();
            if (parentTool == null)
            {
                Debug.LogError("ToolTipCollisionDetector: No DrawingToolBase found in parent!");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (parentTool == null || !parentTool.IsHeld) return;

            DrawingSurface surface = other.GetComponent<DrawingSurface>();
            if (surface != null)
            {
                currentSurface = surface;
                isInContact = true;
                parentTool.OnToolTipEnterSurface(surface, transform.position);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (parentTool == null || !parentTool.IsHeld || currentSurface == null) return;

            if (isInContact)
            {
                parentTool.OnToolTipStaySurface(currentSurface, transform.position);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (parentTool == null) return;

            DrawingSurface surface = other.GetComponent<DrawingSurface>();
            if (surface != null && surface == currentSurface)
            {
                parentTool.OnToolTipExitSurface(surface);
                currentSurface = null;
                isInContact = false;
            }
        }
    }
}
