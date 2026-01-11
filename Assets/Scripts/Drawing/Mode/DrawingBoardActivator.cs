using UnityEngine;

namespace VRDrawing.Mode
{
    public class DrawingBoardActivator : MonoBehaviour
    {
        [Header("Auto Activate")]
        [SerializeField] private bool autoActivateOnSpawn = true;

        private void Start()
        {
            Debug.Log("DrawingBoardActivator Start() called");
            if (autoActivateOnSpawn)
            {
                ActivateDrawingMode();
            }
        }

        public void ActivateDrawingMode()
        {
            Debug.Log("DrawingBoardActivator: Attempting to activate drawing mode");
            
            if (DrawingModeManager.Instance != null)
            {
                Debug.Log("DrawingBoardActivator: DrawingModeManager found, calling EnterDrawingMode()");
                DrawingModeManager.Instance.EnterDrawingMode();
            }
            else
            {
                Debug.LogError("DrawingBoardActivator: DrawingModeManager instance not found");
            }

            Destroy(gameObject);
        }

        public void DeactivateDrawingMode()
        {
            if (DrawingModeManager.Instance != null)
            {
                DrawingModeManager.Instance.ExitDrawingMode();
            }
        }
    }
}
