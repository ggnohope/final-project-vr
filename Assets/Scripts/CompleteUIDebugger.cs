using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CompleteUIDebugger : MonoBehaviour
{
    [SerializeField] private bool enableDebug = false;
    
    private XRRayInteractor uiRay;
    private XRUIInputModule xrInputModule;
    private float timer = 0f;
    
    void Start()
    {
        if (!enableDebug)
        {
            enabled = false;
            return;
        }
        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("🔍 COMPLETE UI DEBUG - STARTUP");
        Debug.Log("════════════════════════════════════════════════════════════");
        
        uiRay = GameObject.Find("UI Ray Interactor")?.GetComponent<XRRayInteractor>();
        if (uiRay != null)
        {
            Debug.Log($"✅ UI Ray found: {uiRay.name}");
            Debug.Log($"   GameObject active: {uiRay.gameObject.activeInHierarchy}");
            Debug.Log($"   Component enabled: {uiRay.enabled}");
            Debug.Log($"   Enable UI Interaction: {uiRay.enableUIInteraction}");
            Debug.Log($"   Allow Select: {uiRay.allowSelect}");
            Debug.Log($"   Allow Hover: {uiRay.allowHover}");
            Debug.Log($"   Ray Origin: {(uiRay.rayOriginTransform != null ? uiRay.rayOriginTransform.name : "NULL")}");
            Debug.Log($"   Line Type: {uiRay.lineType}");
            
            var uiPressInput = uiRay.GetType().GetField("m_UIPressInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (uiPressInput != null)
            {
                var inputValue = uiPressInput.GetValue(uiRay);
                Debug.Log($"   UI Press Input: {inputValue}");
            }
            
            uiRay.uiHoverEntered.AddListener((args) => {
                Debug.Log($"🎯 UI HOVER ENTERED: {args.uiObject.name}");
            });
            
            uiRay.uiHoverExited.AddListener((args) => {
                Debug.Log($"👋 UI HOVER EXITED: {args.uiObject.name}");
            });
        }
        else
        {
            Debug.LogError("❌ UI Ray Interactor NOT FOUND!");
        }
        
        xrInputModule = FindFirstObjectByType<XRUIInputModule>();
        if (xrInputModule != null)
        {
            Debug.Log($"✅ XRUIInputModule found");
            Debug.Log($"   Enabled: {xrInputModule.enabled}");
            Debug.Log($"   Active: {xrInputModule.isActiveAndEnabled}");
        }
        else
        {
            Debug.LogError("❌ XRUIInputModule NOT FOUND!");
        }
        
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"✅ EventSystem found");
            Debug.Log($"   Current: {EventSystem.current != null}");
        }
        
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"\n📋 Found {canvases.Length} Canvas(es):");
        foreach (var canvas in canvases)
        {
            Debug.Log($"   - {canvas.name}");
            Debug.Log($"     RenderMode: {canvas.renderMode}");
            Debug.Log($"     WorldCamera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "NULL")}");
            
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            var trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            Debug.Log($"     GraphicRaycaster: {raycaster != null}");
            Debug.Log($"     TrackedDeviceGraphicRaycaster: {trackedRaycaster != null}");
        }
        
        Debug.Log("════════════════════════════════════════════════════════════");
    }
    
    void Update()
    {
        if (uiRay == null) return;
        
        timer += Time.deltaTime;
        if (timer >= 2f)
        {
            timer = 0f;
            CheckStatus();
        }
        
        if (uiRay.isSelectActive && !wasSelectActive)
        {
            Debug.Log("🔥🔥🔥 SELECT BUTTON PRESSED! 🔥🔥🔥");
            
            if (uiRay.TryGetCurrentUIRaycastResult(out RaycastResult result))
            {
                Debug.Log($"   Hovering over: {result.gameObject.name}");
                Debug.Log($"   Distance: {result.distance:F2}m");
                
                Button btn = result.gameObject.GetComponent<Button>();
                if (btn != null)
                {
                    Debug.Log($"   Button found: {btn.name}");
                    Debug.Log($"   Button interactable: {btn.interactable}");
                    Debug.Log($"   Button enabled: {btn.enabled}");
                    Debug.Log($"   Button onClick listeners: {btn.onClick.GetPersistentEventCount()}");
                }
                else
                {
                    Debug.Log($"   ⚠️ No Button component on {result.gameObject.name}");
                }
            }
            else
            {
                Debug.Log("   ⚠️ NOT hovering over any UI!");
            }
        }
        
        wasSelectActive = uiRay.isSelectActive;
    }
    
    private bool wasSelectActive = false;
    
    void CheckStatus()
    {
        if (uiRay == null) return;
        
        Debug.Log("\n──────────────────────────────────────");
        Debug.Log($"⏱️ Status Check");
        Debug.Log($"   UI Ray active: {uiRay.gameObject.activeInHierarchy}");
        Debug.Log($"   UI Ray enabled: {uiRay.enabled}");
        Debug.Log($"   Has hover: {uiRay.hasHover}");
        
        if (uiRay.TryGetCurrentUIRaycastResult(out RaycastResult result))
        {
            Debug.Log($"   ✅ Hovering: {result.gameObject.name}");
            Debug.Log($"      Layer: {LayerMask.LayerToName(result.gameObject.layer)}");
            Debug.Log($"      Distance: {result.distance:F2}m");
        }
        else
        {
            Debug.Log($"   No UI hover");
        }
        Debug.Log("──────────────────────────────────────");
    }
}
