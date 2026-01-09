using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;

public class DebugXRUIPress : MonoBehaviour
{
    private XRRayInteractor uiRay;
    
    void Start()
    {
        uiRay = GameObject.Find("UI Ray Interactor")?.GetComponent<XRRayInteractor>();
        if (uiRay != null)
        {
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("🔍 XR UI PRESS DEBUG STARTED");
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log($"UI Ray found: {uiRay.name}");
            Debug.Log($"  Enable UI Interaction: {uiRay.enableUIInteraction}");
            Debug.Log($"  Allow Select: {uiRay.allowSelect}");
            
            uiRay.selectEntered.AddListener((args) =>
            {
                Debug.Log($"🎯 XRRayInteractor selectEntered! Target: {args.interactableObject}");
            });
            
            uiRay.uiHoverEntered.AddListener((args) =>
            {
                Debug.Log($"👆 UI Hover ENTERED: {args.uiObject.name}");
            });
            
            uiRay.uiHoverExited.AddListener((args) =>
            {
                Debug.Log($"👋 UI Hover EXITED: {args.uiObject.name}");
            });
        }
        else
        {
            Debug.LogError("❌ UI Ray Interactor not found!");
        }
        
        XRUIInputModule xrInputModule = FindFirstObjectByType<XRUIInputModule>();
        if (xrInputModule != null)
        {
            Debug.Log($"✅ XRUIInputModule found and active: {xrInputModule.enabled}");
        }
        else
        {
            Debug.LogError("❌ XRUIInputModule not found!");
        }
    }
    
    void Update()
    {
        if (uiRay == null) return;
        
        if (uiRay.TryGetCurrentUIRaycastResult(out UnityEngine.EventSystems.RaycastResult result))
        {
            if (Input.GetKeyDown(KeyCode.Space) || (uiRay.isSelectActive && !wasSelectActive))
            {
                Debug.Log($"🔥 SELECT TRIGGERED while hovering: {result.gameObject.name}");
                Debug.Log($"   isSelectActive: {uiRay.isSelectActive}");
                Debug.Log($"   Layer: {LayerMask.LayerToName(result.gameObject.layer)}");
            }
        }
        
        wasSelectActive = uiRay.isSelectActive;
    }
    
    private bool wasSelectActive = false;
}
