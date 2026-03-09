using UnityEngine;
using VRDrawing.Data;
using VRDrawing.Features;

namespace VRDrawing.Setup
{
    /// <summary>
    /// Placed on the DrawingBoard prefab root.
    /// On Start, finds the DrawingSurface sibling and wires the SymbolLayerManager to it.
    /// Also propagates a captured image to the BaseImage layer via the CaptureToBoard hook.
    /// </summary>
    public class AnnotationBoardSetup : MonoBehaviour
    {
        [Header("Optional: pre-assign surface")]
        [SerializeField] private DrawingSurface targetSurface;

        private void Start()
        {
            if (targetSurface == null)
                targetSurface = GetComponentInChildren<DrawingSurface>();

            if (targetSurface == null)
            {
                Debug.LogWarning("[AnnotationBoardSetup] No DrawingSurface found on this board.");
                return;
            }

            // Attach SymbolLayerManager to this board
            if (SymbolLayerManager.Instance != null)
            {
                SymbolLayerManager.Instance.AttachToSurface(targetSurface);
                Debug.Log("[AnnotationBoardSetup] SymbolLayerManager attached to surface.");
            }
            else
            {
                Debug.LogWarning("[AnnotationBoardSetup] SymbolLayerManager.Instance not found in scene.");
            }

            // If a photo was captured before the board spawned, apply it now
            if (PendingCapturePayload.PendingTexture != null)
            {
                VRDrawing.Photo.PhotoPlacementManager.Instance?.EnterPlacementMode(
                    PendingCapturePayload.PendingTexture);
                PendingCapturePayload.Clear();
            }
        }
    }

    /// <summary>
    /// Static payload that carries a captured Texture2D across the board-spawn boundary.
    /// </summary>
    public static class PendingCapturePayload
    {
        public static Texture2D PendingTexture { get; private set; }

        /// <summary>Sets the pending texture so the next spawned board can pick it up.</summary>
        public static void Set(Texture2D tex) => PendingTexture = tex;

        public static void Clear() => PendingTexture = null;
    }
}
