using UnityEngine;
using UnityEditor;
using System.Linq;

namespace VRDrawing.Editor
{
    [CustomEditor(typeof(ItemSpawner))]
    public class ItemSpawnerAutoWire : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            ItemSpawner spawner = (ItemSpawner)target;

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Auto-Wire Drawing Prefabs", GUILayout.Height(30)))
            {
                AutoWirePrefabs(spawner);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            if (GUILayout.Button("Validate References", GUILayout.Height(25)))
            {
                ValidateReferences(spawner);
            }
        }

        private void AutoWirePrefabs(ItemSpawner spawner)
        {
            SerializedObject so = new SerializedObject(spawner);

            GameObject activatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drawing/DrawingBoardActivator.prefab");
            if (activatorPrefab != null)
                so.FindProperty("drawingBoardActivatorPrefab").objectReferenceValue = activatorPrefab;

            GameObject penPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drawing/Pen.prefab");
            if (penPrefab != null)
                so.FindProperty("penPrefab").objectReferenceValue = penPrefab;

            Transform spawnPoint = spawner.transform.Find("SpawnPoint");
            if (spawnPoint == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                    so.FindProperty("spawnPoint").objectReferenceValue = mainCam.transform;
            }

            ItemBarController itemBar = FindFirstObjectByType<ItemBarController>();
            if (itemBar != null)
                so.FindProperty("itemBar").objectReferenceValue = itemBar;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
        }

        private void ValidateReferences(ItemSpawner spawner)
        {
            SerializedObject so = new SerializedObject(spawner);

            bool allValid = true;

            if (so.FindProperty("drawingBoardActivatorPrefab").objectReferenceValue == null) allValid = false;
            if (so.FindProperty("penPrefab").objectReferenceValue == null) allValid = false;
            if (so.FindProperty("spawnPoint").objectReferenceValue == null) allValid = false;
            if (so.FindProperty("itemBar").objectReferenceValue == null) allValid = false;

            if (allValid)
                EditorUtility.DisplayDialog("Validation Complete", "All ItemSpawner references are properly assigned!", "OK");
            else
                EditorUtility.DisplayDialog("Validation Failed", "Some references are missing. Check the Inspector.", "OK");
        }
    }
}
