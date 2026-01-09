using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class UIRayVisibilityController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject canvasToMonitor;
    [SerializeField] private XRRayInteractor uiRayInteractor;
    [SerializeField] private LineRenderer rayLineRenderer;
    
    [Header("Auto Find")]
    [SerializeField] private bool autoFindUIRay = true;
    
    private void Awake()
    {
        if (autoFindUIRay && uiRayInteractor == null)
        {
            uiRayInteractor = GameObject.Find("UI Ray Interactor")?.GetComponent<XRRayInteractor>();
        }
        
        if (uiRayInteractor != null && rayLineRenderer == null)
        {
            rayLineRenderer = uiRayInteractor.GetComponent<LineRenderer>();
        }
        
        if (canvasToMonitor == null)
        {
            Debug.LogWarning("UIRayVisibilityController: No canvas assigned to monitor");
        }
    }
    
    private void OnEnable()
    {
        if (canvasToMonitor != null)
        {
            UpdateRayVisibility(canvasToMonitor.activeSelf);
        }
    }
    
    private void Update()
    {
        if (canvasToMonitor != null)
        {
            bool isCanvasActive = canvasToMonitor.activeSelf;
            UpdateRayVisibility(isCanvasActive);
        }
    }
    
    private void UpdateRayVisibility(bool visible)
    {
        if (rayLineRenderer != null)
        {
            rayLineRenderer.enabled = visible;
        }
    }
}
