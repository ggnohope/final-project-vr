using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lobby
{
    /// <summary>
    /// Attach this to the LoginPanel (or any panel that has a TMP_InputField).
    /// Shows the VRKeyboard when the watched input field is selected and hides it
    /// when focus moves outside of both the input field and the keyboard canvas.
    /// </summary>
    public class VRKeyboardTrigger : MonoBehaviour
    {
        private const float HideCheckDelaySeconds = 0.08f;

        [Header("Input Field")]
        [SerializeField] private TMP_InputField watchedInputField;

        [Header("Keyboard")]
        [SerializeField] private VRKeyboard vrKeyboard;

        /// <summary>Root GameObject of the keyboard canvas – used to detect if focus moved into a key button.</summary>
        [SerializeField] private GameObject keyboardCanvasRoot;

        private bool isHidePending = false;

        // ─────────────────────────────────────────────────────────────
        #region Lifecycle

        private void Awake()
        {
            if (watchedInputField == null)
            {
                Debug.LogError("[VRKeyboardTrigger] watchedInputField is not assigned.");
                return;
            }

            if (vrKeyboard == null)
            {
                Debug.LogError("[VRKeyboardTrigger] vrKeyboard is not assigned.");
                return;
            }

            // Infer the keyboard canvas root from the VRKeyboard component if not set explicitly.
            if (keyboardCanvasRoot == null)
                keyboardCanvasRoot = vrKeyboard.gameObject;

            watchedInputField.onSelect.AddListener(OnInputFieldSelected);
            watchedInputField.onDeselect.AddListener(OnInputFieldDeselected);
        }

        private void OnDestroy()
        {
            if (watchedInputField == null) return;
            watchedInputField.onSelect.RemoveListener(OnInputFieldSelected);
            watchedInputField.onDeselect.RemoveListener(OnInputFieldDeselected);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Event Handlers

        /// <summary>Called when the watched input field gains focus.</summary>
        private void OnInputFieldSelected(string currentText)
        {
            isHidePending = false;
            vrKeyboard.Show(watchedInputField);
        }

        /// <summary>
        /// Called when the watched input field loses focus. Schedules a delayed hide
        /// so that clicking a keyboard key (which briefly moves focus) doesn't close it.
        /// </summary>
        private void OnInputFieldDeselected(string currentText)
        {
            if (!isHidePending)
                StartCoroutine(DelayedHideCheck());
        }

        private IEnumerator DelayedHideCheck()
        {
            isHidePending = true;
            yield return new WaitForSeconds(HideCheckDelaySeconds);
            isHidePending = false;

            // If the currently selected object belongs to the keyboard canvas, keep keyboard open.
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            if (selected != null && IsChildOfKeyboard(selected.transform))
                yield break;

            vrKeyboard.Hide();
        }

        /// <summary>Returns true if <paramref name="t"/> is the keyboard root or a descendant of it.</summary>
        private bool IsChildOfKeyboard(Transform t)
        {
            Transform root = keyboardCanvasRoot.transform;
            while (t != null)
            {
                if (t == root) return true;
                t = t.parent;
            }
            return false;
        }

        #endregion
    }
}

