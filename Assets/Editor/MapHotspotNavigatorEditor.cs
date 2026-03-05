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

            // References
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            DrawProperty("worldMapController");
            DrawProperty("sceneMapData");
            DrawProperty("tileRenderer");

            EditorGUILayout.Space();

            // Auto Generation — hotspots array chỉ hiện khi manual mode
            EditorGUILayout.LabelField("Auto Generation", EditorStyles.boldLabel);
            SerializedProperty autoGenerate = serializedObject.FindProperty("autoGenerateFromData");
            if (autoGenerate != null)
            {
                EditorGUILayout.PropertyField(autoGenerate);
                EditorGUI.indentLevel++;
                if (autoGenerate.boolValue)
                {
                    DrawProperty("hotspotPrefab");
                    DrawProperty("hotspotsContainer");
                }
                else
                {
                    DrawProperty("hotspots", includeChildren: true);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Audio
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            DrawProperty("audioSource");
            DrawProperty("navigationSound");
            DrawProperty("confirmSound");

            EditorGUILayout.Space();

            // Tooltip
            EditorGUILayout.LabelField("Tooltip (Optional)", EditorStyles.boldLabel);
            DrawProperty("tooltip");
            DrawProperty("showTooltipOnSelection");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Draws a property field only if the property exists on the serialized object.</summary>
        private void DrawProperty(string propertyName, bool includeChildren = false)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, includeChildren);
        }
    }
}
