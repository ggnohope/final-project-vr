using UnityEngine;
using VRDrawing.Tools;

public class PenControllerBridge : MonoBehaviour
{
    [Header("Legacy Support")]
    [SerializeField] private PenController legacyPen;
    [SerializeField] private PenTool newPenTool;

    [Header("Auto Conversion")]
    [SerializeField] private bool convertOnStart = true;

    private void Start()
    {
        if (convertOnStart)
        {
            ConvertLegacyPen();
        }
    }

    public void ConvertLegacyPen()
    {
        if (legacyPen == null)
        {
            legacyPen = GetComponent<PenController>();
        }

        if (legacyPen == null)
        {
            Debug.LogWarning("No legacy PenController found to convert.");
            return;
        }

        if (newPenTool == null)
        {
            newPenTool = gameObject.AddComponent<PenTool>();
        }

        var grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
        }

        if (VRDrawing.DrawingSystemManager.Instance != null)
        {
            VRDrawing.DrawingSystemManager.Instance.RegisterTool(newPenTool);
        }

        legacyPen.enabled = false;

        Debug.Log($"Converted {gameObject.name} from legacy PenController to new PenTool system.");
    }
}
