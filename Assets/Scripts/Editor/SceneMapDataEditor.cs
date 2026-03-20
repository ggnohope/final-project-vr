using UnityEditor;
using UnityEngine;
using Core;

namespace Editor
{
    [CustomEditor(typeof(SceneMapData))]
    public class SceneMapDataEditor : UnityEditor.Editor
    {
        // Foldout state per region index
        private bool[] regionFoldouts = new bool[0];
        private Vector2 mapPreviewScrollPos;

        private const float MapPreviewHeight = 300f;
        private const float RegionHandleSize = 6f;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var mapData = (SceneMapData)target;

            // --- World Map Settings ---
            EditorGUILayout.LabelField("World Map Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("worldMapTexture"));
            EditorGUILayout.Space(6);

            // --- Transition Settings ---
            EditorGUILayout.LabelField("Transition Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionFadeTime"), new GUIContent("Transition Fade Time"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumLoadTime"), new GUIContent("Minimum Load Time"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeCurve"), new GUIContent("Fade Curve"));
            EditorGUILayout.Space(6);

            // --- Loading Screen ---
            EditorGUILayout.LabelField("Loading Screen", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loadingScreenOverlay"), new GUIContent("Loading Screen Overlay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loadingTextFormat"), new GUIContent("Loading Text Format"));
            EditorGUILayout.Space(6);

            // --- Region Management ---
            EditorGUILayout.LabelField("Region Management", EditorStyles.boldLabel);

            if (GUILayout.Button("Add New Region"))
            {
                AddNewRegion(mapData);
            }

            EditorGUILayout.Space(4);

            var regionsProp = serializedObject.FindProperty("regions");
            SyncFoldoutArray(regionsProp.arraySize);

            EditorGUILayout.LabelField("Regions List", EditorStyles.boldLabel);

            for (int i = 0; i < regionsProp.arraySize; i++)
            {
                DrawRegionItem(regionsProp, i, mapData);
            }

            EditorGUILayout.Space(8);

            // --- Map Preview ---
            DrawMapPreview(mapData);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Draw a collapsible region entry with a Remove button.</summary>
        private void DrawRegionItem(SerializedProperty regionsProp, int index, SceneMapData mapData)
        {
            var regionProp = regionsProp.GetArrayElementAtIndex(index);
            var displayNameProp = regionProp.FindPropertyRelative("displayName");
            var regionIdProp = regionProp.FindPropertyRelative("regionId");

            string label = string.IsNullOrEmpty(displayNameProp.stringValue)
                ? $"(unnamed) (region_{index})"
                : $"{displayNameProp.stringValue} ({regionIdProp.stringValue})";

            EditorGUILayout.BeginHorizontal();
            regionFoldouts[index] = EditorGUILayout.Foldout(regionFoldouts[index], label, true);

            GUILayout.FlexibleSpace();

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                regionsProp.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                GUI.backgroundColor = prevColor;
                return;
            }
            GUI.backgroundColor = prevColor;
            EditorGUILayout.EndHorizontal();

            if (regionFoldouts[index])
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(regionIdProp, new GUIContent("Region Id"));
                EditorGUILayout.PropertyField(displayNameProp, new GUIContent("Display Name"));
                EditorGUILayout.PropertyField(regionProp.FindPropertyRelative("latLng"), new GUIContent("Lat / Lng", "Province centroid. x = latitude (N), y = longitude (E). Decimal degrees WGS84."));
                EditorGUILayout.PropertyField(regionProp.FindPropertyRelative("plyAssetPath"), new GUIContent("Ply Asset Path"));
                EditorGUILayout.PropertyField(regionProp.FindPropertyRelative("videoResourcePath"), new GUIContent("FlyCam Video Path", "Path relative to Resources folder, no extension. E.g. 'Videos/my-flycam'"));

                DrawModelsArray(regionProp.FindPropertyRelative("models"));

                var cameraProp = regionProp.FindPropertyRelative("cameraConfig");
                EditorGUILayout.LabelField("Camera Config", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(cameraProp.FindPropertyRelative("position"), new GUIContent("Position"));

                var rotProp = cameraProp.FindPropertyRelative("rotation");
                Quaternion currentRot = rotProp.quaternionValue;
                Vector3 euler = EditorGUILayout.Vector3Field("Rotation", currentRot.eulerAngles);
                rotProp.quaternionValue = Quaternion.Euler(euler);

                EditorGUILayout.PropertyField(cameraProp.FindPropertyRelative("fieldOfView"), new GUIContent("Field Of View"));
                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(regionProp.FindPropertyRelative("regionHighlightColor"), new GUIContent("Region Highlight Color"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
        }

        /// <summary>Draw the map preview — shows lat/lng reference info since the static texture is no longer used.</summary>
        private void DrawMapPreview(SceneMapData mapData)
        {
            EditorGUILayout.LabelField("Lat / Lng Reference — Vietnam", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The static world map texture is no longer used.\n" +
                "Tiles are fetched from Mapbox at runtime.\n\n" +
                "Vietnam bounding box (approximate):\n" +
                "  Latitude:  8.18° N  →  23.39° N\n" +
                "  Longitude: 102.14° E  →  109.46° E\n\n" +
                "Enter province centroid coordinates in each region's Lat / Lng field.",
                MessageType.Info
            );
        }

        private void AddNewRegion(SceneMapData mapData)
        {
            Undo.RecordObject(mapData, "Add New Region");

            int newIndex = mapData.regions != null ? mapData.regions.Length : 0;
            var newRegions = new MapRegion[newIndex + 1];

            if (mapData.regions != null)
                System.Array.Copy(mapData.regions, newRegions, newIndex);

            // Default centroid: center of Vietnam
            newRegions[newIndex] = new MapRegion
            {
                regionId             = $"region_{newIndex}",
                displayName          = string.Empty,
                latLng               = new Vector2(16.0f, 106.5f),
                plyAssetPath         = string.Empty,
                videoResourcePath    = string.Empty,
                cameraConfig         = new CameraConfig { fieldOfView = 60f },
                regionHighlightColor = Color.yellow
            };

            mapData.regions = newRegions;
            EditorUtility.SetDirty(mapData);

            SyncFoldoutArray(newRegions.Length);
            regionFoldouts[newIndex] = true;
        }

        private void SyncFoldoutArray(int count)
        {
            if (regionFoldouts.Length != count)
            {
                bool[] updated = new bool[count];
                for (int i = 0; i < Mathf.Min(regionFoldouts.Length, count); i++)
                    updated[i] = regionFoldouts[i];
                regionFoldouts = updated;
            }
        }

        /// <summary>Draws an inline editable list of RegionModel3D entries (model name + position).</summary>
        private void DrawModelsArray(SerializedProperty modelsProp)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3D Models", EditorStyles.miniBoldLabel);

            for (int i = 0; i < modelsProp.arraySize; i++)
            {
                SerializedProperty entry = modelsProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp     = entry.FindPropertyRelative("modelName");
                SerializedProperty posProp      = entry.FindPropertyRelative("position");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(24));
                nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.Width(80));
                posProp.vector3Value = EditorGUILayout.Vector3Field(GUIContent.none, posProp.vector3Value);

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    modelsProp.DeleteArrayElementAtIndex(i);
                    GUI.backgroundColor = prev;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                GUI.backgroundColor = prev;
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Model", EditorStyles.miniButton))
            {
                modelsProp.InsertArrayElementAtIndex(modelsProp.arraySize);
                SerializedProperty newEntry = modelsProp.GetArrayElementAtIndex(modelsProp.arraySize - 1);
                newEntry.FindPropertyRelative("modelName").stringValue = string.Empty;
                newEntry.FindPropertyRelative("position").vector3Value  = Vector3.zero;
            }

            EditorGUILayout.EndVertical();
        }
    }
}
