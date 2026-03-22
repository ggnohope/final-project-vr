using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// VR Compass Controller that points toward world north when held.
/// Works with XR Grab Interactable to detect when compass is being held.
/// The needle rotates around the Y axis in local space to always point to world north.
/// Red/North tip of the needle points toward world north (positive Z).
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class CompassController : MonoBehaviour
{
    [Header("Compass Needle Settings")]
    [Tooltip("The pivot transform of the compass needle. This is the object that rotates.")]
    [SerializeField] private Transform needleTransform;

    [Tooltip("Smooth rotation speed for the needle. Higher values = faster rotation.")]
    [SerializeField] private float rotationSpeed = 8f;

    [Tooltip("Enable smooth rotation. If false, needle snaps instantly to north.")]
    [SerializeField] private bool smoothRotation = true;

    [Header("VR Interaction")]
    [Tooltip("Only rotate needle when compass is being held. If false, always rotates.")]
    [SerializeField] private bool onlyRotateWhenHeld = true;

    private static readonly Vector3 WorldNorth = Vector3.forward;

    private XRGrabInteractable grabInteractable;
    private bool isBeingHeld = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (needleTransform == null)
        {
            Transform found = transform.Find("Needle");
            if (found != null)
                needleTransform = found;
            else
                Debug.LogWarning("[CompassController] Needle transform not assigned and 'Needle' child not found.", this);
        }
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void Update()
    {
        if (onlyRotateWhenHeld && !isBeingHeld)
            return;

        if (needleTransform == null)
            return;

        // Project world north onto the compass body's XZ plane (local space)
        Vector3 localNorth = transform.InverseTransformDirection(WorldNorth);
        localNorth.y = 0f;

        // Degenerate case: compass is pointing straight up/down — keep last rotation
        if (localNorth.sqrMagnitude < 0.001f)
            return;

        localNorth.Normalize();

        // Needle's forward (local Z) should point toward north in compass local space.
        // We only rotate around Y axis to keep the needle flat on the compass face.
        float targetAngle = Mathf.Atan2(localNorth.x, localNorth.z) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

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

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isBeingHeld = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isBeingHeld = false;
    }

    /// <summary>
    /// Manually set the needle pivot transform at runtime.
    /// </summary>
    public void SetNeedleTransform(Transform needle)
    {
        needleTransform = needle;
    }

    /// <summary>
    /// Returns the current heading angle in degrees (0 = North, 90 = East, 180 = South, 270 = West).
    /// Based on the compass body's forward direction projected onto the world XZ plane.
    /// </summary>
    public float GetHeading()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return 0f;

        forward.Normalize();
        float angle = Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
        return angle < 0f ? angle + 360f : angle;
    }

    /// <summary>
    /// Returns the current cardinal direction label based on heading.
    /// </summary>
    public string GetCardinalDirection()
    {
        float heading = GetHeading();
        return heading switch
        {
            < 22.5f or >= 337.5f => "N",
            < 67.5f => "NE",
            < 112.5f => "E",
            < 157.5f => "SE",
            < 202.5f => "S",
            < 247.5f => "SW",
            < 292.5f => "W",
            _ => "NW"
        };
    }

    /// <summary>
    /// Gets whether the compass is currently being held by the player.
    /// </summary>
    public bool IsBeingHeld() => isBeingHeld;
}
