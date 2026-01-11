using UnityEngine;
using UnityEditor;

namespace VRDrawing.Editor
{
    public class InputActionSetupHelper : EditorWindow
    {
        [MenuItem("Tools/VR Drawing/Help/Input Action Setup Guide")]
        public static void ShowWindow()
        {
            var window = GetWindow<InputActionSetupHelper>("Input Action Setup");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Input Action Setup Guide", EditorStyles.boldLabel);

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("The VR Drawing System requires the Y Button on the Left Controller to toggle the Tool Panel. Follow these steps to configure it:", MessageType.Info);

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Step 1: Locate Input Actions Asset", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "Find your Input Actions asset in your project. Common locations:\n" +
                "• Assets/Samples/XR Interaction Toolkit/.../XRI Default Input Actions.inputactions\n" +
                "• Assets/InputActions/XRInputActions.inputactions\n" +
                "• Project Settings > Input System > Input Actions Asset",
                GUILayout.Height(80));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Step 2: Open Input Actions Asset", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "1. Double-click the .inputactions file in Project window\n" +
                "2. This opens the Input Actions editor window",
                GUILayout.Height(50));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Step 3: Find XRI LeftHand Interaction", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "In the Input Actions window:\n" +
                "1. Look for 'XRI LeftHand Interaction' Action Map\n" +
                "2. Or 'Left Controller' or similar naming",
                GUILayout.Height(60));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Step 4: Add Secondary Button Action", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "If 'Secondary Button' action doesn't exist:\n" +
                "1. Click '+' to add new Action\n" +
                "2. Name it 'Secondary Button'\n" +
                "3. Action Type: Button\n" +
                "4. Control Type: Button",
                GUILayout.Height(90));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Step 5: Bind to Y Button", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "1. Click on 'Secondary Button' action\n" +
                "2. In right panel, click '+' next to Bindings\n" +
                "3. Select 'Add Binding'\n" +
                "4. Click '<No Binding>'\n" +
                "5. Press Y button on your left VR controller\n" +
                "6. Or manually select: XR Controller > Left Hand > Secondary Button",
                GUILayout.Height(130));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Step 6: Save Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "1. Click 'Save Asset' button in Input Actions window\n" +
                "2. Close the window",
                GUILayout.Height(50));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Step 7: Assign to DrawingModeManager", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "1. Select DrawingModeManager in Hierarchy\n" +
                "2. In Inspector, find 'Toggle Tool Panel Action'\n" +
                "3. Click the dropdown for 'Action'\n" +
                "4. Navigate: XRI LeftHand Interaction > Secondary Button\n" +
                "5. Ensure 'Use Reference' is selected",
                GUILayout.Height(110));

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Alternative: Copy Existing Action", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "If you have a working Primary Button (X Button):\n" +
                "1. Right-click on 'Primary Button' action\n" +
                "2. Select 'Duplicate'\n" +
                "3. Rename to 'Secondary Button'\n" +
                "4. Edit binding to Y button instead of X",
                GUILayout.Height(90));

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Verification", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "Test in Play Mode:\n" +
                "1. Enter Drawing Mode (spawn Drawing Board)\n" +
                "2. Press Y button on left controller\n" +
                "3. Tool Panel should appear/disappear\n" +
                "4. If not working, check Console for errors",
                GUILayout.Height(90));

            GUILayout.Space(20);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Select DrawingModeManager in Scene", GUILayout.Height(35)))
            {
                var manager = FindFirstObjectByType<VRDrawing.Mode.DrawingModeManager>();
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
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            if (GUILayout.Button("Open Project Settings > Input System", GUILayout.Height(30)))
            {
                SettingsService.OpenProjectSettings("Project/Input System Package");
            }

            GUILayout.Space(20);

            EditorGUILayout.HelpBox("For more information about Input Actions, see Unity's Input System documentation.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }
    }
}
