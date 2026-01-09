using UnityEngine;

namespace VRDrawing.Setup
{
    public static class MaterialSetup
    {
        public static Material CreateStrokeMaterial()
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "DrawingStroke";
            mat.enableInstancing = true;
            mat.SetFloat("_Smoothness", 0.1f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.renderQueue = 2000;
            
            return mat;
        }

        public static Material CreateUnlitStrokeMaterial()
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.name = "DrawingStrokeUnlit";
            mat.enableInstancing = true;
            mat.SetColor("_BaseColor", Color.white);
            
            return mat;
        }

        public static Material CreateVertexColorMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.name = "DrawingStrokeVertexColor";
            mat.enableInstancing = true;
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetFloat("_Metallic", 0f);

            return mat;
        }
    }
}
