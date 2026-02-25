using UnityEngine;
using VRDrawing;
using VRDrawing.Mode;

public class DrawingBoardBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrawingModeManager modeManager;
    [SerializeField] private DrawingSurface drawingSurface;

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnAwake = true;

    private void Awake()
    {
        if (modeManager == null)
        {
            modeManager = FindFirstObjectByType<DrawingModeManager>();
        }

        if (drawingSurface == null)
        {
            drawingSurface = GetComponentInChildren<DrawingSurface>();
        }
        
        if (autoSetupOnAwake && drawingSurface != null)
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
