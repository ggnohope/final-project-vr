using Gsplat;
using UnityEngine;
using UnityEngine.Rendering;

public class GsplatDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[Gsplat] Graphics API: {SystemInfo.graphicsDeviceType}");
        Debug.Log($"[Gsplat] GsplatSettings.Valid: {GsplatSettings.Instance.Valid}");
        Debug.Log($"[Gsplat] GsplatSorter.Valid: {GsplatSorter.Instance.Valid}");

        var cs = GsplatSettings.Instance.ComputeShader;
        if (cs != null)
        {
            int[] kernels = {
                cs.FindKernel("InitPayload"),
                cs.FindKernel("CalcDistance"),
                cs.FindKernel("InitDeviceRadixSort"),
                cs.FindKernel("Upsweep"),
                cs.FindKernel("Scan"),
                cs.FindKernel("Downsweep")
            };
            string[] names = { "InitPayload","CalcDistance","InitDeviceRadixSort","Upsweep","Scan","Downsweep" };
            for (int i = 0; i < kernels.Length; i++)
                Debug.Log($"[Gsplat] Kernel '{names[i]}' ({kernels[i]}) IsSupported: {(kernels[i] >= 0 ? cs.IsSupported(kernels[i]).ToString() : "NOT FOUND")}");
        }
        else
        {
            Debug.LogError("[Gsplat] ComputeShader is NULL on this platform!");
        }
    }
}
