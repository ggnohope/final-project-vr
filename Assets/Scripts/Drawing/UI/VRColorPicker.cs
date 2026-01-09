using UnityEngine;
using UnityEngine.UI;
using VRDrawing.Tools;

namespace VRDrawing.UI
{
    public class VRColorPicker : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private PenTool targetPen;

        [Header("Color Buttons")]
        [SerializeField] private Button[] colorButtons;
        [SerializeField] private Color[] colorPalette = new Color[]
        {
            Color.black,
            Color.white,
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta
        };

        [Header("Feedback")]
        [SerializeField] private Image currentColorDisplay;
        [SerializeField] private AudioClip colorChangeClip;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        private void Start()
        {
            SetupColorButtons();
            
            if (targetPen != null && currentColorDisplay != null)
            {
                currentColorDisplay.color = targetPen.Color;
            }
        }

        private void SetupColorButtons()
        {
            if (colorButtons == null || colorButtons.Length == 0) return;

            int colorCount = Mathf.Min(colorButtons.Length, colorPalette.Length);

            for (int i = 0; i < colorCount; i++)
            {
                if (colorButtons[i] == null) continue;

                int colorIndex = i;
                Color color = colorPalette[i];

                Image buttonImage = colorButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = color;
                }

                colorButtons[i].onClick.AddListener(() => SetColor(color));
            }
        }

        public void SetColor(Color color)
        {
            if (targetPen != null)
            {
                targetPen.SetColor(color);
                
                if (currentColorDisplay != null)
                {
                    currentColorDisplay.color = color;
                }

                if (colorChangeClip != null && audioSource != null)
                {
                    audioSource.PlayOneShot(colorChangeClip);
                }
            }
        }

        public void SetTargetPen(PenTool pen)
        {
            targetPen = pen;
            
            if (targetPen != null && currentColorDisplay != null)
            {
                currentColorDisplay.color = targetPen.Color;
            }
        }
    }
}
