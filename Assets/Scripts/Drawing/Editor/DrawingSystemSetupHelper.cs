using UnityEngine;
using UnityEditor;
using VRDrawing;
using VRDrawing.Tools;
using VRDrawing.Rendering;

namespace VRDrawing.Editor
{
    public class DrawingSystemSetupHelper : EditorWindow
    {
        [MenuItem("VR Drawing/Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<DrawingSystemSetupHelper>("Drawing Setup");
        }

        private GameObject selectedBoard;
        private GameObject selectedPen;

        private void OnGUI()
        {
            GUILayout.Label("VR Drawing System Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("This tool helps you set up drawing boards and pen tools.", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Drawing System Manager", GUILayout.Height(40)))
            {
                CreateSystemManager();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Board Setup", EditorStyles.boldLabel);
            
            selectedBoard = (GameObject)EditorGUILayout.ObjectField("Board Prefab/Object", selectedBoard, typeof(GameObject), true);

            if (selectedBoard != null && GUILayout.Button("Setup Drawing Surface on Board", GUILayout.Height(30)))
            {
                SetupDrawingBoard(selectedBoard);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Pen Tool Setup", EditorStyles.boldLabel);
            
            selectedPen = (GameObject)EditorGUILayout.ObjectField("Pen Prefab/Object", selectedPen, typeof(GameObject), true);

            if (selectedPen != null && GUILayout.Button("Setup Pen Tool", GUILayout.Height(30)))
            {
                SetupPenTool(selectedPen);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create New Pen Prefab", GUILayout.Height(30)))
            {
                CreateNewPenPrefab();
            }

            if (GUILayout.Button("Create New Eraser Prefab", GUILayout.Height(30)))
            {
                CreateNewEraserPrefab();
            }
        }

        private void CreateSystemManager()
        {
            GameObject manager = new GameObject("DrawingSystemManager");
            manager.AddComponent<DrawingSystemManager>();
            EditorGUIUtility.PingObject(manager);
        }

        private void SetupDrawingBoard(GameObject board)
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
            }
            collider.isTrigger = true;
            collider.size = new Vector3(0.4f, 0.3f, 0.02f);

            int drawingSurfaceLayer = LayerMask.NameToLayer("Drawing Surface");
            if (drawingSurfaceLayer != -1)
            {
                surfaceTransform.gameObject.layer = drawingSurfaceLayer;
            }

            EditorGUIUtility.PingObject(surfaceTransform.gameObject);
        }

        private void SetupPenTool(GameObject pen)
        {
            PenTool penTool = pen.GetComponent<PenTool>();
            if (penTool == null)
            {
                penTool = pen.AddComponent<PenTool>();
            }

            Transform tipTransform = pen.transform.Find("ToolTip");
            if (tipTransform == null)
            {
                GameObject tipObj = new GameObject("ToolTip");
                tipObj.transform.SetParent(pen.transform);
                tipObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                tipObj.transform.localRotation = Quaternion.identity;
                tipTransform = tipObj.transform;
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
            }
            tipCollider.radius = 0.005f;
            tipCollider.isTrigger = true;

            EditorGUIUtility.PingObject(pen);
        }

        private void CreateNewPenPrefab()
        {
            GameObject pen = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pen.name = "PenTool";
            pen.transform.localScale = new Vector3(0.01f, 0.05f, 0.01f);

            DestroyImmediate(pen.GetComponent<Collider>());

            CapsuleCollider grabCollider = pen.AddComponent<CapsuleCollider>();
            grabCollider.radius = 1f;
            grabCollider.height = 2f;
            grabCollider.isTrigger = false;

            Rigidbody rb = pen.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var grabInteractable = pen.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            SetupPenTool(pen);

            PrefabUtility.SaveAsPrefabAsset(pen, "Assets/Prefabs/PenTool.prefab");
            EditorGUIUtility.PingObject(pen);
        }

        private void CreateNewEraserPrefab()
        {
            GameObject eraser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eraser.name = "EraserTool";
            eraser.transform.localScale = new Vector3(0.02f, 0.05f, 0.02f);

            DestroyImmediate(eraser.GetComponent<Collider>());

            BoxCollider grabCollider = eraser.AddComponent<BoxCollider>();
            grabCollider.size = Vector3.one;
            grabCollider.isTrigger = false;

            Rigidbody rb = eraser.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var grabInteractable = eraser.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            EraserTool eraserTool = eraser.AddComponent<EraserTool>();

            Transform tipTransform = new GameObject("ToolTip").transform;
            tipTransform.SetParent(eraser.transform);
            tipTransform.localPosition = new Vector3(0f, -0.025f, 0f);
            
            ToolTipCollisionDetector detector = tipTransform.gameObject.AddComponent<ToolTipCollisionDetector>();
            
            SphereCollider tipCollider = tipTransform.gameObject.AddComponent<SphereCollider>();
            tipCollider.radius = 0.01f;
            tipCollider.isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(eraser, "Assets/Prefabs/EraserTool.prefab");
            EditorGUIUtility.PingObject(eraser);
        }
    }
}
