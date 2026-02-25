using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class RuntimeLocomotionTest : MonoBehaviour
{
    void Start()
    {
        Invoke(nameof(CheckLocomotion), 2f);
    }

    void CheckLocomotion()
    {
        Debug.Log("=== RUNTIME LOCOMOTION CHECK ===");
        
        var moveProvider = FindAnyObjectByType<DynamicMoveProvider>();
        if (moveProvider != null)
        {
            Debug.Log($"[Move] GameObject: {moveProvider.gameObject.name}");
            Debug.Log($"[Move] GameObject Active: {moveProvider.gameObject.activeInHierarchy}");
            Debug.Log($"[Move] Component Enabled: {moveProvider.enabled}");
            Debug.Log($"[Move] Left Hand Input: {moveProvider.leftHandMoveInput}");
            Debug.Log($"[Move] Right Hand Input: {moveProvider.rightHandMoveInput}");
        }
        
        var snapTurn = FindAnyObjectByType<SnapTurnProvider>();
        if (snapTurn != null)
        {
            Debug.Log($"[SnapTurn] GameObject: {snapTurn.gameObject.name}");
            Debug.Log($"[SnapTurn] GameObject Active: {snapTurn.gameObject.activeInHierarchy}");
            Debug.Log($"[SnapTurn] Component Enabled: {snapTurn.enabled}");
            Debug.Log($"[SnapTurn] Left Hand Input: {snapTurn.leftHandTurnInput}");
            Debug.Log($"[SnapTurn] Right Hand Input: {snapTurn.rightHandTurnInput}");
        }
        
        var continuousTurn = FindAnyObjectByType<ContinuousTurnProvider>();
        if (continuousTurn != null)
        {
            Debug.Log($"[ContinuousTurn] GameObject: {continuousTurn.gameObject.name}");
            Debug.Log($"[ContinuousTurn] GameObject Active: {continuousTurn.gameObject.activeInHierarchy}");
            Debug.Log($"[ContinuousTurn] Component Enabled: {continuousTurn.enabled}");
            Debug.Log($"[ContinuousTurn] Left Hand Input: {continuousTurn.leftHandTurnInput}");
            Debug.Log($"[ContinuousTurn] Right Hand Input: {continuousTurn.rightHandTurnInput}");
        }
        
        Debug.Log("=== END CHECK ===");
    }
}
