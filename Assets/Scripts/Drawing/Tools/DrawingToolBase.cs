using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRDrawing.Tools
{
    public enum ToolType
    {
        Pen,
        Eraser,
        Highlighter
    }

    public abstract class DrawingToolBase : MonoBehaviour
    {
        [Header("Tool Settings")]
        [SerializeField] protected Transform toolTip;
        [SerializeField] protected float tipRadius = 0.005f;
        [SerializeField] protected LayerMask drawingSurfaceLayer;

        [Header("Haptics")]
        [SerializeField] protected bool enableHaptics = true;
        [SerializeField] protected float hapticIntensity = 0.3f;
        [SerializeField] protected float hapticDuration = 0.05f;

        [Header("Audio")]
        [SerializeField] protected AudioClip touchSurfaceClip;
        [SerializeField] protected AudioClip drawingClip;
        [SerializeField] protected float audioVolume = 0.3f;

        protected XRGrabInteractable grabInteractable;
        protected AudioSource audioSource;
        protected bool isHeld = false;
        protected Rigidbody rb;

        public abstract ToolType Type { get; }
        public abstract Color Color { get; }
        public abstract float Width { get; }
        public abstract string ToolId { get; }

        public bool IsHeld => isHeld;
        public Vector3 TipPosition => toolTip != null ? toolTip.position : transform.position;
        public Vector3 TipForward => toolTip != null ? toolTip.forward : transform.forward;

        public System.Action<DrawingToolBase, DrawingSurface, Vector3> OnSurfaceTouched;
        public System.Action<DrawingToolBase, DrawingSurface, Vector3> OnSurfaceDraw;
        public System.Action<DrawingToolBase, DrawingSurface> OnSurfaceExited;

        protected virtual void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = false;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = audioVolume;

            if (toolTip == null)
            {
                Transform existingTip = transform.Find("ToolTip");
                if (existingTip != null)
                {
                    toolTip = existingTip;
                }
                else
                {
                    GameObject tipObj = new GameObject("ToolTip");
                    tipObj.transform.SetParent(transform);
                    tipObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                    tipObj.transform.localRotation = Quaternion.identity;
                    toolTip = tipObj.transform;
                }
            }

            SphereCollider tipCollider = toolTip.gameObject.GetComponent<SphereCollider>();
            if (tipCollider == null)
            {
                tipCollider = toolTip.gameObject.AddComponent<SphereCollider>();
            }
            tipCollider.radius = tipRadius;
            tipCollider.isTrigger = true;
        }

        protected virtual void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrabbed);
                grabInteractable.selectExited.AddListener(OnReleased);
            }
        }

        protected virtual void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }

        protected virtual void OnGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
        {
            isHeld = true;
        }

        protected virtual void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
        {
            isHeld = false;
        }

        protected void PlayHapticFeedback(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
        {
            if (!enableHaptics) return;

            var interactor = args.interactorObject;
            var controllerInteractor = interactor as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor;
            
            if (controllerInteractor != null)
            {
                var xrController = (interactor as MonoBehaviour)?.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>();
                if (xrController != null)
                {
                    xrController.SendHapticImpulse(hapticIntensity, hapticDuration);
                }
            }
        }

        protected void PlayAudio(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public virtual void OnToolTipEnterSurface(DrawingSurface surface, Vector3 contactPoint)
        {
            OnSurfaceTouched?.Invoke(this, surface, contactPoint);
        }

        public virtual void OnToolTipStaySurface(DrawingSurface surface, Vector3 contactPoint)
        {
            OnSurfaceDraw?.Invoke(this, surface, contactPoint);
        }

        public virtual void OnToolTipExitSurface(DrawingSurface surface)
        {
            OnSurfaceExited?.Invoke(this, surface);
        }
    }
}
