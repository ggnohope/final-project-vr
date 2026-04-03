using UnityEngine;
using UnityEditor;
using VRDrawing.Mode;
using UnityEngine.InputSystem;

namespace VRDrawing.Editor
{
    [CustomEditor(typeof(DrawingModeManager))]
    public class DrawingModeManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty drawingBoardPrefab;
        private SerializedProperty toolPanelPrefab;
        private SerializedProperty toggleToolPanelAction;
        private SerializedProperty teleportationProvider;
        private SerializedProperty continuousMoveProvider;
        private SerializedProperty continuousTurnProvider;
        private SerializedProperty xrOrigin;
        private SerializedProperty uiRayInteractor;
        private SerializedProperty playerCamera;

        private bool showBoardSettings = true;
        private bool showToolPanelSettings = true;
        private bool showInputSettings = false;
        private bool showLocomotionSettings = true;
        private bool showUISettings = true;
        private bool showReferenceSettings = true;

        private void OnEnable()
        {
            drawingBoardPrefab = serializedObject.FindProperty("drawingBoardPrefab");
            toolPanelPrefab = serializedObject.FindProperty("toolPanelPrefab");
            toggleToolPanelAction = serializedObject.FindProperty("toggleToolPanelAction");
            teleportationProvider = serializedObject.FindProperty("teleportationProvider");
            continuousMoveProvider = serializedObject.FindProperty("continuousMoveProvider");
            continuousTurnProvider = serializedObject.FindProperty("continuousTurnProvider");
            xrOrigin = serializedObject.FindProperty("xrOrigin");
            uiRayInteractor = serializedObject.FindProperty("uiRayInteractor");
            playerCamera = serializedObject.FindProperty("playerCamera");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawingModeManager manager = (DrawingModeManager)target;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Drawing Mode Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Manages Drawing Mode state, locomotion control, and UI visibility.", MessageType.Info);

            GUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🔧 Auto-Assign All References", GUILayout.Height(35)))
            {
                AutoAssignAllReferences(manager);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Setup", GUILayout.Height(25)))
            {
                ValidateSetup(manager);
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
            {
                ClearAllReferences();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            showBoardSettings = EditorGUILayout.Foldout(showBoardSettings, "Drawing Board Settings", true);
            if (showBoardSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(drawingBoardPrefab);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boardDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boardHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boardScale"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            showToolPanelSettings = EditorGUILayout.Foldout(showToolPanelSettings, "Tool Panel Settings", true);
            if (showToolPanelSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(toolPanelPrefab);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("toolPanelParent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("panelDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("panelHeight"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            showInputSettings = EditorGUILayout.Foldout(showInputSettings, "Input Settings (Optional)", true);
            if (showInputSettings && toggleToolPanelAction != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(toggleToolPanelAction);
                EditorGUILayout.HelpBox("Assign: XRI LeftHand Interaction/Secondary Button (Y Button)", MessageType.Info);

                SerializedProperty useReference = toggleToolPanelAction.FindPropertyRelative("m_UseReference");
                SerializedProperty reference = toggleToolPanelAction.FindPropertyRelative("m_Reference");
                
                if (useReference != null && reference != null)
                {
                    bool isUsingReference = useReference.boolValue;
                    bool hasReference = reference.objectReferenceValue != null;
                    
                    if (!isUsingReference || !hasReference)
                    {
                        if (GUILayout.Button("Auto-Setup Y Button Input"))
                        {
                            SetupYButtonInput();
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            showLocomotionSettings = EditorGUILayout.Foldout(showLocomotionSettings, "Locomotion Settings", true);
            if (showLocomotionSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(teleportationProvider);
                EditorGUILayout.PropertyField(continuousMoveProvider);
                EditorGUILayout.PropertyField(continuousTurnProvider);
                EditorGUILayout.PropertyField(xrOrigin);

                if (GUILayout.Button("Auto-Find Locomotion Components"))
                {
                    AutoFindLocomotion(manager);
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            showUISettings = EditorGUILayout.Foldout(showUISettings, "UI Ray Settings", true);
            if (showUISettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(uiRayInteractor);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoFindUIRay"));

                if (uiRayInteractor.objectReferenceValue == null)
                {
                    if (GUILayout.Button("Find UI Ray Interactor"))
                    {
                        FindUIRay();
                    }
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            showReferenceSettings = EditorGUILayout.Foldout(showReferenceSettings, "Other References", true);
            if (showReferenceSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(playerCamera);

                if (playerCamera.objectReferenceValue == null)
                {
                    if (GUILayout.Button("Assign Main Camera"))
                    {
                        AssignMainCamera();
                    }
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(15);

            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Is In Drawing Mode:", manager.IsInDrawingMode.ToString());
                if (manager.ActiveDrawingBoard != null)
                {
                    EditorGUILayout.LabelField("Active Board:", manager.ActiveDrawingBoard.name);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime status", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AutoAssignAllReferences(DrawingModeManager manager)
        {
            Undo.RecordObject(manager, "Auto-Assign References");

            GameObject boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drawing/DrawingBoard.prefab");
            if (boardPrefab != null)
                drawingBoardPrefab.objectReferenceValue = boardPrefab;

            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drawing/ToolPanel.prefab");
            if (panelPrefab != null)
                toolPanelPrefab.objectReferenceValue = panelPrefab;

            AutoFindLocomotion(manager);
            FindUIRay();
            AssignMainCamera();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);

            EditorUtility.DisplayDialog("Success", "All references have been auto-assigned!", "OK");
        }

        private void AutoFindLocomotion(DrawingModeManager manager)
        {
            var teleport = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
            if (teleport != null)
                teleportationProvider.objectReferenceValue = teleport;

            var move = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
            if (move != null)
                continuousMoveProvider.objectReferenceValue = move;

            var turn = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>();
            if (turn != null)
                continuousTurnProvider.objectReferenceValue = turn;

            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
                xrOrigin.objectReferenceValue = origin.transform;

            serializedObject.ApplyModifiedProperties();
        }

        private void FindUIRay()
        {
            GameObject uiRayObj = GameObject.Find("UI Ray Interactor");
            if (uiRayObj != null)
            {
                var ray = uiRayObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
                if (ray != null)
                    uiRayInteractor.objectReferenceValue = ray;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AssignMainCamera()
        {
            Camera main = Camera.main;
            if (main != null)
            {
                playerCamera.objectReferenceValue = main.transform;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetupYButtonInput()
        {
            EditorUtility.DisplayDialog("Input Setup", 
                "Please configure the Y Button input manually:\n\n" +
                "1. Open your Input Actions asset\n" +
                "2. Find 'XRI LeftHand Interaction'\n" +
                "3. Add or select 'Secondary Button' action\n" +
                "4. Assign it to the Toggle Tool Panel Action field",
                "OK");
        }

        private void ValidateSetup(DrawingModeManager manager)
        {
            bool valid = true;
            int warnings = 0;

            if (drawingBoardPrefab.objectReferenceValue == null)
                valid = false;

            if (toolPanelPrefab.objectReferenceValue == null)
                valid = false;

            if (toggleToolPanelAction != null)
            {
                SerializedProperty useReference = toggleToolPanelAction.FindPropertyRelative("m_UseReference");
                SerializedProperty reference = toggleToolPanelAction.FindPropertyRelative("m_Reference");

                if (useReference != null && reference != null)
                {
                    if (!useReference.boolValue || reference.objectReferenceValue == null)
                        warnings++;
                }
            }

            if (teleportationProvider.objectReferenceValue == null) warnings++;
            if (continuousMoveProvider.objectReferenceValue == null) warnings++;
            if (continuousTurnProvider.objectReferenceValue == null) warnings++;
            if (xrOrigin.objectReferenceValue == null) warnings++;
            if (uiRayInteractor.objectReferenceValue == null) warnings++;
            if (playerCamera.objectReferenceValue == null) warnings++;

            if (valid && warnings == 0)
                EditorUtility.DisplayDialog("Validation Passed", "DrawingModeManager setup is complete!", "OK");
            else if (!valid)
                EditorUtility.DisplayDialog("Validation Failed", "Critical references are missing. Check the Inspector.", "OK");
            else
                EditorUtility.DisplayDialog("Validation Warning", $"{warnings} optional reference(s) are not assigned.", "OK");
        }

        private void ClearAllReferences()
        {
            if (EditorUtility.DisplayDialog("Clear References",
                "Are you sure you want to clear all references?",
                "Yes", "Cancel"))
            {
                drawingBoardPrefab.objectReferenceValue = null;
                toolPanelPrefab.objectReferenceValue = null;
                teleportationProvider.objectReferenceValue = null;
                continuousMoveProvider.objectReferenceValue = null;
                continuousTurnProvider.objectReferenceValue = null;
                xrOrigin.objectReferenceValue = null;
                uiRayInteractor.objectReferenceValue = null;
                playerCamera.objectReferenceValue = null;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
