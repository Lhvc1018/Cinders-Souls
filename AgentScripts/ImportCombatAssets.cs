using UnityEditor;
using UnityEditor.Compilation;
public static class ImportCombatAssets{public static string Run(){AssetDatabase.ImportAsset("Assets/02_Scripts/Presentation/RitualSurface.cs",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);AssetDatabase.ImportAsset("Assets/02_Scripts/Mechanics/MeleeContact.cs",ImportAssetOptions.ForceUpdate);CompilationPipeline.RequestScriptCompilation();return AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/02_Scripts/Presentation/RitualSurface.cs")?.text.Substring(0,80);}}
