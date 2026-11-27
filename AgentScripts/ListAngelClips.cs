using System.Linq;using UnityEngine;using UnityEditor;
public static class ListAngelClips {public static string Run()=>string.Join("\n",AssetDatabase.LoadAllAssetsAtPath("Assets/Angel_Knight_Anims/Meshs/Angel Knight/LowPoly_AngelKnight.fbx").OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__")).Select(x=>x.name+" "+x.length));}
