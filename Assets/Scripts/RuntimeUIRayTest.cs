using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuntimeUIRayTest : MonoBehaviour
{
    private XRRayInteractor uiRay;
    private float timer = 0f;
    private float interval = 1f;

    void Start()
    {
        uiRay = GameObject.Find("UI Ray Interactor")?.GetComponent<XRRayInteractor>();
        
        if (uiRay == null)
        {
            Debug.LogError("❌ UI Ray Interactor NOT FOUND at runtime!");
            return;
        }
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🎯 RUNTIME UI RAY TEST STARTED");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"UI Ray found: {uiRay.name}");
        Debug.Log($"  GameObject active: {uiRay.gameObject.activeInHierarchy}");
        Debug.Log($"  Component enabled: {uiRay.enabled}");
        Debug.Log($"  Enable UI Interaction: {uiRay.enableUIInteraction}");
        Debug.Log($"  Allow Hover: {uiRay.allowHover}");
        Debug.Log($"  Allow Select: {uiRay.allowSelect}");
        Debug.Log($"  Ray Origin: {(uiRay.rayOriginTransform != null ? uiRay.rayOriginTransform.name : "NULL")}");
        Debug.Log("═══════════════════════════════════════════════════════");
    }

    void Update()
    {
        if (uiRay == null) return;
        
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            CheckRayStatus();
        }
    }

    void CheckRayStatus()
    {
        Debug.Log("\n───────────────────────────────────────────────");
        Debug.Log($"⏱️ Runtime Check - UI Ray Status");
        Debug.Log($"  Active in Hierarchy: {uiRay.gameObject.activeInHierarchy}");
        Debug.Log($"  Enabled: {uiRay.enabled}");
        Debug.Log($"  Has Hover: {uiRay.hasHover}");
        Debug.Log($"  Has Selection: {uiRay.hasSelection}");
        
        if (uiRay.TryGetCurrentUIRaycastResult(out RaycastResult result))
        {
            Debug.Log($"  ✅ HITTING UI: {result.gameObject.name}");
            Debug.Log($"     Distance: {result.distance:F2}m");
            Debug.Log($"     Layer: {LayerMask.LayerToName(result.gameObject.layer)}");
            
            Button btn = result.gameObject.GetComponent<Button>();
            if (btn != null)
            {
                Debug.Log($"     Button: {btn.name}, Interactable: {btn.interactable}");
            }
        }
        else
        {
            Debug.Log($"  ❌ NOT hitting any UI");
            
            RaycastHit hit;
            if (Physics.Raycast(uiRay.rayOriginTransform.position, uiRay.rayOriginTransform.forward, out hit, 30f))
            {
                Debug.Log($"  But 3D raycast hits: {hit.collider.name} at {hit.distance:F2}m");
                Debug.Log($"     Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            else
            {
                Debug.Log($"  3D raycast also hits nothing");
            }
        }
        Debug.Log("───────────────────────────────────────────────\n");
    }
}
