using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    /// <summary>
    /// Discrete hotspot-based navigation system for VR world map interaction.
    /// Replaces free cursor movement with step-based hotspot selection using joystick input.
    /// 
    /// KEY FEATURES:
    /// - Joystick threshold detection (not continuous movement)
    /// - Debounce timer prevents rapid cycling
    /// - Visual feedback through MapHotspot components
    /// - Optional camera focus on selected hotspot
    /// - Optional tooltip display
    /// - Wrapping navigation (last to first)
    /// 
    /// SETUP:
    /// 1. Use Tools > World Map > Hotspot Setup Helper to generate hotspots
    /// 2. Add this component to scene
    /// 3. Assign Input Actions (joystick move, confirm button)
    /// 4. Assign references (WorldMapController, SceneMapData)
    /// 5. Populate hotspots array with all MapHotspot GameObjects
    /// 
    /// NAVIGATION:
    /// - Joystick Right → Next hotspot
    /// - Joystick Left → Previous hotspot
    /// - Joystick Up/Down → Vertical navigation (if enabled)
    /// - Confirm Button → Load selected region
    /// 
    /// CONFIGURATION:
    /// - joystickThreshold: Input sensitivity (0-1, default 0.5)
    /// - navigationDebounceTime: Minimum time between navigations (default 0.3s)
    /// - enableWrapping: Loop from last to first (default true)
    /// - enableVerticalNavigation: Allow up/down input (default false)
    /// </summary>
    public class MapHotspotNavigator : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionProperty joystickMoveAction;
        [SerializeField] private InputActionProperty confirmButtonAction;

        [Header("References")]
        [SerializeField] private WorldMapController worldMapController;
        [SerializeField] private SceneMapData sceneMapData;
        [SerializeField] private MapHotspot[] hotspots;

        [Header("Navigation Settings")]
        [SerializeField] private float joystickThreshold = 0.5f;
        [SerializeField] private float navigationDebounceTime = 0.3f;
        [SerializeField] private bool enableWrapping = true;
        [SerializeField] private bool enableVerticalNavigation = false;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip navigationSound;
        [SerializeField] private AudioClip confirmSound;

        [Header("Camera Focus (Optional)")]
        [SerializeField] private bool enableCameraFocus = false;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float focusTransitionSpeed = 2f;

        [Header("Tooltip (Optional)")]
        [SerializeField] private MapRegionTooltip tooltip;
        [SerializeField] private bool showTooltipOnSelection = true;
        [SerializeField] private Vector2 tooltipOffset = new Vector2(0, 50);

        private int currentHotspotIndex = 0;
        private float lastNavigationTime = 0f;
        private Vector2 lastJoystickInput = Vector2.zero;
        private bool wasJoystickActive = false;
        private Dictionary<string, int> regionToIndexMap;

        private void OnEnable()
        {
            if (joystickMoveAction.action != null)
            {
                joystickMoveAction.action.Enable();
            }

            if (confirmButtonAction.action != null)
            {
                confirmButtonAction.action.Enable();
                confirmButtonAction.action.performed += OnConfirmPressed;
            }
        }

        private void OnDisable()
        {
            if (joystickMoveAction.action != null)
            {
                joystickMoveAction.action.Disable();
            }

            if (confirmButtonAction.action != null)
            {
                confirmButtonAction.action.performed -= OnConfirmPressed;
                confirmButtonAction.action.Disable();
            }
        }

        private void Start()
        {
            InitializeHotspots();
            
            if (enabled)
            {
                SelectHotspot(currentHotspotIndex, immediate: true);
            }
        }

        private void Update()
        {
            HandleJoystickNavigation();
        }

        private void InitializeHotspots()
        {
            if (hotspots == null || hotspots.Length == 0)
            {
                Debug.LogError("[MapHotspotNavigator] No hotspots assigned!");
                return;
            }

            regionToIndexMap = new Dictionary<string, int>();

            for (int i = 0; i < hotspots.Length; i++)
            {
                if (hotspots[i] != null)
                {
                    hotspots[i].Initialize(hotspots[i].RegionId, i);
                    regionToIndexMap[hotspots[i].RegionId] = i;
                }
            }

            Debug.Log($"[MapHotspotNavigator] Initialized {hotspots.Length} hotspots");
        }

        private void HandleJoystickNavigation()
        {
            if (Time.time - lastNavigationTime < navigationDebounceTime)
            {
                return;
            }

            Vector2 joystickInput = GetMoveInput();

            bool isJoystickActive = joystickInput.magnitude > joystickThreshold;

            if (isJoystickActive && !wasJoystickActive)
            {
                ProcessNavigationInput(joystickInput);
                lastNavigationTime = Time.time;
            }

            wasJoystickActive = isJoystickActive;
            lastJoystickInput = joystickInput;
        }

        private Vector2 GetMoveInput()
        {
            if (joystickMoveAction.action != null)
            {
                return joystickMoveAction.action.ReadValue<Vector2>();
            }

            return Vector2.zero;
        }

        private void ProcessNavigationInput(Vector2 input)
        {
            if (hotspots == null || hotspots.Length == 0) return;

            int targetIndex = currentHotspotIndex;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                if (input.x > joystickThreshold)
                {
                    targetIndex = GetNextHotspotIndex(1);
                }
                else if (input.x < -joystickThreshold)
                {
                    targetIndex = GetNextHotspotIndex(-1);
                }
            }
            else if (enableVerticalNavigation)
            {
                if (input.y > joystickThreshold)
                {
                    targetIndex = GetNextHotspotIndex(1);
                }
                else if (input.y < -joystickThreshold)
                {
                    targetIndex = GetNextHotspotIndex(-1);
                }
            }

            if (targetIndex != currentHotspotIndex)
            {
                SelectHotspot(targetIndex);
                PlaySound(navigationSound);
            }
        }

        private int GetNextHotspotIndex(int direction)
        {
            int newIndex = currentHotspotIndex + direction;

            if (enableWrapping)
            {
                if (newIndex >= hotspots.Length)
                {
                    newIndex = 0;
                }
                else if (newIndex < 0)
                {
                    newIndex = hotspots.Length - 1;
                }
            }
            else
            {
                newIndex = Mathf.Clamp(newIndex, 0, hotspots.Length - 1);
            }

            return newIndex;
        }

        private void SelectHotspot(int index, bool immediate = false)
        {
            if (index < 0 || index >= hotspots.Length) return;

            if (hotspots[currentHotspotIndex] != null)
            {
                hotspots[currentHotspotIndex].SetActive(false, immediate);
            }

            currentHotspotIndex = index;

            if (hotspots[currentHotspotIndex] != null)
            {
                hotspots[currentHotspotIndex].SetActive(true, immediate);

                MapRegion? region = GetCurrentRegion();
                if (region.HasValue)
                {
                    if (showTooltipOnSelection && tooltip != null)
                    {
                        Vector3 hotspotPosition = hotspots[currentHotspotIndex].transform.position;
                        string description = $"Region ID: {region.Value.regionId}\nClick Trigger to Load";
                        tooltip.Show(region.Value.displayName, description, hotspotPosition);
                    }

                    if (enableCameraFocus && mainCamera != null)
                    {
                        FocusCameraOnHotspot(hotspots[currentHotspotIndex]);
                    }
                }
            }

            Debug.Log($"[MapHotspotNavigator] Selected hotspot {currentHotspotIndex}");
        }

        private void OnConfirmPressed(InputAction.CallbackContext context)
        {
            ConfirmSelection();
        }

        public void ConfirmSelection()
        {
            MapRegion? region = GetCurrentRegion();
            if (!region.HasValue)
            {
                Debug.LogWarning("[MapHotspotNavigator] No region associated with current hotspot");
                return;
            }

            PlaySound(confirmSound);

            if (tooltip != null)
            {
                tooltip.Hide();
            }

            Debug.Log($"[MapHotspotNavigator] Confirmed: {region.Value.displayName}");

            if (worldMapController != null)
            {
                worldMapController.LoadRegion(region.Value);
            }
        }

        private MapRegion? GetCurrentRegion()
        {
            if (hotspots == null || currentHotspotIndex >= hotspots.Length)
            {
                return null;
            }

            MapHotspot hotspot = hotspots[currentHotspotIndex];
            if (hotspot == null || sceneMapData == null)
            {
                return null;
            }

            return sceneMapData.GetRegionById(hotspot.RegionId);
        }

        private void FocusCameraOnHotspot(MapHotspot hotspot)
        {
            if (mainCamera == null || hotspot == null) return;

            StartCoroutine(SmoothCameraFocus(hotspot.transform.position));
        }

        private IEnumerator SmoothCameraFocus(Vector3 targetPosition)
        {
            Vector3 startPosition = mainCamera.transform.position;
            Vector3 endPosition = new Vector3(targetPosition.x, targetPosition.y, startPosition.z);

            float elapsed = 0f;
            float duration = 1f / focusTransitionSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                mainCamera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                yield return null;
            }

            mainCamera.transform.position = endPosition;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void NavigateToRegion(string regionId)
        {
            if (regionToIndexMap != null && regionToIndexMap.TryGetValue(regionId, out int index))
            {
                SelectHotspot(index);
            }
        }

        public void ResetToFirstHotspot()
        {
            SelectHotspot(0);
        }

        public MapHotspot CurrentHotspot => hotspots != null && currentHotspotIndex < hotspots.Length 
            ? hotspots[currentHotspotIndex] 
            : null;

        public int CurrentIndex => currentHotspotIndex;
        public int HotspotCount => hotspots != null ? hotspots.Length : 0;
    }
}
