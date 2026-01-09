using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InteractionDebugger : MonoBehaviour
{
    [SerializeField] private XRDirectInteractor directInteractor;
    [SerializeField] private KeyCode debugKey = KeyCode.D;
    
    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            DebugInteractor();
            DebugAllInteractables();
        }
    }
    
    private void DebugInteractor()
    {
        if (directInteractor == null)
        {
            Debug.LogWarning("InteractionDebugger: No DirectInteractor assigned!");
            return;
        }
        
        Debug.Log($"=== XRDirectInteractor Debug ===");
        Debug.Log($"Enabled: {directInteractor.enabled}");
        Debug.Log($"GameObject Active: {directInteractor.gameObject.activeInHierarchy}");
        Debug.Log($"Interaction Layers (raw): {directInteractor.interactionLayers.value}");
        Debug.Log($"Allow Hover: {directInteractor.allowHover}");
        Debug.Log($"Allow Select: {directInteractor.allowSelect}");
        Debug.Log($"Has Hover: {directInteractor.hasHover}");
        Debug.Log($"Has Selection: {directInteractor.hasSelection}");
        Debug.Log($"PhysicsLayerMask: {directInteractor.physicsLayerMask.value}");
        
        SphereCollider sphere = directInteractor.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Debug.Log($"SphereCollider: enabled={sphere.enabled}, isTrigger={sphere.isTrigger}, radius={sphere.radius}");
        }
    }
    
    private void DebugAllInteractables()
    {
        Debug.Log($"=== All XRGrabInteractable Objects ===");
        
        XRGrabInteractable[] interactables = FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
        
        foreach (XRGrabInteractable interactable in interactables)
        {
            Debug.Log($"[{interactable.gameObject.name}] enabled={interactable.enabled}, " +
                     $"active={interactable.gameObject.activeInHierarchy}, " +
                     $"layer={LayerMask.LayerToName(interactable.gameObject.layer)}, " +
                     $"interactionLayers={interactable.interactionLayers.value}, " +
                     $"isSelected={interactable.isSelected}, " +
                     $"isHovered={interactable.isHovered}");
            
            Rigidbody rb = interactable.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"  -> Rigidbody: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}");
            }
            
            Collider[] colliders = interactable.GetComponents<Collider>();
            Debug.Log($"  -> Colliders: {colliders.Length} total");
            foreach (Collider col in colliders)
            {
                Debug.Log($"     - {col.GetType().Name}: enabled={col.enabled}, isTrigger={col.isTrigger}");
            }
        }
    }
}
