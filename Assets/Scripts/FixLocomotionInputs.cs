using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FixLocomotionInputs : MonoBehaviour
{
    [ContextMenu("Diagnose Current Setup")]
    void DiagnoseSetup()
    {
        Debug.Log("=== LOCOMOTION DIAGNOSIS ===");
        
        var moveProvider = FindAnyObjectByType<DynamicMoveProvider>();
        if (moveProvider != null)
        {
            Debug.Log($"Move Provider on: {moveProvider.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("No DynamicMoveProvider found!");
        }
        
        var snapTurnProvider = FindAnyObjectByType<SnapTurnProvider>();
        if (snapTurnProvider != null)
        {
            Debug.Log($"Snap Turn Provider on: {snapTurnProvider.gameObject.name}");
        }
        
        var continuousTurnProvider = FindAnyObjectByType<ContinuousTurnProvider>();
        if (continuousTurnProvider != null)
        {
            Debug.Log($"Continuous Turn Provider on: {continuousTurnProvider.gameObject.name}");
        }
        
        Debug.Log("Check the Inspector on the Move and Turn GameObjects to see the input configuration.");
        Debug.Log("Expected: Move uses 'Left Hand Move', Turn uses 'Right Hand Turn' or 'Right Hand Snap Turn'");
    }
    
    [ContextMenu("Apply Standard VR Configuration")]
    void ApplyStandardConfiguration()
    {
        Debug.Log("=== APPLYING STANDARD VR LOCOMOTION ===");
        Debug.Log("Left Joystick = Movement, Right Joystick = Turning");
        
#if UNITY_EDITOR
        var moveProvider = FindAnyObjectByType<DynamicMoveProvider>();
        var snapTurnProvider = FindAnyObjectByType<SnapTurnProvider>();
        var continuousTurnProvider = FindAnyObjectByType<ContinuousTurnProvider>();
        
        if (moveProvider != null)
        {
            Undo.RecordObject(moveProvider, "Fix Move Provider Inputs");
            EditorUtility.SetDirty(moveProvider);
            Debug.Log("✓ Move Provider found - Please manually set inputs in Inspector");
            Debug.Log("  - Left Hand Move Input should use 'Left Hand Move' action");
            Debug.Log("  - Right Hand Move Input can be disabled or set to 'Right Hand Move'");
        }
        else
        {
            Debug.LogError("✗ No DynamicMoveProvider found in scene!");
        }
        
        if (snapTurnProvider != null)
        {
            Undo.RecordObject(snapTurnProvider, "Fix Snap Turn Provider Inputs");
            EditorUtility.SetDirty(snapTurnProvider);
            Debug.Log("✓ Snap Turn Provider found - Please manually set inputs in Inspector");
            Debug.Log("  - Left Hand Turn Input can be disabled");
            Debug.Log("  - Right Hand Turn Input should use 'Right Hand Snap Turn' action");
        }
        
        if (continuousTurnProvider != null)
        {
            Undo.RecordObject(continuousTurnProvider, "Fix Continuous Turn Provider Inputs");
            EditorUtility.SetDirty(continuousTurnProvider);
            Debug.Log("✓ Continuous Turn Provider found - Please manually set inputs in Inspector");
            Debug.Log("  - Left Hand Turn Input can be disabled");
            Debug.Log("  - Right Hand Turn Input should use 'Right Hand Turn' action");
        }
        
        Debug.Log("\n=== MANUAL CONFIGURATION REQUIRED ===");
        Debug.Log("1. Select the 'Move' GameObject under XR Origin (VR)/Locomotion");
        Debug.Log("2. In DynamicMoveProvider component, verify:");
        Debug.Log("   - Left Hand Move Input → Input Action Name: 'Left Hand Move'");
        Debug.Log("   - Right Hand Move Input → Can be None or 'Right Hand Move'");
        Debug.Log("\n3. Select the 'Turn' GameObject under XR Origin (VR)/Locomotion");
        Debug.Log("4. In turn provider components, verify:");
        Debug.Log("   - Left Hand Turn Input → Can be None");
        Debug.Log("   - Right Hand Turn Input → 'Right Hand Turn' or 'Right Hand Snap Turn'");
#else
        Debug.LogWarning("This function only works in the Unity Editor!");
#endif
    }
}
