using UnityEngine;

namespace VRItems.Camera
{
    public class CameraModeActivator : MonoBehaviour
    {
        private void Start()
        {
            ActivateCameraMode();
        }
        
        public void ActivateCameraMode()
        {
            if (CameraModeManager.Instance != null)
            {
                CameraModeManager.Instance.EnterCameraMode();
                Debug.Log("[CameraModeActivator] Activating Camera Mode");
            }
            else
            {
                Debug.LogError("[CameraModeActivator] CameraModeManager.Instance is null!");
            }
        }
        
        private void OnDestroy()
        {
            if (CameraModeManager.Instance != null)
            {
                CameraModeManager.Instance.ExitCameraMode();
            }
        }
    }
}
