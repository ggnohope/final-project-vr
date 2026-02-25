using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PenController : MonoBehaviour
{
    [Header("Pen Settings")]
    [SerializeField] private Color penColor = Color.blue;
    [SerializeField] private Transform penTip;
    [SerializeField] private float tipRadius = 0.01f;
    
    [Header("Visual")]
    [SerializeField] private Renderer penRenderer;
    [SerializeField] private string colorPropertyName = "_BaseColor";
    
    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private Rigidbody rb;
    
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        
        if (penTip == null)
        {
            penTip = transform.Find("PenTip");
            if (penTip == null)
            {
                GameObject tipObj = new GameObject("PenTip");
                tipObj.transform.SetParent(transform);
                tipObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                penTip = tipObj.transform;
            }
        }
        
        if (penRenderer != null)
        {
            penRenderer.material.SetColor(colorPropertyName, penColor);
        }
        
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
        }
    }
    
    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }
    
    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    private void OnGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        isHeld = true;
    }
    
    private void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        isHeld = false;
    }
    
    public bool IsHeld()
    {
        return isHeld;
    }
    
    public Vector3 GetTipPosition()
    {
        return penTip != null ? penTip.position : transform.position;
    }
    
    public Vector3 GetTipDirection()
    {
        return penTip != null ? penTip.forward : transform.forward;
    }
    
    public Color GetPenColor()
    {
        return penColor;
    }
    
    public float GetTipRadius()
    {
        return tipRadius;
    }
}
