using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Debug script to check if Compass is properly set up for VR interaction
/// </summary>
public class CompassDebug : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Collider col;

    private void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        Debug.Log("=== COMPASS DEBUG INFO ===");
        Debug.Log($"GameObject: {gameObject.name}");
        
        // Check XR Grab Interactable
        if (grabInteractable == null)
        {
            Debug.LogError("❌ XR Grab Interactable component NOT FOUND!");
        }
        else
        {
            Debug.Log("✅ XR Grab Interactable found");
            Debug.Log($"   - Enabled: {grabInteractable.enabled}");
            Debug.Log($"   - Interaction Manager: {(grabInteractable.interactionManager != null ? grabInteractable.interactionManager.name : "None (will auto-find)")}");
            Debug.Log($"   - Colliders Count: {grabInteractable.colliders.Count}");
            if (grabInteractable.colliders.Count > 0)
            {
                foreach (var collider in grabInteractable.colliders)
                {
                    Debug.Log($"     - {collider.name} ({collider.GetType().Name})");
                }
            }
            else
            {
                Debug.Log("     - Colliders list is empty (will auto-detect)");
            }
            Debug.Log($"   - Interaction Layers: {grabInteractable.interactionLayers.value}");
            Debug.Log($"   - Select Mode: {grabInteractable.selectMode}");
        }

        // Check Rigidbody
        if (rb == null)
        {
            Debug.LogError("❌ Rigidbody component NOT FOUND!");
        }
        else
        {
            Debug.Log("✅ Rigidbody found");
            Debug.Log($"   - Is Kinematic: {rb.isKinematic}");
            Debug.Log($"   - Use Gravity: {rb.useGravity}");
            Debug.Log($"   - Mass: {rb.mass}");
        }

        // Check Collider
        if (col == null)
        {
            Debug.LogError("❌ Collider component NOT FOUND!");
        }
        else
        {
            Debug.Log("✅ Collider found");
            Debug.Log($"   - Type: {col.GetType().Name}");
            Debug.Log($"   - Enabled: {col.enabled}");
            Debug.Log($"   - Is Trigger: {col.isTrigger}");
            if (col is BoxCollider boxCol)
            {
                Debug.Log($"   - Size: {boxCol.size}");
                Debug.Log($"   - Center: {boxCol.center}");
            }
        }

        // Check for XR Interaction Manager in scene
        XRInteractionManager manager = FindObjectOfType<XRInteractionManager>();
        if (manager == null)
        {
            Debug.LogError("❌ XR Interaction Manager NOT FOUND in scene!");
            Debug.LogError("   Please add XR Interaction Manager to the scene!");
        }
        else
        {
            Debug.Log($"✅ XR Interaction Manager found: {manager.name}");
        }

        // Check for XR Direct Interactors
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor[] interactors = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
        if (interactors.Length == 0)
        {
            Debug.LogWarning("⚠️ No XR Direct Interactor found in scene!");
            Debug.LogWarning("   Make sure XR Origin (VR) is in the scene with hand controllers!");
        }
        else
        {
            Debug.Log($"✅ Found {interactors.Length} XR Direct Interactor(s):");
            foreach (var interactor in interactors)
            {
                Debug.Log($"   - {interactor.name} (Layer: {interactor.interactionLayers.value})");
            }
        }

        Debug.Log("=== END DEBUG INFO ===");
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"🎯 HOVER ENTERED: {args.interactorObject.transform.name} is hovering over Compass!");
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"🎯 HOVER EXITED: {args.interactorObject.transform.name} stopped hovering");
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"✋ GRABBED: {args.interactorObject.transform.name} grabbed the Compass!");
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"✋ RELEASED: {args.interactorObject.transform.name} released the Compass");
    }
}


