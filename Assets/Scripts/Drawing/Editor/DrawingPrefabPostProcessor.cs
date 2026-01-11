using UnityEngine;
using UnityEditor;
using VRDrawing.Tools;
using VRDrawing.UI;

namespace VRDrawing.Editor
{
    public class DrawingPrefabPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.Contains("Prefabs/Drawing/DrawingBoard.prefab"))
                {
                    ValidateDrawingBoardPrefab(assetPath);
                }
                else if (assetPath.Contains("Prefabs/Drawing/Pen.prefab"))
                {
                    ValidatePenPrefab(assetPath);
                }
                else if (assetPath.Contains("Prefabs/Drawing/ToolPanel.prefab"))
                {
                    ValidateToolPanelPrefab(assetPath);
                }
            }
        }

        private static void ValidateDrawingBoardPrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return;

            bool modified = false;

            DrawingSurface surface = prefab.GetComponentInChildren<DrawingSurface>();
            if (surface != null)
            {
                SerializedObject so = new SerializedObject(surface);

                if (surface.gameObject.layer != LayerMask.NameToLayer("Drawing Surface"))
                {
                    surface.gameObject.layer = LayerMask.NameToLayer("Drawing Surface");
                    modified = true;
                }

                Collider collider = surface.GetComponent<Collider>();
                if (collider != null && !collider.isTrigger)
                {
                    collider.isTrigger = true;
                    modified = true;
                }

                so.ApplyModifiedProperties();
            }

            VRDrawing.Rendering.MeshStrokeRenderer renderer = prefab.GetComponentInChildren<VRDrawing.Rendering.MeshStrokeRenderer>();
            if (renderer == null)
            {
                if (surface != null)
                {
                    surface.gameObject.AddComponent<VRDrawing.Rendering.MeshStrokeRenderer>();
                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        private static void ValidatePenPrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return;

            bool modified = false;

            PenTool penTool = prefab.GetComponent<PenTool>();
            if (penTool != null)
            {
                SerializedObject so = new SerializedObject(penTool);

                Transform toolTip = prefab.transform.Find("ToolTip");
                if (toolTip != null)
                {
                    so.FindProperty("toolTip").objectReferenceValue = toolTip;

                    SphereCollider tipCollider = toolTip.GetComponent<SphereCollider>();
                    if (tipCollider != null && !tipCollider.isTrigger)
                    {
                        tipCollider.isTrigger = true;
                        modified = true;
                    }

                    so.FindProperty("tipRadius").floatValue = 0.005f;
                    so.FindProperty("drawingSurfaceLayer").intValue = LayerMask.GetMask("Drawing Surface");
                }

                so.ApplyModifiedProperties();
                modified = true;
            }

            VRDrawing.Tools.ToolTipCollisionDetector detector = prefab.GetComponentInChildren<VRDrawing.Tools.ToolTipCollisionDetector>();
            if (detector != null)
            {
                SerializedObject so = new SerializedObject(detector);

                Transform toolTip = prefab.transform.Find("ToolTip");
                if (toolTip != null)
                {
                    so.FindProperty("toolTip").objectReferenceValue = toolTip;
                }

                PenTool tool = prefab.GetComponent<PenTool>();
                if (tool != null)
                {
                    so.FindProperty("drawingTool").objectReferenceValue = tool;
                }

                so.ApplyModifiedProperties();
                modified = true;
            }

            Rigidbody rb = prefab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = false;
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        private static void ValidateToolPanelPrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return;

            DrawingToolPanel panel = prefab.GetComponent<DrawingToolPanel>();
            if (panel != null)
            {
                SerializedObject so = new SerializedObject(panel);

                AutoAssignButton(so, "penButton", prefab.transform, "PenButton");
                AutoAssignButton(so, "eraserButton", prefab.transform, "EraserButton");
                AutoAssignButton(so, "undoButton", prefab.transform, "UndoButton");
                AutoAssignButton(so, "clearButton", prefab.transform, "ClearButton");

                Transform colorPanel = prefab.transform.Find("ColorPanel");
                if (colorPanel != null)
                {
                    int colorCount = colorPanel.childCount;
                    SerializedProperty colorButtons = so.FindProperty("colorButtons");
                    colorButtons.arraySize = colorCount;

                    for (int i = 0; i < colorCount; i++)
                    {
                        Transform colorBtn = colorPanel.GetChild(i);
                        var button = colorBtn.GetComponent<UnityEngine.UI.Button>();
                        if (button != null)
                        {
                            colorButtons.GetArrayElementAtIndex(i).objectReferenceValue = button;
                        }
                    }
                }

                Transform slider = prefab.transform.Find("ThicknessSlider");
                if (slider != null)
                {
                    so.FindProperty("thicknessSlider").objectReferenceValue = slider.GetComponent<UnityEngine.UI.Slider>();
                }

                so.ApplyModifiedProperties();
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        private static void AutoAssignButton(SerializedObject so, string propertyName, Transform root, string objectName)
        {
            Transform btnTransform = root.Find(objectName);
            if (btnTransform != null)
            {
                var button = btnTransform.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = button;
                }
            }
        }
    }
}
