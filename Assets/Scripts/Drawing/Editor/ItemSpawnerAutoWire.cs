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
            {
                so.FindProperty("drawingBoardActivatorPrefab").objectReferenceValue = activatorPrefab;
                Debug.Log("✓ Assigned Drawing Board Activator Prefab");
            }
            else
            {
                Debug.LogWarning("✗ Drawing Board Activator Prefab not found at Assets/Prefabs/Drawing/DrawingBoardActivator.prefab");
            }

            GameObject penPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drawing/Pen.prefab");
            if (penPrefab != null)
            {
                so.FindProperty("penPrefab").objectReferenceValue = penPrefab;
                Debug.Log("✓ Assigned Pen Prefab");
            }
            else
            {
                Debug.LogWarning("✗ Pen Prefab not found at Assets/Prefabs/Drawing/Pen.prefab");
            }

            Transform spawnPoint = spawner.transform.Find("SpawnPoint");
            if (spawnPoint == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    so.FindProperty("spawnPoint").objectReferenceValue = mainCam.transform;
                    Debug.Log("✓ Assigned Main Camera as Spawn Point");
                }
            }

            ItemBarController itemBar = FindFirstObjectByType<ItemBarController>();
            if (itemBar != null)
            {
                so.FindProperty("itemBar").objectReferenceValue = itemBar;
                Debug.Log("✓ Assigned Item Bar Controller");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);

            Debug.Log("=== ItemSpawner Auto-Wire Complete ===");
        }

        private void ValidateReferences(ItemSpawner spawner)
        {
            SerializedObject so = new SerializedObject(spawner);

            bool allValid = true;

            if (so.FindProperty("drawingBoardActivatorPrefab").objectReferenceValue == null)
            {
                Debug.LogWarning("✗ Drawing Board Activator Prefab is not assigned");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ Drawing Board Activator Prefab assigned");
            }

            if (so.FindProperty("penPrefab").objectReferenceValue == null)
            {
                Debug.LogWarning("✗ Pen Prefab is not assigned");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ Pen Prefab assigned");
            }

            if (so.FindProperty("spawnPoint").objectReferenceValue == null)
            {
                Debug.LogWarning("✗ Spawn Point is not assigned");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ Spawn Point assigned");
            }

            if (so.FindProperty("itemBar").objectReferenceValue == null)
            {
                Debug.LogWarning("✗ Item Bar is not assigned");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ Item Bar assigned");
            }

            if (allValid)
            {
                Debug.Log("=== All ItemSpawner references are valid ===");
                EditorUtility.DisplayDialog("Validation Complete", "All ItemSpawner references are properly assigned!", "OK");
            }
            else
            {
                Debug.LogWarning("=== Some ItemSpawner references are missing ===");
                EditorUtility.DisplayDialog("Validation Failed", "Some references are missing. Check the Console for details.", "OK");
            }
        }
    }
}
