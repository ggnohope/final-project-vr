using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    /// <summary>
    /// A world-space VR keyboard that types into a target TMP_InputField.
    /// Interaction is done via the right controller ray interactor clicking UI buttons.
    /// </summary>
    public class VRKeyboard : MonoBehaviour
    {
        private const string BackspaceSymbol = "⌫";
        private const string SpaceSymbol = "SPACE";
        private const string ClearSymbol = "CLR";
        private const string DoneSymbol = "OK";
        private const string CapsSymbol = "⇧";

        private static readonly string[] KeyRows = new[]
        {
            "QWERTYUIOP",
            "ASDFGHJKL",
            "ZXCVBNM"
        };

        [Header("References")]
        [SerializeField] private Transform keyContainer;
        [SerializeField] private GameObject keyButtonPrefab;
        [SerializeField] private TMP_Text previewText;

        [Header("Layout")]
        [SerializeField] private float keySize = 60f;
        [SerializeField] private float keySpacing = 8f;

        private TMP_InputField targetInputField;
        private bool isCapsOn = false;

        // ─────────────────────────────────────────────────────────────
        #region Lifecycle

        private void Awake()
        {
            BuildKeyboard();
            gameObject.SetActive(false);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Public API

        private static readonly Vector3 FixedWorldPosition = new Vector3(0f, 0.68f, 2f);

        /// <summary>Shows the keyboard at the fixed world position and targets the given input field.</summary>
        public void Show(TMP_InputField inputField)
        {
            targetInputField = inputField;
            transform.position = FixedWorldPosition;
            RefreshPreview();
            gameObject.SetActive(true);
        }

        /// <summary>Hides the keyboard and clears the target.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            targetInputField = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Key Building

        private void BuildKeyboard()
        {
            if (keyContainer == null || keyButtonPrefab == null)
            {
                Debug.LogError("[VRKeyboard] keyContainer or keyButtonPrefab is not assigned.");
                return;
            }

            float step = keySize + keySpacing;

            // Total rows = letter rows + 1 bottom row.
            // Vertically center the whole key block inside the KeyContainer
            // so that the block starts at +halfBlock and ends at -halfBlock.
            int totalRows = KeyRows.Length + 1;                         // 4
            float blockHeight = totalRows * step - keySpacing;          // 4*68-8 = 264
            float topY = (blockHeight - keySize) / 2f;                  // push first row to top of block

            // Letter rows
            for (int row = 0; row < KeyRows.Length; row++)
            {
                string rowChars = KeyRows[row];
                float totalWidth = rowChars.Length * step - keySpacing;
                float startX = -totalWidth / 2f + keySize / 2f;
                float y = topY - row * step;

                for (int col = 0; col < rowChars.Length; col++)
                {
                    string letter = rowChars[col].ToString();
                    float x = startX + col * step;
                    CreateKey(letter, new Vector2(x, y), keySize, keySize, () => OnLetterKey(letter));
                }
            }

            // Bottom row: Caps, Space, Backspace, Clear, Done
            float bottomY = topY - KeyRows.Length * step;
            float bottomKeyW = keySize * 1.4f;

            CreateKey(CapsSymbol,      new Vector2(-260f, bottomY), bottomKeyW, keySize, OnCapsKey);
            CreateKey(SpaceSymbol,     new Vector2(-70f,  bottomY), keySize * 4.5f, keySize, OnSpaceKey);
            CreateKey(BackspaceSymbol, new Vector2(110f,  bottomY), bottomKeyW, keySize, OnBackspaceKey);
            CreateKey(ClearSymbol,     new Vector2(195f,  bottomY), bottomKeyW, keySize, OnClearKey);
            CreateKey(DoneSymbol,      new Vector2(280f,  bottomY), bottomKeyW, keySize, OnDoneKey);
        }

        private void CreateKey(string label, Vector2 anchoredPos, float width, float height, Action onClick)
        {
            GameObject keyGO = Instantiate(keyButtonPrefab, keyContainer);
            keyGO.name = $"Key_{label}";

            RectTransform rt = keyGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = new Vector2(width, height);
            }

            TMP_Text keyLabel = keyGO.GetComponentInChildren<TMP_Text>();
            if (keyLabel != null)
                keyLabel.text = label;

            Button btn = keyGO.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => onClick());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Key Handlers

        private void OnLetterKey(string letter)
        {
            if (targetInputField == null) return;
            string charToInsert = isCapsOn ? letter.ToUpper() : letter.ToLower();
            InsertText(charToInsert);
        }

        private void OnSpaceKey()
        {
            InsertText(" ");
        }

        private void OnBackspaceKey()
        {
            if (targetInputField == null) return;
            string current = targetInputField.text;
            if (current.Length > 0)
            {
                targetInputField.text = current[..^1];
                RefreshPreview();
            }
        }

        private void OnClearKey()
        {
            if (targetInputField == null) return;
            targetInputField.text = string.Empty;
            RefreshPreview();
        }

        private void OnCapsKey()
        {
            isCapsOn = !isCapsOn;
            RefreshCapsVisuals();
        }

        private void OnDoneKey()
        {
            Hide();
        }

        private void InsertText(string chars)
        {
            if (targetInputField == null) return;
            targetInputField.text += chars;
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (previewText == null) return;
            previewText.text = targetInputField != null ? targetInputField.text : string.Empty;
        }

        private void RefreshCapsVisuals()
        {
            // Find the Caps key button and tint it to indicate state
            Transform capsKey = keyContainer.Find("Key_⇧");
            if (capsKey == null) return;
            Button btn = capsKey.GetComponent<Button>();
            if (btn == null) return;
            ColorBlock colors = btn.colors;
            colors.normalColor = isCapsOn ? new Color(0.4f, 0.7f, 1f) : Color.white;
            btn.colors = colors;
        }

        #endregion
    }
}
