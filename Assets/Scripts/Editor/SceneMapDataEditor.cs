using UnityEngine;
using UnityEditor;
using Core;

namespace Editor
{
    [CustomEditor(typeof(SceneMapData))]
    public class SceneMapDataEditor : UnityEditor.Editor
    {
        private SceneMapData sceneMapData;
        private int selectedRegionIndex = -1;
        private Vector2 previewScrollPosition;
        private bool showRegionList = true;
        private bool showPreview = true;

        private void OnEnable()
        {
            sceneMapData = (SceneMapData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("World Map Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Region Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Add New Region", GUILayout.Height(30)))
            {
                AddNewRegion();
            }

            EditorGUILayout.Space(10);

            showRegionList = EditorGUILayout.Foldout(showRegionList, "Regions List", true);
            if (showRegionList)
            {
                DrawRegionsList();
            }

            EditorGUILayout.Space(10);

            showPreview = EditorGUILayout.Foldout(showPreview, "Map Preview", true);
            if (showPreview && sceneMapData.worldMapTexture != null)
            {
                DrawMapPreview();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(sceneMapData);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddNewRegion()
        {
            ArrayUtility.Add(ref sceneMapData.regions, new MapRegion
            {
                regionId = $"region_{sceneMapData.regions.Length}",
                displayName = "New Region",
                uvBounds = new Rect(0.1f, 0.1f, 0.2f, 0.2f),
                plyAssetPath = "",
                cameraConfig = new CameraConfig
                {
                    position = Vector3.zero,
                    rotation = Quaternion.identity,
                    fieldOfView = 60f
                },
                regionHighlightColor = new Color(1f, 1f, 0f, 0.3f)
            });
        }

        private void DrawRegionsList()
        {
            if (sceneMapData.regions == null || sceneMapData.regions.Length == 0)
            {
                EditorGUILayout.HelpBox("No regions defined. Click 'Add New Region' to create one.", MessageType.Info);
                return;
            }

            for (int i = 0; i < sceneMapData.regions.Length; i++)
            {
                DrawRegionItem(i);
            }
        }

        private void DrawRegionItem(int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            bool isExpanded = selectedRegionIndex == index;
            string label = $"{sceneMapData.regions[index].displayName} ({sceneMapData.regions[index].regionId})";
            
            if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(20)))
            {
                selectedRegionIndex = isExpanded ? -1 : index;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Remove Region",
                    $"Are you sure you want to remove '{sceneMapData.regions[index].displayName}'?",
                    "Yes", "No"))
                {
                    ArrayUtility.RemoveAt(ref sceneMapData.regions, index);
                    if (selectedRegionIndex == index)
                        selectedRegionIndex = -1;
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawRegionDetails(index);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawRegionDetails(int index)
        {
            MapRegion region = sceneMapData.regions[index];

            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            region.regionId = EditorGUILayout.TextField("Region ID", region.regionId);
            region.displayName = EditorGUILayout.TextField("Display Name", region.displayName);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("UV Bounds (Normalized 0-1)", EditorStyles.boldLabel);
            region.uvBounds = EditorGUILayout.RectField("UV Bounds", region.uvBounds);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Gaussian Splatting Asset", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Path relative to Resources folder (without .asset extension)", MessageType.Info);
            region.plyAssetPath = EditorGUILayout.TextField("PLY Asset Path", region.plyAssetPath);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Camera Configuration", EditorStyles.boldLabel);
            region.cameraConfig.position = EditorGUILayout.Vector3Field("Position", region.cameraConfig.position);
            region.cameraConfig.rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", region.cameraConfig.rotation.eulerAngles));
            region.cameraConfig.fieldOfView = EditorGUILayout.Slider("Field of View", region.cameraConfig.fieldOfView, 1f, 179f);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
            region.regionHighlightColor = EditorGUILayout.ColorField("Highlight Color", region.regionHighlightColor);

            sceneMapData.regions[index] = region;
        }

        private void DrawMapPreview()
        {
            Texture2D mapTexture = sceneMapData.worldMapTexture;
            if (mapTexture == null) return;

            float aspectRatio = (float)mapTexture.width / mapTexture.height;
            float previewWidth = EditorGUIUtility.currentViewWidth - 40;
            float previewHeight = previewWidth / aspectRatio;

            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);

            EditorGUI.DrawPreviewTexture(previewRect, mapTexture);

            if (sceneMapData.regions != null)
            {
                foreach (var region in sceneMapData.regions)
                {
                    Rect regionRect = new Rect(
                        previewRect.x + region.uvBounds.x * previewRect.width,
                        previewRect.y + (1 - region.uvBounds.y - region.uvBounds.height) * previewRect.height,
                        region.uvBounds.width * previewRect.width,
                        region.uvBounds.height * previewRect.height
                    );

                    EditorGUI.DrawRect(regionRect, region.regionHighlightColor);
                    
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                    labelStyle.normal.textColor = Color.white;
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    labelStyle.fontSize = 10;
                    
                    GUI.Label(regionRect, region.displayName, labelStyle);
                }
            }

            EditorGUILayout.HelpBox(
                "Preview shows the world map with region bounds overlaid. " +
                "UV coordinates are normalized (0-1). Origin is bottom-left.",
                MessageType.Info);
        }
    }
}
