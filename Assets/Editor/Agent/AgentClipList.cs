using System.Linq;using UnityEditor;using UnityEngine;
public static class AgentClipList{public static string Run(){var p=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();var all=AssetDatabase.LoadAllAssetsAtPath(p);
 var imp=AssetImporter.GetAtPath(p) as ModelImporter;return "type="+(imp?imp.animationType.ToString():"-")+" avatar="+all.OfType<Avatar>().Count()+" clips: "+string.Join(", ",all.OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).Select(c=>c.name+"("+c.length.ToString("0.0")+"s)"));}}
