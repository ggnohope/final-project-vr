using UnityEngine;
using UnityEditor;
using VRDrawing.Mode;
using VRDrawing.Tools;
using System.IO;

namespace VRDrawing.Editor
{
    public class VRDrawingValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showDetails = true;

        [MenuItem("Tools/VR Drawing/Validate Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<VRDrawingValidator>("VR Drawing Validator");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("VR Drawing System Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool validates your VR Drawing System setup and identifies missing components.", MessageType.Info);

            GUILayout.Space(20);

            if (GUILayout.Button("Run Validation", GUILayout.Height(40)))
            {
                RunValidation();
            }

            GUILayout.Space(10);

            showDetails = EditorGUILayout.Foldout(showDetails, "Validation Details", true);

            if (showDetails)
            {
                ValidateAndDisplay();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RunValidation()
        {
            Debug.Log("=== VR Drawing System Validation Started ===");
            ValidateAndDisplay();
            Debug.Log("=== VR Drawing System Validation Completed ===");
        }

        private void ValidateAndDisplay()
        {
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            ValidatePrefab("Drawing Board", "Assets/Prefabs/Drawing/DrawingBoard.prefab");
            ValidatePrefab("Tool Panel", "Assets/Prefabs/Drawing/ToolPanel.prefab");
            ValidatePrefab("Drawing Board Activator", "Assets/Prefabs/Drawing/DrawingBoardActivator.prefab");
            ValidatePrefab("Pen", "Assets/Prefabs/Drawing/Pen.prefab");

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            ValidateMaterial("Drawing Canvas", "Assets/Materials/Drawing/DrawingCanvas.mat");
            ValidateMaterial("Pen Material", "Assets/Materials/Drawing/PenMaterial.mat");

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);
            ValidateSceneComponent<DrawingModeManager>("DrawingModeManager");

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            ValidateLayer("Drawing Surface", 3);

            GUILayout.Space(10);

            EditorGUILayout.LabelField("XR Components", EditorStyles.boldLabel);
            ValidateSceneComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>("TeleportationProvider");
            ValidateSceneComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>("ContinuousMoveProvider");
            ValidateSceneComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>("ContinuousTurnProvider");
            ValidateSceneComponent<Unity.XR.CoreUtils.XROrigin>("XR Origin");

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Drawing System", EditorStyles.boldLabel);
            ValidateSceneComponent<DrawingSystemManager>("DrawingSystemManager");
        }

        private void ValidatePrefab(string name, string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                EditorGUILayout.LabelField(name, EditorStyles.helpBox);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                GUI.backgroundColor = Color.red;
                EditorGUILayout.LabelField($"{name} - MISSING", EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                Debug.LogWarning($"Missing prefab: {path}");
            }
        }

        private void ValidateMaterial(string name, string path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                EditorGUILayout.LabelField(name, EditorStyles.helpBox);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = mat;
                    EditorGUIUtility.PingObject(mat);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                GUI.backgroundColor = Color.red;
                EditorGUILayout.LabelField($"{name} - MISSING", EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                Debug.LogWarning($"Missing material: {path}");
            }
        }

        private void ValidateSceneComponent<T>(string name) where T : Component
        {
            T component = FindFirstObjectByType<T>();
            if (component != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                EditorGUILayout.LabelField(name, EditorStyles.helpBox);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = component.gameObject;
                    EditorGUIUtility.PingObject(component.gameObject);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                GUI.backgroundColor = Color.yellow;
                EditorGUILayout.LabelField($"{name} - NOT FOUND IN SCENE", EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                Debug.LogWarning($"Component not found in scene: {typeof(T).Name}");
            }
        }

        private void ValidateLayer(string layerName, int expectedIndex)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex >= 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                if (layerIndex == expectedIndex)
                {
                    EditorGUILayout.LabelField($"{layerName} (Layer {layerIndex})", EditorStyles.helpBox);
                }
                else
                {
                    GUI.backgroundColor = Color.yellow;
                    EditorGUILayout.LabelField($"{layerName} (Layer {layerIndex} - Expected {expectedIndex})", EditorStyles.helpBox);
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                GUI.backgroundColor = Color.red;
                EditorGUILayout.LabelField($"{layerName} - MISSING", EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                Debug.LogWarning($"Layer not found: {layerName}");
            }
        }
    }
}
