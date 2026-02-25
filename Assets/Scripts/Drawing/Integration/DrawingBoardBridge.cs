using UnityEngine;
using VRDrawing;

public class DrawingBoardBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrawingSurface drawingSurface;

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnPlacement = true;

    private void Awake()
    {
        if (drawingSurface == null)
        {
            drawingSurface = GetComponentInChildren<DrawingSurface>();
        }

        if (autoSetupOnPlacement && drawingSurface != null)
        {
            if (DrawingSystemManager.Instance != null)
            {
                DrawingSystemManager.Instance.RegisterSurface(drawingSurface);
            }
        }
    }

    public void EnableDrawing()
    {
        if (drawingSurface != null)
        {
            drawingSurface.enabled = true;
        }
    }

    public void DisableDrawing()
    {
        if (drawingSurface != null)
        {
            drawingSurface.enabled = false;
        }
    }

    public DrawingSurface GetDrawingSurface()
    {
        return drawingSurface;
    }
}
