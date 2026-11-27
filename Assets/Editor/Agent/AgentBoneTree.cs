using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
public static class AgentBoneTree{public static string Run(){var p=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();var root=PrefabUtility.LoadPrefabContents(p);var sb=new StringBuilder();
 var arm=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Armature");
 foreach(var t in arm.GetComponentsInChildren<Transform>(true)){int d=0;for(var x=t;x!=arm;x=x.parent)d++;sb.AppendLine(new string(' ',d)+t.name+" pos="+t.position.ToString("F2")+" rot="+t.localEulerAngles.ToString("F0"));}
 sb.AppendLine("root euler "+arm.parent.localEulerAngles+" scale "+arm.parent.localScale);PrefabUtility.UnloadPrefabContents(root);return sb.ToString();}}
