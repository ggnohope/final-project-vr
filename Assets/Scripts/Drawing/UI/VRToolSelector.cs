using UnityEngine;
using UnityEngine.UI;
using VRDrawing.Tools;
using TMPro;

namespace VRDrawing.UI
{
    public class VRToolSelector : MonoBehaviour
    {
        [Header("Tool Prefabs")]
        [SerializeField] private GameObject penToolPrefab;
        [SerializeField] private GameObject eraserToolPrefab;

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.1f, 0.1f);

        [Header("UI Buttons")]
        [SerializeField] private Button spawnPenButton;
        [SerializeField] private Button spawnEraserButton;
        [SerializeField] private Button returnToolButton;

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI toolInfoText;

        [Header("Audio")]
        [SerializeField] private AudioClip spawnClip;
        [SerializeField] private AudioClip returnClip;

        private DrawingToolBase currentTool;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        private void OnEnable()
        {
            if (spawnPenButton != null)
                spawnPenButton.onClick.AddListener(SpawnPen);
            
            if (spawnEraserButton != null)
                spawnEraserButton.onClick.AddListener(SpawnEraser);
            
            if (returnToolButton != null)
                returnToolButton.onClick.AddListener(ReturnTool);

            UpdateUI();
        }

        private void OnDisable()
        {
            if (spawnPenButton != null)
                spawnPenButton.onClick.RemoveListener(SpawnPen);
            
            if (spawnEraserButton != null)
                spawnEraserButton.onClick.RemoveListener(SpawnEraser);
            
            if (returnToolButton != null)
                returnToolButton.onClick.RemoveListener(ReturnTool);
        }

        private void Update()
        {
            UpdateUI();
        }

        private void SpawnPen()
        {
            SpawnTool(penToolPrefab, "Pen");
        }

        private void SpawnEraser()
        {
            SpawnTool(eraserToolPrefab, "Eraser");
        }

        private void SpawnTool(GameObject prefab, string toolName)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"{toolName} prefab not assigned!");
                return;
            }

            if (currentTool != null)
            {
                ReturnTool();
            }

            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position + spawnOffset : Vector3.zero;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject toolObj = Instantiate(prefab, spawnPosition, spawnRotation);
            currentTool = toolObj.GetComponent<DrawingToolBase>();

            if (currentTool != null && DrawingSystemManager.Instance != null)
            {
                DrawingSystemManager.Instance.RegisterTool(currentTool);
            }

            if (spawnClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(spawnClip);
            }

            UpdateUI();
        }

        private void ReturnTool()
        {
            if (currentTool == null) return;

            if (DrawingSystemManager.Instance != null)
            {
                DrawingSystemManager.Instance.UnregisterTool(currentTool);
            }

            Destroy(currentTool.gameObject);
            currentTool = null;

            if (returnClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(returnClip);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            bool hasTool = currentTool != null;

            if (spawnPenButton != null)
                spawnPenButton.interactable = !hasTool;
            
            if (spawnEraserButton != null)
                spawnEraserButton.interactable = !hasTool;
            
            if (returnToolButton != null)
                returnToolButton.interactable = hasTool;

            if (toolInfoText != null)
            {
                if (hasTool)
                {
                    toolInfoText.text = $"Current: {currentTool.Type}";
                }
                else
                {
                    toolInfoText.text = "No Tool";
                }
            }
        }
    }
}
