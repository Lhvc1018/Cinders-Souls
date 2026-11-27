using UnityEditor;
using UnityEditor.Compilation;
public static class ReimportRitual{public static string Run(){var p="Assets/02_Scripts/Presentation/RitualSurfaceRuntime.cs";var error=AssetDatabase.MoveAsset(p,"Assets/02_Scripts/Presentation/RitualSurface.cs");AssetDatabase.Refresh();CompilationPipeline.RequestScriptCompilation();return error;}}

