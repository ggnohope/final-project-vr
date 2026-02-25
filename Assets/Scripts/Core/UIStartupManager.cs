using UnityEngine;

namespace Core
{
    public class UIStartupManager : MonoBehaviour
    {
        [Header("Canvases to Disable on Startup")]
        [SerializeField] private GameObject[] canvasesToDisable;

        [Header("Canvases to Keep Active")]
        [SerializeField] private GameObject[] canvasesToKeepActive;

        private void Awake()
        {
            DisableNonEssentialCanvases();
        }

        private void DisableNonEssentialCanvases()
        {
            if (canvasesToDisable == null || canvasesToDisable.Length == 0)
            {
                return;
            }

            foreach (var canvas in canvasesToDisable)
            {
                if (canvas != null && canvas.activeSelf)
                {
                    canvas.SetActive(false);
                }
            }
        }

        [ContextMenu("Find All Canvases")]
        public void FindAllCanvases()
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log($"[UIStartup] Found {allCanvases.Length} canvases in scene");
            
            foreach (var canvas in allCanvases)
            {
                Debug.Log($"  - {canvas.gameObject.name} (Active: {canvas.gameObject.activeSelf})");
            }
        }
    }
}
