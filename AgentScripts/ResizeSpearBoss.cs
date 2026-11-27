using System;using UnityEngine;using UnityEngine.AI;using UnityEditor;using Codice.Bosses;
public static class ResizeSpearBoss {
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode");
  string path="Assets/Resources/ExpeditionEnemies/Lanza del Infierno.prefab";
  var root=PrefabUtility.LoadPrefabContents(path);
  try{
   var boss=root.GetComponent<BossCharacter>();var capsule=root.GetComponent<CapsuleCollider>();float factor=2.4f/capsule.height;
   boss.visual.localScale*=factor;boss.visual.localPosition*=factor;
   capsule.height=2.4f;capsule.center=Vector3.up*1.2f;capsule.radius=.5f;
   var agent=root.GetComponent<NavMeshAgent>();agent.height=2.4f;agent.radius=.5f;agent.baseOffset=0;
   PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
   return "Lanza del Infierno: 3.2m -> 2.4m (-25%); native skeleton and attached spear scaled together; collider and agent updated.";
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}
