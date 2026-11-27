using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class AuditRequestedCombat {
 public static string Run(){
  var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ExpeditionEnemies/AngelKnight.prefab"));
  try{
   var a=root.GetComponentInChildren<Animator>(true);var c=(AnimatorController)a.runtimeAnimatorController;
   string report="";
   foreach(var state in c.layers[0].stateMachine.states){var clip=state.state.motion as AnimationClip;if(!clip)continue;clip.SampleAnimation(a.gameObject,clip.length*.3f);
    var mesh=new Mesh();Bounds b=default;bool first=true;
    foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.BakeMesh(mesh);foreach(var v in mesh.vertices){var w=r.transform.TransformPoint(v);if(first){b=new Bounds(w,Vector3.zero);first=false;}else b.Encapsulate(w);}}
    UnityEngine.Object.DestroyImmediate(mesh);report+=state.state.name+" bounds="+b+" activeSkins="+root.GetComponentsInChildren<SkinnedMeshRenderer>().Length+"\n";
   }return report;
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
