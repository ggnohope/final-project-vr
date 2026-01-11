using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRDrawing.Mode;
using VRDrawing.Tools;
using VRDrawing.UI;
using System.IO;
using System.Linq;

namespace VRDrawing.Editor
{
    public class VRDrawingAutoSetup : EditorWindow
    {
        private const string PREFAB_DIR = "Assets/Prefabs/Drawing";
        private const string MATERIAL_DIR = "Assets/Materials/Drawing";
        private const string DRAWING_SURFACE_LAYER = "Drawing Surface";
        private const int DRAWING_SURFACE_LAYER_INDEX = 3;

        private Vector2 scrollPosition;
        private bool setupComplete = false;

        [MenuItem("Tools/VR Drawing/Auto Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<VRDrawingAutoSetup>("VR Drawing Setup");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("VR Drawing System Auto Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool will automatically create and configure all required prefabs, materials, and scene objects for the VR Drawing System.", MessageType.Info);

            GUILayout.Space(20);

            if (!ValidateXRToolkit())
            {
                EditorGUILayout.HelpBox("XR Interaction Toolkit not found! Please install com.unity.xr.interaction.toolkit version 3.0 or higher.", MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("Prerequisites", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("✓ XR Interaction Toolkit 3.2.1", EditorStyles.helpBox);
            EditorGUILayout.LabelField("✓ Input System 1.14.2", EditorStyles.helpBox);

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Setup Steps", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Setup Layers", GUILayout.Height(40)))
            {
                SetupLayers();
            }

            if (GUILayout.Button("2. Create Materials", GUILayout.Height(40)))
            {
                CreateMaterials();
            }

            if (GUILayout.Button("3. Create Drawing Board Prefab", GUILayout.Height(40)))
            {
                CreateDrawingBoardPrefab();
            }

            if (GUILayout.Button("4. Create Tool Panel Prefab", GUILayout.Height(40)))
            {
                CreateToolPanelPrefab();
            }

            if (GUILayout.Button("5. Create Drawing Board Activator Prefab", GUILayout.Height(40)))
            {
                CreateDrawingBoardActivatorPrefab();
            }

            if (GUILayout.Button("6. Create Pen Prefab", GUILayout.Height(40)))
            {
                CreatePenPrefab();
            }

            if (GUILayout.Button("7. Setup Scene (DrawingModeManager)", GUILayout.Height(40)))
            {
                SetupScene();
            }

            GUILayout.Space(20);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("⚡ RUN COMPLETE SETUP ⚡", GUILayout.Height(60)))
            {
                RunCompleteSetup();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(20);

            if (setupComplete)
            {
                EditorGUILayout.HelpBox("✓ Setup completed successfully! Check the Console for details.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool ValidateXRToolkit()
        {
            var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Unity.XR.Interaction.Toolkit");
            return assembly != null;
        }

        private void RunCompleteSetup()
        {
            Debug.Log("=== VR Drawing System Auto Setup Started ===");

            SetupLayers();
            CreateMaterials();
            CreateDrawingBoardPrefab();
            CreateToolPanelPrefab();
            CreateDrawingBoardActivatorPrefab();
            CreatePenPrefab();
            SetupScene();

            setupComplete = true;
            Debug.Log("=== VR Drawing System Auto Setup Completed ===");
            EditorUtility.DisplayDialog("Setup Complete", "VR Drawing System has been set up successfully!", "OK");
        }

        private void SetupLayers()
        {
            Debug.Log("Setting up layers...");

            if (!LayerExists(DRAWING_SURFACE_LAYER))
            {
                CreateLayer(DRAWING_SURFACE_LAYER, DRAWING_SURFACE_LAYER_INDEX);
                Debug.Log($"✓ Created layer: {DRAWING_SURFACE_LAYER} at index {DRAWING_SURFACE_LAYER_INDEX}");
            }
            else
            {
                Debug.Log($"✓ Layer already exists: {DRAWING_SURFACE_LAYER}");
            }
        }

        private bool LayerExists(string layerName)
        {
            for (int i = 0; i < 32; i++)
            {
                string layer = LayerMask.LayerToName(i);
                if (layer == layerName)
                    return true;
            }
            return false;
        }

        private void CreateLayer(string layerName, int layerIndex)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            if (layerIndex >= 0 && layerIndex < layers.arraySize)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(layerIndex);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    layerSP.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                }
            }
        }

        private void CreateMaterials()
        {
            Debug.Log("Creating materials...");

            if (!Directory.Exists(MATERIAL_DIR))
            {
                Directory.CreateDirectory(MATERIAL_DIR);
                AssetDatabase.Refresh();
            }

            string canvasMatPath = $"{MATERIAL_DIR}/DrawingCanvas.mat";
            if (!File.Exists(canvasMatPath))
            {
                Material canvasMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                canvasMat.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(canvasMat, canvasMatPath);
                Debug.Log($"✓ Created material: {canvasMatPath}");
            }

            string penMatPath = $"{MATERIAL_DIR}/PenMaterial.mat";
            if (!File.Exists(penMatPath))
            {
                Material penMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                penMat.SetColor("_BaseColor", new Color(0.2f, 0.4f, 0.8f));
                AssetDatabase.CreateAsset(penMat, penMatPath);
                Debug.Log($"✓ Created material: {penMatPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateDrawingBoardPrefab()
        {
            Debug.Log("Creating Drawing Board Prefab...");

            if (!Directory.Exists(PREFAB_DIR))
            {
                Directory.CreateDirectory(PREFAB_DIR);
                AssetDatabase.Refresh();
            }

            string prefabPath = $"{PREFAB_DIR}/DrawingBoard.prefab";

            GameObject board = new GameObject("DrawingBoard");

            GameObject surfaceQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surfaceQuad.name = "Canvas";
            surfaceQuad.transform.SetParent(board.transform);
            surfaceQuad.transform.localPosition = Vector3.zero;
            surfaceQuad.transform.localRotation = Quaternion.identity;
            surfaceQuad.transform.localScale = Vector3.one;

            DestroyImmediate(surfaceQuad.GetComponent<MeshCollider>());

            Material canvasMat = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIAL_DIR}/DrawingCanvas.mat");
            if (canvasMat != null)
            {
                surfaceQuad.GetComponent<MeshRenderer>().material = canvasMat;
            }

            GameObject surfaceObj = new GameObject("DrawingSurface");
            surfaceObj.transform.SetParent(board.transform);
            surfaceObj.transform.localPosition = new Vector3(0, 0, -0.01f);
            surfaceObj.transform.localRotation = Quaternion.identity;
            surfaceObj.transform.localScale = Vector3.one;

            BoxCollider surfaceCollider = surfaceObj.AddComponent<BoxCollider>();
            surfaceCollider.size = new Vector3(0.95f, 0.95f, 0.02f);
            surfaceCollider.isTrigger = true;

            surfaceObj.layer = LayerMask.NameToLayer(DRAWING_SURFACE_LAYER);

            DrawingSurface drawingSurface = surfaceObj.AddComponent<DrawingSurface>();

            VRDrawing.Rendering.MeshStrokeRenderer renderer = surfaceObj.AddComponent<VRDrawing.Rendering.MeshStrokeRenderer>();

            DrawingBoardActivator activator = board.AddComponent<DrawingBoardActivator>();

            board.transform.localScale = new Vector3(1f, 0.7f, 0.1f);

            PrefabUtility.SaveAsPrefabAsset(board, prefabPath);
            DestroyImmediate(board);

            Debug.Log($"✓ Created prefab: {prefabPath}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateToolPanelPrefab()
        {
            Debug.Log("Creating Tool Panel Prefab...");

            string prefabPath = $"{PREFAB_DIR}/ToolPanel.prefab";

            GameObject canvasObj = new GameObject("ToolPanel");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400, 300);
            canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

            GameObject bgPanel = new GameObject("Background");
            bgPanel.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image bgImage = bgPanel.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject penBtn = CreateButton("PenButton", "Pen", new Vector2(0, 100), canvasObj.transform);
            GameObject eraserBtn = CreateButton("EraserButton", "Eraser", new Vector2(0, 50), canvasObj.transform);
            GameObject undoBtn = CreateButton("UndoButton", "Undo", new Vector2(0, 0), canvasObj.transform);
            GameObject clearBtn = CreateButton("ClearButton", "Clear", new Vector2(0, -50), canvasObj.transform);

            GameObject colorPanel = new GameObject("ColorPanel");
            colorPanel.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.GridLayoutGroup grid = colorPanel.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(30, 30);
            grid.spacing = new Vector2(5, 5);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            
            RectTransform colorRect = colorPanel.GetComponent<RectTransform>();
            colorRect.anchoredPosition = new Vector2(0, -120);
            colorRect.sizeDelta = new Vector2(300, 40);

            Color[] colors = new Color[]
            {
                Color.black, Color.white, Color.red, Color.green,
                Color.blue, Color.yellow, Color.cyan, Color.magenta
            };

            for (int i = 0; i < colors.Length; i++)
            {
                GameObject colorBtn = CreateColorButton($"Color{i}", colors[i], colorPanel.transform);
            }

            GameObject sliderObj = new GameObject("ThicknessSlider");
            sliderObj.transform.SetParent(canvasObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(0, -180);
            sliderRect.sizeDelta = new Vector2(300, 20);

            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0.002f;
            slider.maxValue = 0.02f;
            slider.value = 0.005f;

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            UnityEngine.UI.Image bgImg = bg.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = Color.gray;
            RectTransform bgSliderRect = bg.GetComponent<RectTransform>();
            bgSliderRect.anchorMin = Vector2.zero;
            bgSliderRect.anchorMax = Vector2.one;
            bgSliderRect.offsetMin = Vector2.zero;
            bgSliderRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            UnityEngine.UI.Image fillImg = fill.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = Color.blue;
            RectTransform fillRect = fill.GetComponent<RectTransform>();

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            UnityEngine.UI.Image handleImg = handle.AddComponent<UnityEngine.UI.Image>();
            handleImg.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            DrawingToolPanel toolPanel = canvasObj.AddComponent<DrawingToolPanel>();

            PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
            DestroyImmediate(canvasObj);

            Debug.Log($"✓ Created prefab: {prefabPath}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private GameObject CreateButton(string name, string label, Vector2 position, Transform parent)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150, 30);

            UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f);

            UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return btnObj;
        }

        private GameObject CreateColorButton(string name, Color color, Transform parent)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
            img.color = color;

            UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();

            return btnObj;
        }

        private void CreateDrawingBoardActivatorPrefab()
        {
            Debug.Log("Creating Drawing Board Activator Prefab...");

            string prefabPath = $"{PREFAB_DIR}/DrawingBoardActivator.prefab";

            GameObject activatorObj = new GameObject("DrawingBoardActivator");
            DrawingBoardActivator activator = activatorObj.AddComponent<DrawingBoardActivator>();

            PrefabUtility.SaveAsPrefabAsset(activatorObj, prefabPath);
            DestroyImmediate(activatorObj);

            Debug.Log($"✓ Created prefab: {prefabPath}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreatePenPrefab()
        {
            Debug.Log("Creating Pen Prefab...");

            string prefabPath = $"{PREFAB_DIR}/Pen.prefab";

            GameObject penObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            penObj.name = "Pen";
            penObj.transform.localScale = new Vector3(0.01f, 0.1f, 0.01f);

            DestroyImmediate(penObj.GetComponent<Collider>());

            CapsuleCollider collider = penObj.AddComponent<CapsuleCollider>();
            collider.radius = 0.5f;
            collider.height = 2f;
            collider.direction = 1;

            Rigidbody rb = penObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.mass = 0.1f;

            Material penMat = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIAL_DIR}/PenMaterial.mat");
            if (penMat != null)
            {
                penObj.GetComponent<MeshRenderer>().material = penMat;
            }

            var grabInteractable = penObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            GameObject tipObj = new GameObject("ToolTip");
            tipObj.transform.SetParent(penObj.transform);
            tipObj.transform.localPosition = new Vector3(0, -1f, 0);
            tipObj.transform.localRotation = Quaternion.identity;
            tipObj.transform.localScale = Vector3.one;

            SphereCollider tipCollider = tipObj.AddComponent<SphereCollider>();
            tipCollider.radius = 0.5f;
            tipCollider.isTrigger = true;

            PenTool penTool = penObj.AddComponent<PenTool>();

            VRDrawing.Tools.ToolTipCollisionDetector detector = tipObj.AddComponent<VRDrawing.Tools.ToolTipCollisionDetector>();

            PrefabUtility.SaveAsPrefabAsset(penObj, prefabPath);
            DestroyImmediate(penObj);

            Debug.Log($"✓ Created prefab: {prefabPath}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SetupScene()
        {
            Debug.Log("Setting up scene...");

            DrawingModeManager existing = FindFirstObjectByType<DrawingModeManager>();
            if (existing != null)
            {
                Debug.Log("✓ DrawingModeManager already exists in scene");
                AutoAssignReferences(existing);
                return;
            }

            GameObject managerObj = new GameObject("DrawingModeManager");
            DrawingModeManager manager = managerObj.AddComponent<DrawingModeManager>();

            AutoAssignReferences(manager);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("✓ Created DrawingModeManager in scene");
        }

        private void AutoAssignReferences(DrawingModeManager manager)
        {
            SerializedObject so = new SerializedObject(manager);

            GameObject drawingBoardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/DrawingBoard.prefab");
            if (drawingBoardPrefab != null)
            {
                so.FindProperty("drawingBoardPrefab").objectReferenceValue = drawingBoardPrefab;
                Debug.Log("✓ Assigned Drawing Board Prefab");
            }

            GameObject toolPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/ToolPanel.prefab");
            if (toolPanelPrefab != null)
            {
                so.FindProperty("toolPanelPrefab").objectReferenceValue = toolPanelPrefab;
                Debug.Log("✓ Assigned Tool Panel Prefab");
            }

            var teleportProvider = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
            if (teleportProvider != null)
            {
                so.FindProperty("teleportationProvider").objectReferenceValue = teleportProvider;
                Debug.Log("✓ Assigned Teleportation Provider");
            }

            var moveProvider = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
            if (moveProvider != null)
            {
                so.FindProperty("continuousMoveProvider").objectReferenceValue = moveProvider;
                Debug.Log("✓ Assigned Continuous Move Provider");
            }

            var turnProvider = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>();
            if (turnProvider != null)
            {
                so.FindProperty("continuousTurnProvider").objectReferenceValue = turnProvider;
                Debug.Log("✓ Assigned Continuous Turn Provider");
            }

            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                so.FindProperty("xrOrigin").objectReferenceValue = xrOrigin.transform;
                Debug.Log("✓ Assigned XR Origin");
            }

            GameObject uiRayObj = GameObject.Find("UI Ray Interactor");
            if (uiRayObj != null)
            {
                var uiRay = uiRayObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
                if (uiRay != null)
                {
                    so.FindProperty("uiRayInteractor").objectReferenceValue = uiRay;
                    Debug.Log("✓ Assigned UI Ray Interactor");
                }
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                so.FindProperty("playerCamera").objectReferenceValue = mainCam.transform;
                Debug.Log("✓ Assigned Player Camera");
            }

            so.ApplyModifiedProperties();
        }
    }
}
