using UnityEngine;
using UnityEditor;
using VRDrawing.Geology;
using VRDrawing.Mode;
using System.IO;

namespace VRDrawing.Editor
{
    /// <summary>
    /// Editor helper that wires the GeologicalAnnotationManager into the scene
    /// and assigns the annotation canvas prefabs to DrawingModeManager.
    ///
    /// Menu: Tools > Geology > Setup Annotation System In Scene
    /// </summary>
    public static class AnnotationSystemSetupHelper
    {
        private const string PrefabDir            = "Assets/Prefabs/Drawing";
        private const string SymbolPalettePrefab  = "Assets/Prefabs/Drawing/SymbolPaletteCanvas.prefab";
        private const string LegendCanvasPrefab   = "Assets/Prefabs/Drawing/AnnotationLegendCanvas.prefab";
        private const string DatabasePath         = "Assets/Resources/Geology/GeologicalSymbolDatabase.asset";
        private const string ManagerName          = "GeologicalAnnotationManager";

        [MenuItem("Tools/Geology/Setup Annotation System In Scene")]
        public static void SetupInScene()
        {
            EnsureDatabase();
            EnsureAnnotationManager();
            WireDrawingModeManager();

            EditorUtility.DisplayDialog("Done",
                "GeologicalAnnotationManager is set up in the scene.\n" +
                "DrawingModeManager annotation canvas references have been assigned.",
                "OK");
        }

        // ── Database ─────────────────────────────────────────────────────────

        private static void EnsureDatabase()
        {
            GeologicalSymbolDatabase db =
                AssetDatabase.LoadAssetAtPath<GeologicalSymbolDatabase>(DatabasePath);

            if (db == null)
            {
                Debug.LogWarning(
                    "[AnnotationSystemSetupHelper] GeologicalSymbolDatabase not found at " + DatabasePath +
                    ". Run Tools > Geology > Generate Symbol Assets first.");
            }
        }

        // ── Scene manager ─────────────────────────────────────────────────────

        private static GeologicalAnnotationManager EnsureAnnotationManager()
        {
            GeologicalAnnotationManager existing =
                Object.FindFirstObjectByType<GeologicalAnnotationManager>();

            if (existing != null)
            {
                AssignDatabase(existing);
                return existing;
            }

            DrawingModeManager dmm = Object.FindFirstObjectByType<DrawingModeManager>();
            GameObject host = dmm != null ? dmm.gameObject : new GameObject(ManagerName);

            GeologicalAnnotationManager manager = host.AddComponent<GeologicalAnnotationManager>();
            AssignDatabase(manager);

            EditorUtility.SetDirty(host);
            return manager;
        }

        private static void AssignDatabase(GeologicalAnnotationManager manager)
        {
            GeologicalSymbolDatabase db =
                AssetDatabase.LoadAssetAtPath<GeologicalSymbolDatabase>(DatabasePath);
            if (db == null) return;

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty dbProp = so.FindProperty("database");
            if (dbProp != null && dbProp.objectReferenceValue == null)
            {
                dbProp.objectReferenceValue = db;
                so.ApplyModifiedProperties();
            }
        }

        // ── DrawingModeManager wiring ─────────────────────────────────────────

        private static void WireDrawingModeManager()
        {
            DrawingModeManager dmm = Object.FindFirstObjectByType<DrawingModeManager>();
            if (dmm == null)
            {
                Debug.LogWarning("[AnnotationSystemSetupHelper] DrawingModeManager not found in scene. Skipping canvas prefab assignment.");
                return;
            }

            GameObject palettePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SymbolPalettePrefab);
            GameObject legendPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(LegendCanvasPrefab);

            if (palettePrefab == null)
                Debug.LogWarning($"[AnnotationSystemSetupHelper] SymbolPaletteCanvas prefab not found at {SymbolPalettePrefab}. Run 'Generate Annotation Canvas Prefabs' first.");

            if (legendPrefab == null)
                Debug.LogWarning($"[AnnotationSystemSetupHelper] AnnotationLegendCanvas prefab not found at {LegendCanvasPrefab}. Run 'Generate Annotation Canvas Prefabs' first.");

            SerializedObject so = new SerializedObject(dmm);

            SerializedProperty paletteProp = so.FindProperty("symbolPaletteCanvasPrefab");
            SerializedProperty legendProp  = so.FindProperty("annotationLegendCanvasPrefab");

            bool changed = false;

            if (paletteProp != null && palettePrefab != null && paletteProp.objectReferenceValue == null)
            {
                paletteProp.objectReferenceValue = palettePrefab;
                changed = true;
            }

            if (legendProp != null && legendPrefab != null && legendProp.objectReferenceValue == null)
            {
                legendProp.objectReferenceValue = legendPrefab;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dmm);
            }
        }
    }
}
