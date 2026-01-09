using UnityEngine;
using UnityEngine.InputSystem;

public class DebugInputActions : MonoBehaviour
{
    [Header("Actions to Debug")]
    [SerializeField] private InputActionProperty[] actionsToDebug;
    
    [Header("Settings")]
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private float updateInterval = 1f;
    
    private float lastLogTime;
    
    private void Update()
    {
        if (!logToConsole) return;
        
        if (Time.time - lastLogTime > updateInterval)
        {
            lastLogTime = Time.time;
            DebugActions();
        }
    }
    
    private void DebugActions()
    {
        foreach (var actionProp in actionsToDebug)
        {
            if (actionProp.action == null)
            {
                Debug.LogWarning("InputAction is null");
                continue;
            }
            
            string actionName = actionProp.action.name;
            bool isEnabled = actionProp.action.enabled;
            
            if (!isEnabled)
            {
                Debug.Log($"[{actionName}] DISABLED");
                continue;
            }
            
            try
            {
                Vector2 vec2Value = actionProp.action.ReadValue<Vector2>();
                Debug.Log($"[{actionName}] Vector2: {vec2Value}");
            }
            catch
            {
                try
                {
                    float floatValue = actionProp.action.ReadValue<float>();
                    Debug.Log($"[{actionName}] Float: {floatValue}");
                }
                catch
                {
                    Debug.Log($"[{actionName}] Unknown type or not bound");
                }
            }
        }
    }
}
