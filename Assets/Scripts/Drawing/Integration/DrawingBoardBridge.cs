using UnityEngine;
using VRDrawing;

public class DrawingBoardBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardPlacementController placementController;
    [SerializeField] private DrawingSurface drawingSurface;

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnPlacement = true;

    private void Awake()
    {
        if (placementController == null)
        {
            placementController = GetComponent<BoardPlacementController>();
        }

        if (drawingSurface == null)
        {
            drawingSurface = GetComponentInChildren<DrawingSurface>();
        }
    }

    private void OnEnable()
    {
        if (placementController != null)
        {
            placementController.OnBoardPlaced += OnBoardPlaced;
        }
    }

    private void OnDisable()
    {
        if (placementController != null)
        {
            placementController.OnBoardPlaced -= OnBoardPlaced;
        }
    }

    private void OnBoardPlaced(GameObject board)
    {
        if (!autoSetupOnPlacement) return;

        if (drawingSurface != null)
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
