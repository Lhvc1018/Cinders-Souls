using UnityEditor.Compilation;
using System.Linq;
public static class CompilationInventory{public static string Run(){return string.Join("\n",CompilationPipeline.GetAssemblies().SelectMany(a=>a.sourceFiles.Where(f=>f.Contains("RitualSurface")||f.Contains("MeleeContact")).Select(f=>a.name+":"+f)));}}
