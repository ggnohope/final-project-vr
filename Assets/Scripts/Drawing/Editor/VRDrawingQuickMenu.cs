using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRDrawing.Mode;

namespace VRDrawing.Editor
{
    public static class VRDrawingQuickMenu
    {
        [MenuItem("Tools/VR Drawing/Quick Setup/1. Run Complete Auto Setup", priority = 0)]
        public static void RunCompleteSetup()
        {
            VRDrawingAutoSetup.ShowWindow();
        }

        [MenuItem("Tools/VR Drawing/Quick Setup/2. Validate Setup", priority = 1)]
        public static void ValidateSetup()
        {
            VRDrawingValidator.ShowWindow();
        }

        [MenuItem("Tools/VR Drawing/Quick Setup/3. Setup Drawing Mode Manager", priority = 2)]
        public static void SetupDrawingModeManager()
        {
            DrawingModeManager existing = Object.FindFirstObjectByType<DrawingModeManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("DrawingModeManager already exists in scene. Selected.");
                return;
            }

            GameObject managerObj = new GameObject("DrawingModeManager");
            DrawingModeManager manager = managerObj.AddComponent<DrawingModeManager>();

            AutoAssignManagerReferences(manager);

            Selection.activeGameObject = managerObj;
            EditorGUIUtility.PingObject(managerObj);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("✓ Created DrawingModeManager with auto-assigned references");
            EditorUtility.DisplayDialog("Success", "DrawingModeManager created and configured!", "OK");
        }

        [MenuItem("Tools/VR Drawing/Scene Objects/Select Drawing Mode Manager", priority = 20)]
        public static void SelectDrawingModeManager()
        {
            DrawingModeManager manager = Object.FindFirstObjectByType<DrawingModeManager>();
            if (manager != null)
            {
                Selection.activeGameObject = manager.gameObject;
                EditorGUIUtility.PingObject(manager.gameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", "DrawingModeManager not found in scene.", "OK");
            }
        }

        [MenuItem("Tools/VR Drawing/Scene Objects/Select Drawing System Manager", priority = 21)]
        public static void SelectDrawingSystemManager()
        {
            DrawingSystemManager manager = Object.FindFirstObjectByType<DrawingSystemManager>();
            if (manager != null)
            {
                Selection.activeGameObject = manager.gameObject;
                EditorGUIUtility.PingObject(manager.gameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", "DrawingSystemManager not found in scene.", "OK");
            }
        }

        [MenuItem("Tools/VR Drawing/Scene Objects/Select Item Spawner", priority = 22)]
        public static void SelectItemSpawner()
        {
            ItemSpawner spawner = Object.FindFirstObjectByType<ItemSpawner>();
            if (spawner != null)
            {
                Selection.activeGameObject = spawner.gameObject;
                EditorGUIUtility.PingObject(spawner.gameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", "ItemSpawner not found in scene.", "OK");
            }
        }

        [MenuItem("Tools/VR Drawing/Prefabs/Open Drawing Board Prefab", priority = 40)]
        public static void OpenDrawingBoardPrefab()
        {
            OpenPrefab("Assets/Prefabs/Drawing/DrawingBoard.prefab");
        }

        [MenuItem("Tools/VR Drawing/Prefabs/Open Tool Panel Prefab", priority = 41)]
        public static void OpenToolPanelPrefab()
        {
            OpenPrefab("Assets/Prefabs/Drawing/ToolPanel.prefab");
        }

        [MenuItem("Tools/VR Drawing/Prefabs/Open Pen Prefab", priority = 42)]
        public static void OpenPenPrefab()
        {
            OpenPrefab("Assets/Prefabs/Drawing/Pen.prefab");
        }

        [MenuItem("Tools/VR Drawing/Prefabs/Open Activator Prefab", priority = 43)]
        public static void OpenActivatorPrefab()
        {
            OpenPrefab("Assets/Prefabs/Drawing/DrawingBoardActivator.prefab");
        }

        [MenuItem("Tools/VR Drawing/Documentation/Open Setup Guide", priority = 60)]
        public static void OpenSetupGuide()
        {
            Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html");
        }

        [MenuItem("Tools/VR Drawing/Documentation/Show Console Log", priority = 61)]
        public static void ShowConsoleLog()
        {
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor"));
        }

        [MenuItem("Tools/VR Drawing/Utilities/Clear All Console Logs", priority = 80)]
        public static void ClearConsoleLogs()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
            Debug.Log("Console cleared.");
        }

        [MenuItem("Tools/VR Drawing/Utilities/Force Recompile Scripts", priority = 81)]
        public static void ForceRecompile()
        {
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Debug.Log("Recompiling scripts...");
        }

        private static void OpenPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                AssetDatabase.OpenAsset(prefab);
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", $"Prefab not found at {path}", "OK");
            }
        }

        private static void AutoAssignManagerReferences(DrawingModeManager manager)
        {
            SerializedObject so = new SerializedObject(manager);

            GameObject boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drawing/DrawingBoard.prefab");
            if (boardPrefab != null)
            {
                so.FindProperty("drawingBoardPrefab").objectReferenceValue = boardPrefab;
            }

            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drawing/ToolPanel.prefab");
            if (panelPrefab != null)
            {
                so.FindProperty("toolPanelPrefab").objectReferenceValue = panelPrefab;
            }

            var teleport = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
            if (teleport != null)
            {
                so.FindProperty("teleportationProvider").objectReferenceValue = teleport;
            }

            var move = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
            if (move != null)
            {
                so.FindProperty("continuousMoveProvider").objectReferenceValue = move;
            }

            var turn = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>();
            if (turn != null)
            {
                so.FindProperty("continuousTurnProvider").objectReferenceValue = turn;
            }

            var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                so.FindProperty("xrOrigin").objectReferenceValue = origin.transform;
            }

            GameObject uiRayObj = GameObject.Find("UI Ray Interactor");
            if (uiRayObj != null)
            {
                var ray = uiRayObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
                if (ray != null)
                {
                    so.FindProperty("uiRayInteractor").objectReferenceValue = ray;
                }
            }

            Camera main = Camera.main;
            if (main != null)
            {
                so.FindProperty("playerCamera").objectReferenceValue = main.transform;
            }

            so.ApplyModifiedProperties();
        }
    }
}
