using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
    [CustomEditor(typeof(MapHotspotNavigator))]
    public class MapHotspotNavigatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Input Actions
            EditorGUILayout.LabelField("Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("joystickMoveAction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("confirmButtonAction"));

            EditorGUILayout.Space();

            // References
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("worldMapController"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneMapData"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tileRenderer"));

            EditorGUILayout.Space();

            // Auto Generation — hotspots array chỉ hiện khi manual mode
            EditorGUILayout.LabelField("Auto Generation", EditorStyles.boldLabel);
            SerializedProperty autoGenerate = serializedObject.FindProperty("autoGenerateFromData");
            EditorGUILayout.PropertyField(autoGenerate);

            EditorGUI.indentLevel++;
            if (autoGenerate.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hotspotPrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hotspotsContainer"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hotspots"), true);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Navigation Settings
            EditorGUILayout.LabelField("Navigation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("joystickThreshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationDebounceTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableWrapping"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableVerticalNavigation"));

            EditorGUILayout.Space();

            // Audio
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioSource"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("confirmSound"));

            EditorGUILayout.Space();

            // Camera Focus
            EditorGUILayout.LabelField("Camera Focus (Optional)", EditorStyles.boldLabel);
            SerializedProperty enableCameraFocus = serializedObject.FindProperty("enableCameraFocus");
            EditorGUILayout.PropertyField(enableCameraFocus);
            if (enableCameraFocus.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mainCamera"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("focusTransitionSpeed"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Tooltip
            EditorGUILayout.LabelField("Tooltip (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tooltip"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showTooltipOnSelection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tooltipOffset"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
