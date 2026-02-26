using UnityEngine;

namespace Core
{
    public class FloatingAnimation : MonoBehaviour
    {
        [Header("Float Settings")]
        [SerializeField] private bool enableFloat = true;
        [SerializeField] private float amplitude = 0.05f;
        [SerializeField] private float frequency = 1f;
        [SerializeField] private Vector3 floatAxis = Vector3.up;

        [Header("Rotation Settings")]
        [SerializeField] private bool enableRotation = false;
        [SerializeField] private float rotationSpeed = 20f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Header("Pulse Settings")]
        [SerializeField] private bool enablePulse = false;
        [SerializeField] private float pulseAmount = 0.05f;
        [SerializeField] private float pulseSpeed = 2f;

        private Vector3 startPosition;
        private Vector3 startScale;
        private float timeOffset;

        private void Start()
        {
            startPosition = transform.localPosition;
            startScale = transform.localScale;
            timeOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (enableFloat)
            {
                ApplyFloatAnimation();
            }

            if (enableRotation)
            {
                ApplyRotationAnimation();
            }

            if (enablePulse)
            {
                ApplyPulseAnimation();
            }
        }

        private void ApplyFloatAnimation()
        {
            float offset = Mathf.Sin((Time.time + timeOffset) * frequency) * amplitude;
            transform.localPosition = startPosition + floatAxis.normalized * offset;
        }

        private void ApplyRotationAnimation()
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
        }

        private void ApplyPulseAnimation()
        {
            float scale = 1f + Mathf.Sin((Time.time + timeOffset) * pulseSpeed) * pulseAmount;
            transform.localScale = startScale * scale;
        }

        public void SetEnabled(bool enabled)
        {
            enableFloat = enabled;
            if (!enabled)
            {
                transform.localPosition = startPosition;
                transform.localScale = startScale;
            }
        }

        /// <summary>Resets the base local position for float animation to the current local position. Call this after repositioning the GameObject.</summary>
        public void ResetBasePosition()
        {
            startPosition = transform.localPosition;
        }
    }
}
