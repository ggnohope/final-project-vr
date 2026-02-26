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
                EditorGUILayout.PropertyField(regionProp.FindPropertyRelative("uvBounds"), new GUIContent("UV Bounds"));
                EditorGUILayout.PropertyField(regionProp.FindPropertyRelative("plyAssetPath"), new GUIContent("Ply Asset Path"));

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

        /// <summary>Draw the map preview with region UV bounds overlaid.</summary>
        private void DrawMapPreview(SceneMapData mapData)
        {
            EditorGUILayout.LabelField("Map Preview", EditorStyles.boldLabel);

            if (mapData.worldMapTexture == null)
            {
                EditorGUILayout.HelpBox("Assign a World Map Texture to see the preview.", MessageType.Info);
                return;
            }

            // Reserve a fixed-height rect for the map
            Rect previewRect = GUILayoutUtility.GetRect(
                GUILayoutUtility.GetLastRect().width,
                MapPreviewHeight,
                GUILayout.ExpandWidth(true)
            );

            // Draw checkerboard background
            EditorGUI.DrawTextureTransparent(previewRect, Texture2D.grayTexture);
            // Draw texture fitted inside rect (maintain aspect)
            Rect texRect = FitRectInside(previewRect, mapData.worldMapTexture.width, mapData.worldMapTexture.height);
            GUI.DrawTexture(texRect, mapData.worldMapTexture, ScaleMode.ScaleToFit, true);

            // Overlay region UV bounds
            if (mapData.regions != null)
            {
                foreach (var region in mapData.regions)
                {
                    DrawRegionOverlay(texRect, region);
                }
            }

            EditorGUILayout.HelpBox(
                "Preview shows the world map with region bounds overlaid. UV coordinates are normalized (0-1). Origin is bottom-left.",
                MessageType.None
            );
        }

        /// <summary>Draw a colored rectangle overlay for a region's UV bounds.</summary>
        private void DrawRegionOverlay(Rect texRect, MapRegion region)
        {
            // UV origin is bottom-left; GUI origin is top-left — flip Y
            float x = texRect.x + region.uvBounds.x * texRect.width;
            float y = texRect.y + (1f - region.uvBounds.y - region.uvBounds.height) * texRect.height;
            float w = region.uvBounds.width * texRect.width;
            float h = region.uvBounds.height * texRect.height;

            Rect guiRect = new Rect(x, y, w, h);

            Color fillColor = region.regionHighlightColor;
            fillColor.a = 0.25f;
            EditorGUI.DrawRect(guiRect, fillColor);

            // Border
            Color borderColor = region.regionHighlightColor;
            borderColor.a = 0.85f;
            DrawRectBorder(guiRect, borderColor, 2f);

            // Label
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold
            };

            string label = string.IsNullOrEmpty(region.displayName) ? region.regionId : region.displayName;
            GUI.Label(new Rect(x + 3, y + 2, w - 4, 16), label, labelStyle);
        }

        private static void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static Rect FitRectInside(Rect container, float texW, float texH)
        {
            float texAspect = texW / texH;
            float containerAspect = container.width / container.height;

            float w, h;
            if (texAspect > containerAspect)
            {
                w = container.width;
                h = w / texAspect;
            }
            else
            {
                h = container.height;
                w = h * texAspect;
            }

            float offsetX = container.x + (container.width - w) * 0.5f;
            float offsetY = container.y + (container.height - h) * 0.5f;
            return new Rect(offsetX, offsetY, w, h);
        }

        private void AddNewRegion(SceneMapData mapData)
        {
            Undo.RecordObject(mapData, "Add New Region");

            int newIndex = mapData.regions != null ? mapData.regions.Length : 0;
            var newRegions = new MapRegion[newIndex + 1];

            if (mapData.regions != null)
                System.Array.Copy(mapData.regions, newRegions, newIndex);

            newRegions[newIndex] = new MapRegion
            {
                regionId = $"region_{newIndex}",
                displayName = string.Empty,
                uvBounds = new Rect(0.5f, 0.5f, 0.1f, 0.1f),
                plyAssetPath = string.Empty,
                cameraConfig = new CameraConfig { fieldOfView = 60f },
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
    }
}
