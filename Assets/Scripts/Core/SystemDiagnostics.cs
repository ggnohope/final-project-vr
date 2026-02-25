using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Core
{
    public class SystemDiagnostics : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas worldMapCanvas;
        [SerializeField] private DynamicMoveProvider moveProvider;
        [SerializeField] private SnapTurnProvider turnProvider;
        [SerializeField] private GsplatSceneLoader sceneLoader;

        [ContextMenu("Run Full Diagnostics")]
        public void RunDiagnostics()
        {
            Debug.Log("=== SYSTEM DIAGNOSTICS ===");
            
            // Canvas
            if (worldMapCanvas != null)
            {
                Debug.Log($"WorldMapCanvas Active: {worldMapCanvas.gameObject.activeSelf}");
                Debug.Log($"WorldMapCanvas Enabled: {worldMapCanvas.enabled}");
                Debug.Log($"WorldMapCanvas Position: {worldMapCanvas.transform.position}");
                Debug.Log($"WorldMapCanvas LocalPosition: {worldMapCanvas.transform.localPosition}");
                Debug.Log($"WorldMapCanvas LocalScale: {worldMapCanvas.transform.localScale}");
            }
            else
            {
                Debug.LogError("WorldMapCanvas reference is NULL!");
            }

            // Locomotion
            if (moveProvider != null)
            {
                Debug.Log($"MoveProvider Active: {moveProvider.gameObject.activeSelf}");
                Debug.Log($"MoveProvider Enabled: {moveProvider.enabled}");
            }
            else
            {
                Debug.LogError("MoveProvider reference is NULL!");
            }

            if (turnProvider != null)
            {
                Debug.Log($"TurnProvider Active: {turnProvider.gameObject.activeSelf}");
                Debug.Log($"TurnProvider Enabled: {turnProvider.enabled}");
            }
            else
            {
                Debug.LogError("TurnProvider reference is NULL!");
            }

            // Scene Loader
            if (sceneLoader != null)
            {
                Debug.Log($"SceneLoader IsLoading: {sceneLoader.IsLoading}");
            }
            else
            {
                Debug.LogError("SceneLoader reference is NULL!");
            }

            Debug.Log("=== END DIAGNOSTICS ===");
        }

        [ContextMenu("Force Enable Canvas")]
        public void ForceEnableCanvas()
        {
            if (worldMapCanvas != null)
            {
                worldMapCanvas.gameObject.SetActive(true);
                Debug.Log("[Diagnostics] WorldMapCanvas ENABLED");
            }
        }

        [ContextMenu("Force Enable Locomotion")]
        public void ForceEnableLocomotion()
        {
            if (moveProvider != null)
            {
                moveProvider.enabled = true;
                Debug.Log("[Diagnostics] MoveProvider ENABLED");
            }

            if (turnProvider != null)
            {
                turnProvider.enabled = true;
                Debug.Log("[Diagnostics] TurnProvider ENABLED");
            }
        }

        [ContextMenu("Force Disable Locomotion")]
        public void ForceDisableLocomotion()
        {
            if (moveProvider != null)
            {
                moveProvider.enabled = false;
                Debug.Log("[Diagnostics] MoveProvider DISABLED");
            }

            if (turnProvider != null)
            {
                turnProvider.enabled = false;
                Debug.Log("[Diagnostics] TurnProvider DISABLED");
            }
        }
    }
}
