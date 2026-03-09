using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRDrawing.Tools
{
    public class UIRayDrawingTool : DrawingToolBase
    {
        [Header("UI Ray Drawing Settings")]
        [SerializeField] private XRRayInteractor rayInteractor;
        [SerializeField] private bool autoFindRayInteractor = true;
        [SerializeField] private LayerMask surfaceLayerMask = 1 << 3;
        
        [Header("Tool Properties")]
        [SerializeField] private Color drawColor = Color.black;
        [SerializeField] private float drawWidth = 0.0008f;
        [SerializeField] private ToolType toolType = ToolType.Pen;
        [SerializeField] private string toolId = "UIRayPen";

        private DrawingSurface currentSurface;
        private bool isDrawing = false;
        private bool wasDrawActionActive  = false;

        [Header("Drawing Input")]
        [SerializeField] private InputActionProperty drawAction;

        public override ToolType Type => toolType;
        public override Color Color => drawColor;
        public override float Width => drawWidth;
        public override string ToolId => toolId;

        protected override void Awake()
        {
            base.Awake();

            if (autoFindRayInteractor && rayInteractor == null)
                rayInteractor = GetComponent<XRRayInteractor>();

            if (rayInteractor == null)
            {
                Debug.LogError($"UIRayDrawingTool on {gameObject.name}: XRRayInteractor not found!");
                enabled = false;
                return;
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.volume = audioVolume;
            }

            if (DrawingSystemManager.Instance != null)
                DrawingSystemManager.Instance.RegisterTool(this);
            else
                Debug.LogWarning("UIRayDrawingTool: DrawingSystemManager.Instance is NULL.");
        }


        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            if (isDrawing && currentSurface != null)
            {
                OnSurfaceExited?.Invoke(this, currentSurface);
                isDrawing = false;
                currentSurface = null;
            }
        }

        private void Update()
        {
            if (!isEnabled || rayInteractor == null)
            {
                // Only log once per state change, not every frame
                return;
            }

            if (VRDrawing.Photo.PhotoPlacementManager.Instance != null && 
                VRDrawing.Photo.PhotoPlacementManager.Instance.IsInPlacementMode)
            {
                if (isDrawing) EndDrawing();
                return;
            }

            bool isDrawActionActive = drawAction.action != null && drawAction.action.IsPressed();
            
            if (isDrawActionActive)
            {
                HandleRayDrawing();
            }
            else if (wasDrawActionActive)
            {
                EndDrawing();
            }

            wasDrawActionActive = isDrawActionActive;
        }


        private void HandleRayDrawing()
        {
            bool hasHit = rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit);
            if (!hasHit) { EndDrawing(); return; }

            DrawingSurface surface = hit.collider.GetComponent<DrawingSurface>();
            bool isOnCorrectLayer = ((1 << hit.collider.gameObject.layer) & surfaceLayerMask) != 0;

            if (surface != null && isOnCorrectLayer)
            {
                if (!isDrawing || currentSurface != surface)
                {
                    if (isDrawing && currentSurface != null && currentSurface != surface)
                        OnSurfaceExited?.Invoke(this, currentSurface);

                    currentSurface = surface;
                    isDrawing = true;
                    Debug.Log($"[UIRayDrawingTool] Drawing started on '{surface.name}' at {hit.point}");
                    OnSurfaceTouched?.Invoke(this, surface, hit.point);
                    PlayAudio(touchSurfaceClip);
                }
                else
                {
                    OnSurfaceDraw?.Invoke(this, surface, hit.point);
                }
            }
            else
            {
                if (!hasHit)
                    Debug.LogWarning($"[UIRayDrawingTool] No 3D hit. Layer check={isOnCorrectLayer}, surface={surface != null}");
                EndDrawing();
            }
        }


        private void EndDrawing()
        {
            if (isDrawing && currentSurface != null)
            {
                Debug.Log($"[UIRayDrawingTool] ❌ DRAWING ENDED");
                OnSurfaceExited?.Invoke(this, currentSurface);
                isDrawing = false;
                currentSurface = null;
            }
        }


        public void SetDrawColor(Color color)
        {
            drawColor = color;
        }

        public void SetDrawWidth(float width)
        {
            drawWidth = width;
        }

        public override void SetColor(Color color)
        {
            SetDrawColor(color);
            Debug.Log($"[UIRayDrawingTool] ✅ SetColor called! New color: {color} (R:{color.r}, G:{color.g}, B:{color.b})");
        }

        public override void SetThickness(float thickness)
        {
            SetDrawWidth(thickness);
        }

        public override void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            // CRITICAL: UIRayDrawingTool must not deactivate the GameObject
            // because it's attached to the UI Ray Interactor, not a grabbable tool.
            // Only set the isEnabled flag to control Update() execution.
            Debug.Log($"[UIRayDrawingTool] SetEnabled({enabled})");
            
            if (!enabled)
            {
                EndDrawing();
            }
        }

        public void SetToolType(ToolType type)
        {
            toolType = type;
        }
    }
}
