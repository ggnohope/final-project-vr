using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System.Collections.Generic;

public class UIRayDeepDiagnostic : MonoBehaviour
{
    private float checkInterval = 2f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            RunDiagnostic();
        }
    }

    void RunDiagnostic()
    {
        Debug.Log("═════════════════════════════════════════════════════");
        Debug.Log("UI RAY DEEP DIAGNOSTIC - RUNTIME STATE");
        Debug.Log("═════════════════════════════════════════════════════");

        XRRayInteractor uiRay = GameObject.Find("UI Ray Interactor")?.GetComponent<XRRayInteractor>();
        XRRayInteractor teleportRay = GameObject.Find("Teleport Interactor")?.GetComponent<XRRayInteractor>();
        
        if (uiRay != null)
        {
            Debug.Log($"[UI RAY] Found: {uiRay.name}");
            Debug.Log($"  GameObject Active: {uiRay.gameObject.activeInHierarchy}");
            Debug.Log($"  Component Enabled: {uiRay.enabled}");
            Debug.Log($"  Enable UI Interaction: {uiRay.enableUIInteraction}");
            Debug.Log($"  Allow Hover: {uiRay.allowHover}");
            Debug.Log($"  Allow Select: {uiRay.allowSelect}");
            Debug.Log($"  Raycast Mask: {string.Join(", ", LayerMaskToNames(uiRay.raycastMask))}");
            Debug.Log($"  Ray Origin Transform: {(uiRay.rayOriginTransform != null ? uiRay.rayOriginTransform.name : "NULL")}");
            Debug.Log($"  Has Hover: {uiRay.hasHover}");
            Debug.Log($"  Has Selection: {uiRay.hasSelection}");

            if (uiRay.TryGetCurrentUIRaycastResult(out var uiResult))
            {
                Debug.Log($"  ✓✓✓ HITTING UI: {uiResult.gameObject.name} at {uiResult.distance:F2}m");
            }
            else
            {
                Debug.Log($"  ✗ NOT HITTING ANY UI");
            }
        }
        else
        {
            Debug.LogError("[UI RAY] NOT FOUND!");
        }

        if (teleportRay != null)
        {
            Debug.Log($"\n[TELEPORT RAY] Found: {teleportRay.name}");
            Debug.Log($"  GameObject Active: {teleportRay.gameObject.activeInHierarchy}");
            Debug.Log($"  Component Enabled: {teleportRay.enabled}");
            Debug.Log($"  Ray Origin Transform: {(teleportRay.rayOriginTransform != null ? teleportRay.rayOriginTransform.name : "NULL")}");
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"\n[CANVAS] Found: {canvas.name}");
            Debug.Log($"  Active: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"  Render Mode: {canvas.renderMode}");
            Debug.Log($"  World Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "NULL")}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(canvas.gameObject.layer)}");

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"  GraphicRaycaster: Present, Enabled={raycaster.enabled}");
                Debug.Log($"    Blocking Objects: {raycaster.blockingObjects}");
            }
        }

        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"\n[EVENT SYSTEM]");
            Debug.Log($"  Active: {eventSystem.gameObject.activeInHierarchy}");
            Debug.Log($"  Enabled: {eventSystem.enabled}");
            
            XRUIInputModule xrModule = eventSystem.GetComponent<XRUIInputModule>();
            if (xrModule != null)
            {
                Debug.Log($"  XRUIInputModule: Present, Enabled={xrModule.enabled}");
            }
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        Debug.Log($"\n[BUTTONS] Found {buttons.Length} buttons in scene");
        foreach (var btn in buttons)
        {
            if (btn.gameObject.activeInHierarchy)
            {
                Debug.Log($"  ✓ {btn.name}: Active, Interactable={btn.interactable}, Layer={LayerMask.LayerToName(btn.gameObject.layer)}");
            }
        }

        Debug.Log("═════════════════════════════════════════════════════");
    }

    List<string> LayerMaskToNames(LayerMask mask)
    {
        List<string> names = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    names.Add(layerName);
                }
            }
        }
        return names;
    }
}
