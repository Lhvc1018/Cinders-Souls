using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
/// Project-wide material audit: shaders that render magenta in URP (missing, error, built-in Standard / Legacy / Particles),
/// grouped by folder. Fix() upgrades them to the URP equivalents keeping the main texture, colour and emission.
public static class AgentMaterials
{
    static bool Broken(Shader s) => !s || !s.isSupported || s.name == "Hidden/InternalErrorShader" || s.name == "Standard" || s.name == "Standard (Specular setup)" || s.name.StartsWith("Legacy Shaders/") || s.name.StartsWith("Particles/") || s.name.StartsWith("Mobile/");
    public static string Run() => Go(false);
    /// Runs Unity's own Built-in -> URP material converter (keeps metallic/normal/occlusion maps), then reports what is left.
    public static string Convert()
    {
        try
        {
            UnityEditor.Rendering.Universal.Converters.RunInBatchMode(UnityEditor.Rendering.Universal.ConverterContainerId.BuiltInToURP,
                new System.Collections.Generic.List<UnityEditor.Rendering.Universal.ConverterId> { UnityEditor.Rendering.Universal.ConverterId.Material }, UnityEditor.Rendering.Universal.ConverterFilter.Inclusive);
        }
        catch (System.Exception e) { return "converter failed: " + e.Message + "\n" + Run(); }
        AssetDatabase.SaveAssets(); return "converter done\n" + Run();
    }
    public static string Fix() => Go(true);
    static string Go(bool fix)
    {
        var sb = new StringBuilder(); int n = 0, fixedCount = 0; var byShader = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); if (path.Contains("/Editor/") || path.Contains("_to_delete")) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (!m || !Broken(m.shader)) continue;
            n++; string sn = m.shader ? m.shader.name : "null"; byShader[sn] = byShader.TryGetValue(sn, out var c) ? c + 1 : 1;
            if (n <= 60) sb.AppendLine($"{sn} | {path}");
            if (!fix) continue;
            bool particle = sn.Contains("Particle") || sn.Contains("Additive") || sn.Contains("Alpha Blended") || path.ToLower().Contains("effect") || path.ToLower().Contains("fx");
            var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null; var col = m.HasProperty("_Color") ? m.GetColor("_Color") : m.HasProperty("_TintColor") ? m.GetColor("_TintColor") * 2 : Color.white;
            var emis = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black; var emTex = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
            bool additive = sn.Contains("Additive");
            m.shader = Shader.Find(particle ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit");
            if (tex) m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", col);
            if (particle)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", additive ? 2 : 0); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)); m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else if (emis.maxColorComponent > .01f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emis); if (emTex) m.SetTexture("_EmissionMap", emTex); }
            EditorUtility.SetDirty(m); fixedCount++;
        }
        if (fix) AssetDatabase.SaveAssets();
        return $"broken={n} fixed={fixedCount}\n" + string.Join("\n", byShader.Select(k => $"  {k.Key}: {k.Value}")) + "\n" + sb;
    }
}
