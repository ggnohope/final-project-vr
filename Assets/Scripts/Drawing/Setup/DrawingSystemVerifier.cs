using UnityEngine;
using VRDrawing;
using VRDrawing.Rendering;

namespace VRDrawing.Setup
{
    public class DrawingSystemVerifier : MonoBehaviour
    {
        [Header("Verification Results")]
        [SerializeField] private bool systemManagerFound;
        [SerializeField] private bool surfacesFound;
        [SerializeField] private bool toolsFound;
        [SerializeField] private int totalSurfaces;
        [SerializeField] private int totalTools;

        [ContextMenu("Verify Drawing System")]
        public void VerifySystem()
        {
            Debug.Log("=== VR Drawing System Verification ===");

            systemManagerFound = DrawingSystemManager.Instance != null;
            Debug.Log($"✓ DrawingSystemManager: {(systemManagerFound ? "FOUND" : "MISSING")}");

            if (systemManagerFound)
            {
                totalSurfaces = DrawingSystemManager.Instance.Surfaces.Count;
                totalTools = DrawingSystemManager.Instance.Tools.Count;

                Debug.Log($"  - Registered Surfaces: {totalSurfaces}");
                Debug.Log($"  - Registered Tools: {totalTools}");
            }

            DrawingSurface[] surfaces = FindObjectsByType<DrawingSurface>(FindObjectsSortMode.None);
            surfacesFound = surfaces.Length > 0;
            Debug.Log($"✓ DrawingSurfaces in scene: {surfaces.Length}");

            foreach (var surface in surfaces)
            {
                MeshStrokeRenderer renderer = surface.GetComponent<MeshStrokeRenderer>();
                BoxCollider collider = surface.GetComponent<BoxCollider>();
                
                Debug.Log($"  - {surface.gameObject.name}:");
                Debug.Log($"    Renderer: {(renderer != null ? "✓" : "✗")}");
                Debug.Log($"    Collider: {(collider != null ? "✓" : "✗")}");
                Debug.Log($"    Trigger: {(collider != null && collider.isTrigger ? "✓" : "✗")}");
            }

            DrawingBoardBridge[] bridges = FindObjectsByType<DrawingBoardBridge>(FindObjectsSortMode.None);
            Debug.Log($"✓ DrawingBoardBridges: {bridges.Length}");

            RuntimeDrawingSetup setup = FindFirstObjectByType<RuntimeDrawingSetup>();
            if (setup != null)
            {
                Debug.Log($"✓ RuntimeDrawingSetup found:");
                Debug.Log($"  - Setup on Awake: {setup.GetType().GetField("setupOnAwake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(setup)}");
            }

            Debug.Log("=== Verification Complete ===");
            
            if (systemManagerFound && surfacesFound)
            {
                Debug.Log("✅ SYSTEM READY - Enter Play Mode to test!");
            }
            else
            {
                Debug.LogWarning("⚠️ System incomplete - Check missing components above");
            }
        }

        private void Start()
        {
            VerifySystem();
        }
    }
}
