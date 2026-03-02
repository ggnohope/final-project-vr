using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VRDrawing.UI
{
    /// <summary>
    /// Scrolls the gallery ScrollRect using the right controller thumbstick (Y axis).
    /// Reads directly from the XR device to bypass ControllerInputActionManager,
    /// which disables all locomotion/thumbstick actions when hovering over UI panels.
    /// </summary>
    public class GalleryScrollController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Scroll Settings")]
        [SerializeField] private float scrollSpeed = 1.5f;
        [SerializeField] private float deadzone = 0.15f;

        private InputDevice _rightController;
        private readonly List<InputDevice> _deviceBuffer = new List<InputDevice>();

        private void OnEnable()
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            RefreshRightController();
        }

        private void OnDisable()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
        }

        private void OnDeviceConnected(InputDevice device)
        {
            const InputDeviceCharacteristics rightController =
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;

            if ((device.characteristics & rightController) == rightController)
                _rightController = device;
        }

        /// <summary>Queries connected XR devices for the right-hand controller.</summary>
        private void RefreshRightController()
        {
            _deviceBuffer.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                _deviceBuffer);

            if (_deviceBuffer.Count > 0)
                _rightController = _deviceBuffer[0];
        }

        private void Update()
        {
            if (scrollRect == null)
                return;

            if (!_rightController.isValid)
            {
                RefreshRightController();
                return;
            }

            if (!_rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
                return;

            if (Mathf.Abs(thumbstick.y) > deadzone)
            {
                float normalized = (Mathf.Abs(thumbstick.y) - deadzone) / (1f - deadzone);
                float delta = Mathf.Sign(thumbstick.y) * normalized * scrollSpeed * Time.deltaTime;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta);
            }
        }
    }
}
