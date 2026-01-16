using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class FixGsplatRenderer
{
    [MenuItem("Tools/Gsplat/Fix PC_Renderer (Clean and Re-add Feature)")]
    public static void FixPCRenderer()
    {
        string path = "Assets/Settings/PC_Renderer.asset";
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

        if (rendererData == null)
        {
            Debug.LogError($"Could not find renderer at: {path}");
            return;
        }

        var so = new SerializedObject(rendererData);
        var featuresProp = so.FindProperty("m_RendererFeatures");
        var featureMapProp = so.FindProperty("m_RendererFeatureMap");

        if (featuresProp != null && featureMapProp != null)
        {
            int originalCount = featuresProp.arraySize;
            
            featuresProp.ClearArray();
            featureMapProp.ClearArray();
            
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Cleared {originalCount} features from {path}");
            Debug.Log("Now manually add 'Gsplat URP Feature' via Inspector:");
            Debug.Log("1. Select PC_Renderer in Project");
            Debug.Log("2. Click 'Add Renderer Feature' (+)");
            Debug.Log("3. Choose 'Gsplat URP Feature'");
            
            EditorGUIUtility.PingObject(rendererData);
            Selection.activeObject = rendererData;
        }
        else
        {
            Debug.LogError("Could not find m_RendererFeatures property!");
        }
    }
    
    [MenuItem("Tools/Gsplat/Check Graphics API")]
    public static void CheckGraphicsAPI()
    {
        var currentAPI = SystemInfo.graphicsDeviceType;
        Debug.Log($"Current Graphics API: {currentAPI}");
        
        if (currentAPI == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12 ||
            currentAPI == UnityEngine.Rendering.GraphicsDeviceType.Vulkan ||
            currentAPI == UnityEngine.Rendering.GraphicsDeviceType.Metal)
        {
            Debug.Log("✅ Graphics API is compatible with Gsplat (supports wave/subgroup operations)");
        }
        else
        {
            Debug.LogWarning($"⚠️ Graphics API '{currentAPI}' may NOT support Gsplat!");
            Debug.LogWarning("Go to Edit > Project Settings > Player > Other Settings");
            Debug.LogWarning("Uncheck 'Auto Graphics API for Windows'");
            Debug.LogWarning("Add 'Direct3D12' or 'Vulkan', remove others");
            Debug.LogWarning("Unity will require restart");
        }
    }
}
