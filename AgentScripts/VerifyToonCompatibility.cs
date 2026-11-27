using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public static class VerifyToonCompatibility
{
    public static string Run()
    {
        var report = new StringBuilder();
        foreach (var name in new[] { "Toon", "Toon(Tessellation)" })
        {
            var shader = Shader.Find(name);
            if (!shader) throw new Exception("Missing shader: " + name);
            var path = AssetDatabase.GetAssetPath(shader);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var data = ShaderUtil.GetShaderData(shader);
            int compiled = 0;
            for (int s = 0; s < data.SubshaderCount; s++)
            {
                var sub = data.GetSubshader(s);
                for (int p = 0; p < sub.PassCount; p++)
                {
                    var pass = sub.GetPass(p);
                    if (pass.Name != "ForwardLit") continue;
                    var result = pass.CompileVariant(ShaderType.Fragment,
                        new[] { "_EMISSIVE_SIMPLE", "_IS_ANGELRING_OFF", "_IS_CLIPPING_OFF", "_IS_TRANSCLIPPING_OFF" },
                        ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64);
                    if (!result.Success) throw new Exception(name + ": " + string.Join("; ", result.Messages.Select(m => m.message)));
                    compiled++;
                }
            }
            if (compiled == 0) throw new Exception("No ForwardLit pass: " + name);
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == ShaderCompilerMessageSeverity.Error).ToArray();
            if (errors.Length > 0) throw new Exception(string.Join("; ", errors.Select(m => m.message)));
            report.AppendLine(name + ": PASS D3D ForwardLit, " + compiled + " pass(es), " + path);
        }
        Debug.Log(report.ToString());
        return report.ToString();
    }
}
