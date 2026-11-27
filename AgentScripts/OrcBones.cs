using System.Linq;using UnityEngine;using UnityEditor;
public static class OrcBones {public static string Run(){var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Bosses/MonsterOrc.prefab");return string.Join("\n",p.GetComponentsInChildren<Transform>().Where(t=>!t.name.Contains("hand.")).Select(t=>t.name+" "+t.position));}}
