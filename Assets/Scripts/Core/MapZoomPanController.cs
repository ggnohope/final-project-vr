using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Core
{
    /// <summary>
    /// Translates VR joystick input into pan and zoom commands for MapTileRenderer.
    ///
    /// INPUT MAPPING:
    /// - Left joystick  → Pan map (move view center)
    /// - Right joystick Y-axis → Zoom in/out
    ///
    /// SETUP:
    /// 1. Assign panAction  (left joystick, e.g. XRI Left Locomotion/Move).
    /// 2. Assign zoomAction (right joystick, e.g. XRI Right Locomotion/Turn).
    /// 3. Assign tileRenderer.
    /// </summary>
    public class MapZoomPanController : MonoBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("Left joystick action for panning. e.g. XRI Left Locomotion/Move")]
        [SerializeField] private InputActionProperty panAction;

        [Tooltip("Right joystick action for zooming. Y-axis controls zoom in/out.")]
        [SerializeField] private InputActionProperty zoomAction;

        [Header("References")]
        [SerializeField] private MapTileRenderer tileRenderer;

        [Tooltip("Optional: assign ControllerInputActionManager from RightHandController " +
                 "to include its enabled state in zoom diagnostics.")]
        [SerializeField] private ControllerInputActionManager rightHandInputActionManager;

        [Header("Pan Settings")]
        [Tooltip("Canvas pixels per second moved at full joystick deflection.")]
        [SerializeField] private float panSpeed = 300f;

        [Tooltip("Joystick magnitude below which pan is ignored.")]
        [SerializeField] [Range(0f, 1f)] private float panDeadzone = 0.15f;

        [Header("Zoom Settings")]
        [Tooltip("Seconds to wait before allowing the next zoom step while joystick is held.")]
        [SerializeField] private float zoomCooldown = 0.4f;

        [Tooltip("Y-axis magnitude below which zoom is ignored.")]
        [SerializeField] [Range(0f, 1f)] private float zoomDeadzone = 0.3f;

        [Tooltip("Invert the zoom axis. Enable when joystick-up should zoom in " +
                 "(increase detail). Most VR controllers have Y axis physically inverted " +
                 "on the Turn action, so this should be true.")]
        [SerializeField] private bool invertZoom = true;

        private float lastZoomTime = -999f;

        // Tracks whether we force-enabled a reference action that was already disabled
        // (e.g. the Turn action is disabled when the project uses snap turn).
        // We must restore it to disabled on OnDisable to avoid breaking locomotion state.
        private bool forcedZoomActionEnabled = false;
        private bool forcedPanActionEnabled  = false;

        private void OnEnable()
        {
            forcedPanActionEnabled  = ForceEnableAction(panAction);
            forcedZoomActionEnabled = ForceEnableAction(zoomAction);
        }

        private void OnDisable()
        {
            RestoreAction(panAction,  ref forcedPanActionEnabled);
            RestoreAction(zoomAction, ref forcedZoomActionEnabled);
        }

        private void Update()
        {
            if (tileRenderer == null) return;
            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            if (panAction.action == null) return;

            Vector2 input = panAction.action.ReadValue<Vector2>();
            if (input.magnitude < panDeadzone) return;

            float remapped = (input.magnitude - panDeadzone) / (1f - panDeadzone);
            Vector2 direction = input.normalized * remapped;

            tileRenderer.Pan(direction * (panSpeed * Time.deltaTime));
        }

        private void HandleZoom()
        {
            if (zoomAction.action == null) return;

            float rawY = ReadRawYFromAction(zoomAction.action);
            float zoomInput = invertZoom ? -rawY : rawY;

            if (Mathf.Abs(zoomInput) < zoomDeadzone) return;
            if (Time.time - lastZoomTime < zoomCooldown) return;

            lastZoomTime = Time.time;

            // Pass only the sign — AdjustZoom snaps to the next integer level.
            tileRenderer.AdjustZoom(Mathf.Sign(zoomInput));
        }

        /// <summary>
        /// Reads the Y component directly from the first physical control bound to the action,
        /// bypassing any action-level or binding-level processors.
        /// </summary>
        private static float ReadRawYFromAction(InputAction action)
        {
            foreach (InputControl control in action.controls)
            {
                if (control is StickControl stick)  return stick.y.ReadValue();
                if (control is Vector2Control vec2)  return vec2.y.ReadValue();
            }
            return 0f;
        }

        /// <summary>
        /// Ensures the action is enabled when the map opens.
        /// - DIRECT actions always need explicit Enable().
        /// - REFERENCE actions: only force-enable if currently disabled (e.g. Turn disabled
        ///   in snap-turn mode). Tracks this so we can restore state in OnDisable.
        /// Returns true if a force-enable was applied to a reference action.
        /// </summary>
        private static bool ForceEnableAction(InputActionProperty prop)
        {
            if (prop.action == null) return false;

            if (prop.reference == null)
            {
                prop.action.Enable();
                return false;
            }

            if (!prop.action.enabled)
            {
                prop.action.Enable();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Restores an action's enabled state when the map closes.
        /// - DIRECT actions are always disabled.
        /// - REFERENCE actions are disabled only if we force-enabled them.
        /// </summary>
        private static void RestoreAction(InputActionProperty prop, ref bool wasForced)
        {
            if (prop.action == null) return;

            if (prop.reference == null || wasForced)
                prop.action.Disable();

            wasForced = false;
        }
    }
}
