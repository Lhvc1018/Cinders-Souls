using UnityEditor;
using UnityEngine;
/// Shaders that gameplay code creates with Shader.Find are stripped from player builds when no asset uses them
/// (the guest's glint threw "Value cannot be null: shader"). These Resources materials keep each shader and the
/// keyword variants the code switches on (transparent alpha / additive, emission) inside every build.
public static class AgentShaderKeep
{
    const string Dir = "Assets/Resources/ShaderKeep";
    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Resources", "ShaderKeep");
        int n = 0;
        void Make(string name, string shader, bool transparent, bool additive, bool emission)
        {
            var s = Shader.Find(shader); if (!s) { Debug.LogWarning("missing " + shader); return; }
            var m = new Material(s) { name = name };
            if (transparent)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", additive ? 2 : 0); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)); m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); if (additive) m.EnableKeyword("_BLENDMODE_ADD");
            }
            if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white); }
            var path = $"{Dir}/{name}.mat"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path); n++;
        }
        Make("Unlit", "Universal Render Pipeline/Unlit", false, false, false);
        Make("Unlit Alpha", "Universal Render Pipeline/Unlit", true, false, false);
        Make("Unlit Additive", "Universal Render Pipeline/Unlit", true, true, false);
        Make("Particles Alpha", "Universal Render Pipeline/Particles/Unlit", true, false, false);
        Make("Particles Additive", "Universal Render Pipeline/Particles/Unlit", true, true, false);
        Make("Particles Opaque", "Universal Render Pipeline/Particles/Unlit", false, false, false);
        Make("Lit", "Universal Render Pipeline/Lit", false, false, false);
        Make("Lit Emission", "Universal Render Pipeline/Lit", false, false, true);
        Make("Lit Transparent", "Universal Render Pipeline/Lit", true, false, false);
        Make("Sprites Default", "Sprites/Default", false, false, false);
        Make("UI Default", "UI/Default", false, false, false);
        AssetDatabase.SaveAssets(); return "shader keep materials: " + n;
    }
}
