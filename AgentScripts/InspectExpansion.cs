using System;using System.Linq;using UnityEngine;using UnityEditor;
public static class InspectExpansion {
 public static string Run(){
 var paths=new[]{"Assets/Angel_Knight_Anims/Meshs/Angel Knight/LowPoly_AngelKnight.fbx","Assets/EEJANAI_Team/FreeSwordAnimations/Animations/slash1.anim","Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX"};
 var result="";foreach(var p in paths){result+=p+"\n";foreach(var c in AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")))result+=$"{c.name}|{c.length:0.00}|human={c.isHumanMotion}\n";}
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Angel_Knight_Anims/Prefabs/LowPoly_AngelKnight.prefab");
 result+="COMPONENTS "+string.Join(",",prefab.GetComponentsInChildren<Component>(true).Where(c=>c&&!(c is Transform)).Select(c=>c.GetType().Name).Distinct());
 foreach(var a in prefab.GetComponentsInChildren<Animator>(true))result+=$"\nAnimator {a.name} human={a.isHuman} controller={a.runtimeAnimatorController?.name}";
 result+="\nMaterials "+string.Join(",",prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(m=>m.name+":"+m.shader.name));
 return result;
 }
}
