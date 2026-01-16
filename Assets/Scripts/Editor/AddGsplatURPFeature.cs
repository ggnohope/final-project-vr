using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Collections.Generic;

public static class AddGsplatURPFeature
{
    [MenuItem("Tools/Gsplat/Add Gsplat URP Feature to Renderers")]
    public static void AddFeatureToRenderers()
    {
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        int addedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

            if (rendererData == null) continue;

            bool hasGsplatFeature = false;
            var featuresProp = rendererData.GetType().GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            
            if (featuresProp != null)
            {
                var features = featuresProp.GetValue(rendererData) as List<ScriptableRendererFeature>;
                if (features != null)
                {
                    foreach (var feature in features)
                    {
                        if (feature != null && feature.GetType().Name.Contains("Gsplat"))
                        {
                            hasGsplatFeature = true;
                            break;
                        }
                    }
                }
            }

            if (!hasGsplatFeature)
            {
                var gsplatFeatureType = System.Type.GetType("Gsplat.GsplatURPFeature, Gsplat");
                if (gsplatFeatureType != null)
                {
                    var feature = ScriptableObject.CreateInstance(gsplatFeatureType) as ScriptableRendererFeature;
                    if (feature != null)
                    {
                        feature.name = "Gsplat URP Feature";
                        
                        var addFeatureMethod = rendererData.GetType().GetMethod("AddRendererFeature", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (addFeatureMethod != null)
                        {
                            addFeatureMethod.Invoke(rendererData, new object[] { feature });
                            EditorUtility.SetDirty(rendererData);
                            Debug.Log($"Added Gsplat URP Feature to: {path}");
                            addedCount++;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Gsplat.GsplatURPFeature type not found. Make sure the package is properly installed and GSPLAT_ENABLE_URP define is set.");
                }
            }
            else
            {
                Debug.Log($"Gsplat URP Feature already exists in: {path}");
            }
        }

        if (addedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Successfully added Gsplat URP Feature to {addedCount} renderer(s)!");
        }
        else
        {
            Debug.Log("No changes needed - all renderers already have Gsplat URP Feature or it's already configured.");
        }
    }
}
