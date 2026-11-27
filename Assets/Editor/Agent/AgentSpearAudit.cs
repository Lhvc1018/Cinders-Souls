using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;

public static class AgentSpearAudit {
 public static IEnumerable<Vector3> Surface(SkinnedMeshRenderer s){var mesh=s.sharedMesh;var matrices=s.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var pts=mesh.vertices;var weights=mesh.boneWeights;for(int i=0;i<pts.Length;i++){var w=weights[i];yield return matrices[w.boneIndex0].MultiplyPoint3x4(pts[i])*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(pts[i])*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(pts[i])*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(pts[i])*w.weight3;}} public static string Run(){
  var rows=new List<string>();
  foreach(var name in new[]{"Lanza del Infierno","Duende Mutante","Gurdi del Abismo"}){
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ExpeditionEnemies/"+name+".prefab");
   var go=UnityEngine.Object.Instantiate(asset);
   var animator=go.GetComponentInChildren<Animator>(); animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var clips=animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
   animator.applyRootMotion=false;var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   var output=AnimationPlayableOutput.Create(graph,"boss",animator);graph.Play();
   var mesh=new Mesh();
   try{
    foreach(var clip in clips){
     var pl=AnimationClipPlayable.Create(graph,clip);output.SetSourcePlayable(pl);
     float min=999,max=-999,weapon=999;
     for(int i=0;i<=24;i++){pl.SetTime(clip.length*i/24);graph.Evaluate(0);
      var body=go.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s=>{return Surface(s).Min(v=>go.transform.InverseTransformPoint(v).y);}).Min();
      if(i==0&&clip.name=="Idle")foreach(var r in go.GetComponentsInChildren<Renderer>()){var skin=r as SkinnedMeshRenderer;var mf=r.GetComponent<MeshFilter>();if(!skin&&!mf)continue;if(skin)skin.BakeMesh(mesh);var verts=(skin?Surface(skin):mf.sharedMesh.vertices.Select(r.transform.TransformPoint)).Select(go.transform.InverseTransformPoint).ToArray();if(verts.Length==0)continue;rows.Add(r.name+" scale="+r.transform.lossyScale+" bounds="+r.bounds+" foot="+string.Join(",",go.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Foot.")).Select(t=>t.position.y))+" min="+verts.Min(v=>v.y)+" max="+verts.Max(v=>v.y)+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")));}min=Mathf.Min(min,body);max=Mathf.Max(max,body);
      foreach(var filter in go.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.Contains("Spear")))weapon=Mathf.Min(weapon,filter.sharedMesh.vertices.Min(v=>go.transform.InverseTransformPoint(filter.transform.TransformPoint(v)).y));
     }
     rows.Add(name+" "+clip.name+" body="+min.ToString("F2")+".."+max.ToString("F2")+" spear="+weapon.ToString("F2"));
     graph.DestroyPlayable(pl);
    }
   }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(go);}
  }
  return string.Join("\n",rows);
 }
}




