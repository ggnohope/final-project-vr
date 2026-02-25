using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class DiagnoseLocomotion : MonoBehaviour
{
    void Start()
    {
        var moveProvider = FindAnyObjectByType<DynamicMoveProvider>();
        if (moveProvider != null)
        {
            Debug.Log("=== MOVE PROVIDER ===");
            Debug.Log($"Move Provider found on: {moveProvider.gameObject.name}");
            Debug.Log($"Left Hand Move Input: {moveProvider.leftHandMoveInput}");
            Debug.Log($"Right Hand Move Input: {moveProvider.rightHandMoveInput}");
        }
        
        var snapTurnProvider = FindAnyObjectByType<SnapTurnProvider>();
        if (snapTurnProvider != null)
        {
            Debug.Log("=== SNAP TURN PROVIDER ===");
            Debug.Log($"Snap Turn Provider found on: {snapTurnProvider.gameObject.name}");
            Debug.Log($"Left Hand Turn Input: {snapTurnProvider.leftHandTurnInput}");
            Debug.Log($"Right Hand Turn Input: {snapTurnProvider.rightHandTurnInput}");
        }
        
        var continuousTurnProvider = FindAnyObjectByType<ContinuousTurnProvider>();
        if (continuousTurnProvider != null)
        {
            Debug.Log("=== CONTINUOUS TURN PROVIDER ===");
            Debug.Log($"Continuous Turn Provider found on: {continuousTurnProvider.gameObject.name}");
            Debug.Log($"Left Hand Turn Input: {continuousTurnProvider.leftHandTurnInput}");
            Debug.Log($"Right Hand Turn Input: {continuousTurnProvider.rightHandTurnInput}");
        }
        
        Destroy(gameObject);
    }
}
