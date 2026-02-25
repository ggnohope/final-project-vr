using UnityEngine;
using VRDrawing;
using VRDrawing.Tools;
using VRDrawing.Rendering;

namespace VRDrawing.Setup
{
    public class RuntimeDrawingSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private bool createSystemManager = true;
        [SerializeField] private bool setupExistingBoards = true;
        [SerializeField] private bool convertLegacyPens = true;

        [Header("Board Setup")]
        [SerializeField] private Vector2 defaultSurfaceSize = new Vector2(0.4f, 0.3f);
        [SerializeField] private Material strokeMaterial;

        private void Awake()
        {
            if (setupOnAwake)
            {
                PerformSetup();
            }
        }

        [ContextMenu("Perform Drawing System Setup")]
        public void PerformSetup()
        {
            if (createSystemManager)
            {
                SetupSystemManager();
            }

            if (setupExistingBoards)
            {
                SetupAllBoards();
            }

            if (convertLegacyPens)
            {
                ConvertAllPens();
            }

            Debug.Log("VR Drawing System setup complete!");
        }

        private void SetupSystemManager()
        {
            if (DrawingSystemManager.Instance == null)
            {
                GameObject managerObj = new GameObject("DrawingSystemManager");
                managerObj.AddComponent<DrawingSystemManager>();
                Debug.Log("Created DrawingSystemManager");
            }
        }

        private void SetupAllBoards()
        {
            DrawingSurface[] surfaces = FindObjectsByType<DrawingSurface>(FindObjectsSortMode.None);

            foreach (var surface in surfaces)
            {
                SetupBoardDrawingSurface(surface.gameObject);
            }

            Debug.Log($"[RuntimeDrawingSetup] Setup {surfaces.Length} drawing surfaces");
        }

        private void SetupBoardDrawingSurface(GameObject board)
        {
            Transform surfaceTransform = board.transform.Find("DrawingSurface");
            
            if (surfaceTransform == null)
            {
                GameObject surfaceObj = new GameObject("DrawingSurface");
                surfaceObj.transform.SetParent(board.transform);
                surfaceObj.transform.localPosition = new Vector3(0f, 0f, 0.051f);
                surfaceObj.transform.localRotation = Quaternion.identity;
                surfaceObj.transform.localScale = Vector3.one;
                surfaceTransform = surfaceObj.transform;
            }

            DrawingSurface surface = surfaceTransform.GetComponent<DrawingSurface>();
            if (surface == null)
            {
                surface = surfaceTransform.gameObject.AddComponent<DrawingSurface>();
            }

            MeshStrokeRenderer renderer = surfaceTransform.GetComponent<MeshStrokeRenderer>();
            if (renderer == null)
            {
                renderer = surfaceTransform.gameObject.AddComponent<MeshStrokeRenderer>();
            }

            BoxCollider collider = surfaceTransform.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = surfaceTransform.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(defaultSurfaceSize.x, defaultSurfaceSize.y, 0.02f);
            }

            int drawingSurfaceLayer = LayerMask.NameToLayer("Drawing Surface");
            if (drawingSurfaceLayer != -1)
            {
                surfaceTransform.gameObject.layer = drawingSurfaceLayer;
            }

            DrawingBoardBridge bridge = board.GetComponent<DrawingBoardBridge>();
            if (bridge == null)
            {
                bridge = board.AddComponent<DrawingBoardBridge>();
            }

            if (DrawingSystemManager.Instance != null)
            {
                DrawingSystemManager.Instance.RegisterSurface(surface);
            }
        }

        private void ConvertAllPens()
        {
            PenController[] legacyPens = FindObjectsByType<PenController>(FindObjectsSortMode.None);

            foreach (var legacyPen in legacyPens)
            {
                ConvertPenTool(legacyPen.gameObject);
            }

            Debug.Log($"Converted {legacyPens.Length} legacy pens to new system");
        }

        private void ConvertPenTool(GameObject penObj)
        {
            PenTool penTool = penObj.GetComponent<PenTool>();
            if (penTool == null)
            {
                penTool = penObj.AddComponent<PenTool>();
            }

            Transform tipTransform = penObj.transform.Find("ToolTip");
            if (tipTransform == null)
            {
                tipTransform = penObj.transform.Find("PenTip");
            }

            if (tipTransform == null)
            {
                GameObject tipObj = new GameObject("ToolTip");
                tipObj.transform.SetParent(penObj.transform);
                tipObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                tipObj.transform.localRotation = Quaternion.identity;
                tipTransform = tipObj.transform;
            }
            else
            {
                tipTransform.name = "ToolTip";
            }

            ToolTipCollisionDetector detector = tipTransform.GetComponent<ToolTipCollisionDetector>();
            if (detector == null)
            {
                detector = tipTransform.gameObject.AddComponent<ToolTipCollisionDetector>();
            }

            SphereCollider tipCollider = tipTransform.GetComponent<SphereCollider>();
            if (tipCollider == null)
            {
                tipCollider = tipTransform.gameObject.AddComponent<SphereCollider>();
                tipCollider.radius = 0.005f;
                tipCollider.isTrigger = true;
            }

            if (DrawingSystemManager.Instance != null)
            {
                DrawingSystemManager.Instance.RegisterTool(penTool);
            }

            PenController legacyPen = penObj.GetComponent<PenController>();
            if (legacyPen != null)
            {
                legacyPen.enabled = false;
            }
        }
    }
}
