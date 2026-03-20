using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace Core
{
    /// <summary>
    /// A world-space video panel that appears in front of the user for flycam preview.
    /// Loads a VideoClip from Resources, allows seek/play/pause, and has a close button.
    /// </summary>
    public class FlyCamVideoPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RawImage videoDisplay;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private TMP_Text playPauseButtonText;
        [SerializeField] private Slider seekSlider;
        [SerializeField] private TMP_Text currentTimeText;
        [SerializeField] private TMP_Text totalTimeText;
        [SerializeField] private TMP_Text regionNameText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Video Player")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("Positioning")]
        [SerializeField] private float distanceFromPlayer = 2f;
        [SerializeField] private float fadeSpeed = 8f;

        [Header("Map Integration")]
        [Tooltip("WorldMapCanvas to hide while the FlyCam panel is open.")]
        [SerializeField] private GameObject worldMapCanvas;

        [Header("Render Texture")]
        [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(1280, 720);

        private RenderTexture renderTexture;
        private bool isVisible = false;
        private bool isDraggingSeek = false;
        private Transform playerCamera;
        private Coroutine positionCoroutine;

        private const string PlayLabel = "▶";
        private const string PauseLabel = "⏸";

        private void Awake()
        {
            playerCamera = Camera.main?.transform;

            SetupVideoPlayer();
            RegisterButtonListeners();

            // Auto-find WorldMapCanvas if not assigned
            if (worldMapCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Canvas c in canvases)
                {
                    if (c.gameObject.name == "WorldMapCanvas")
                    {
                        worldMapCanvas = c.gameObject;
                        break;
                    }
                }
            }

            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void SetupVideoPlayer()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();

            renderTexture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 0);
            renderTexture.Create();

            videoPlayer.renderMode    = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping     = false;
            videoPlayer.playOnAwake   = false;
            videoPlayer.timeUpdateMode  = VideoTimeUpdateMode.GameTime;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            if (videoDisplay != null)
                videoDisplay.texture = renderTexture;

            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEnded;
            videoPlayer.errorReceived    += OnVideoError;

            Debug.Log($"[FlyCamVideoPanel] SetupVideoPlayer — renderTexture={renderTexture.width}x{renderTexture.height} " +
                      $"videoDisplay={(videoDisplay != null ? "OK" : "NULL")} " +
                      $"targetTexture={(videoPlayer.targetTexture != null ? "OK" : "NULL")}");
        }

        private void RegisterButtonListeners()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(TogglePlayPause);

            if (seekSlider != null)
            {
                seekSlider.onValueChanged.AddListener(OnSeekSliderChanged);
            }
        }

        private void Update()
        {
            if (!isVisible || videoPlayer == null) return;

            if (videoPlayer.isPrepared && !isDraggingSeek)
            {
                UpdateSeekUI();
            }
        }

        private void UpdateSeekUI()
        {
            double duration = videoPlayer.length;
            if (duration <= 0) return;

            double current = videoPlayer.time;
            float normalized = (float)(current / duration);

            if (seekSlider != null)
                seekSlider.SetValueWithoutNotify(normalized);

            if (currentTimeText != null)
                currentTimeText.text = FormatTime(current);

            if (totalTimeText != null)
                totalTimeText.text = FormatTime(duration);
        }

        private string FormatTime(double seconds)
        {
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            return $"{m:00}:{s:00}";
        }

        /// <summary>Shows the panel with the given video resource and region name.</summary>
        public void Show(string videoResourcePath, string regionName)
        {
            gameObject.SetActive(true);
            PositionInFrontOfPlayer();
            isVisible = true;

            if (regionNameText != null)
                regionNameText.text = regionName;

            if (loadingIndicator != null)
                loadingIndicator.SetActive(true);

            if (seekSlider != null)
                seekSlider.SetValueWithoutNotify(0f);

            if (playPauseButtonText != null)
                playPauseButtonText.text = PauseLabel;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Hide the world map so it doesn't block view or intercept the ray
            if (worldMapCanvas != null)
                worldMapCanvas.SetActive(false);

            StartCoroutine(FadeTo(1f));
            LoadAndPlayVideo(videoResourcePath);
        }

        /// <summary>Hides and unloads the video panel, then restores the world map.</summary>
        public void Hide()
        {
            isVisible = false;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            StartCoroutine(FadeOutAndDeactivate());
        }

        private IEnumerator FadeOutAndDeactivate()
        {
            yield return FadeTo(0f);
            StopVideo();

            // Restore world map after video panel is fully faded out
            if (worldMapCanvas != null)
                worldMapCanvas.SetActive(true);
        }

        private IEnumerator FadeTo(float target)
        {
            if (canvasGroup == null) yield break;

            while (!Mathf.Approximately(canvasGroup.alpha, target))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.unscaledDeltaTime * fadeSpeed);
                yield return null;
            }

            canvasGroup.alpha = target;
        }

        private void LoadAndPlayVideo(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return;

            VideoClip clip = Resources.Load<VideoClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"[FlyCamVideoPanel] VideoClip not found at Resources/{resourcePath}");
                if (loadingIndicator != null)
                    loadingIndicator.SetActive(false);
                return;
            }

            videoPlayer.Stop();
            videoPlayer.clip = clip;
            videoPlayer.Prepare();
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            videoPlayer.Play();

            if (playPauseButtonText != null)
                playPauseButtonText.text = PauseLabel;
        }

        private void OnVideoEnded(VideoPlayer vp)
        {
            if (playPauseButtonText != null)
                playPauseButtonText.text = PlayLabel;

            if (seekSlider != null)
                seekSlider.SetValueWithoutNotify(1f);
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[FlyCamVideoPanel] VideoPlayer error: {message}");

            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }

        private void TogglePlayPause()
        {
            if (!videoPlayer.isPrepared) return;

            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                if (playPauseButtonText != null)
                    playPauseButtonText.text = PlayLabel;
            }
            else
            {
                videoPlayer.Play();
                if (playPauseButtonText != null)
                    playPauseButtonText.text = PauseLabel;
            }
        }

        private void OnSeekSliderChanged(float value)
        {
            if (!videoPlayer.isPrepared || videoPlayer.length <= 0) return;
            isDraggingSeek = true;
            videoPlayer.time = value * videoPlayer.length;
            isDraggingSeek = false;
        }

        private void StopVideo()
        {
            if (videoPlayer == null) return;
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        private void PositionInFrontOfPlayer()
        {
            if (playerCamera == null)
                playerCamera = Camera.main?.transform;

            if (playerCamera == null) return;

            Vector3 forward = playerCamera.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            transform.position = playerCamera.position + forward * distanceFromPlayer;

            Vector3 lookDir = transform.position - playerCamera.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.loopPointReached -= OnVideoEnded;
                videoPlayer.errorReceived    -= OnVideoError;
            }
        }
    }
}
