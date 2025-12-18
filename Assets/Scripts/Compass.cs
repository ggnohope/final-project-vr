using UnityEngine;

/// <summary>
/// Controls the compass needle to always point to world north (positive Z axis).
/// The compass body can be rotated by the player, but the needle will always point north.
/// </summary>
public class Compass : MonoBehaviour
{
    [Header("Compass Settings")]
    [Tooltip("The transform of the compass needle. Should be a child object that rotates to point north.")]
    [SerializeField] private Transform needleTransform;
    
    [Tooltip("The direction the needle should point when facing north. Default is forward (0, 0, 1).")]
    [SerializeField] private Vector3 northDirection = Vector3.forward;
    
    [Tooltip("Smooth rotation speed for the needle. Higher values = faster rotation.")]
    [SerializeField] private float rotationSpeed = 10f;
    
    [Tooltip("Enable smooth rotation. If false, needle snaps instantly to north.")]
    [SerializeField] private bool smoothRotation = true;

    private void Awake()
    {
        // If needle transform is not assigned, try to find it
        if (needleTransform == null)
        {
            // Look for a child object named "Needle"
            Transform foundNeedle = transform.Find("Needle");
            if (foundNeedle != null)
            {
                needleTransform = foundNeedle;
            }
            else
            {
                Debug.LogError($"Compass: Needle Transform not assigned and no child named 'Needle' found on {gameObject.name}. Please assign the needle transform in the inspector.");
            }
        }
    }

    private void Update()
    {
        if (needleTransform == null)
            return;

        // Calculate the direction to world north (positive Z axis) in world space
        Vector3 worldNorth = Vector3.forward;
        
        // Convert world north direction to local space relative to the compass body
        Vector3 localNorth = transform.InverseTransformDirection(worldNorth);
        
        // Project onto the XZ plane (remove Y component) to keep rotation only on Y axis
        localNorth.y = 0;
        localNorth.Normalize();
        
        // Calculate the target rotation for the needle
        // The needle should point in the direction of localNorth (toward world north)
        Quaternion targetRotation = Quaternion.LookRotation(localNorth, Vector3.up);
        
        // Apply rotation to the needle
        if (smoothRotation)
        {
            needleTransform.localRotation = Quaternion.Slerp(
                needleTransform.localRotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
        else
        {
            needleTransform.localRotation = targetRotation;
        }
    }

    /// <summary>
    /// Call this method to manually set the needle transform if it's not assigned in inspector.
    /// </summary>
    public void SetNeedleTransform(Transform needle)
    {
        needleTransform = needle;
    }

    /// <summary>
    /// Get the current heading angle in degrees (0 = North, 90 = East, 180 = South, 270 = West).
    /// </summary>
    public float GetHeading()
    {
        // Get the forward direction of the compass body in world space
        Vector3 forward = transform.forward;
        
        // Project onto XZ plane
        forward.y = 0;
        forward.Normalize();
        
        // Calculate angle from north (forward/Z axis)
        float angle = Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
        
        // Convert to 0-360 range
        if (angle < 0)
            angle += 360f;
        
        return angle;
    }
}

