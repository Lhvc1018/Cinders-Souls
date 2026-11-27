using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.Playables;using UnityEngine.Animations;
public static class AuditClassAnimations{
 public static string Run(){
  var report=new List<string>();int total=0;
  foreach(string role in new[]{"Warrior","Tank","Archer","Mage","Shinobi","Berserker"}){
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Presentation/Classes/"+role+".prefab"));
   var anim=root.GetComponentInChildren<Animator>();var controller=Codice.Presentation.ClassAnimationControllers.Load((Codice.Characters.PlayerClass)Enum.Parse(typeof(Codice.Characters.PlayerClass),role));
   anim.applyRootMotion=false;anim.runtimeAnimatorController=null;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var graph=PlayableGraph.Create("Class animation audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output=AnimationPlayableOutput.Create(graph,"body",anim);graph.Play();
   var clips=controller.animationClips.Distinct().ToArray();var warnings=new List<string>();
   try{foreach(var clip in clips){
    if(!clip.isHumanMotion)throw new Exception(role+" has non-humanoid clip "+clip.name);
    var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);output.SetSourcePlayable(playable);float maxLean=0;
    for(int f=0;f<16;f++){
     playable.SetTime(clip.length*f/16);graph.Evaluate(0);var hips=anim.GetBoneTransform(HumanBodyBones.Hips);var head=anim.GetBoneTransform(HumanBodyBones.Head);float lean=Vector3.Angle(head.position-hips.position,Vector3.up);maxLean=Mathf.Max(maxLean,lean);
     foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh);if(mesh.vertices.Any(v=>float.IsNaN(v.x)||float.IsInfinity(v.x)||v.magnitude>10))throw new Exception(role+" invalid mesh: "+clip.name);UnityEngine.Object.DestroyImmediate(mesh);}
     total++;
    }
    bool attack=(clip.name.StartsWith("Lords_")&&!clip.name.StartsWith("Lords_Run"))||(clip.name.StartsWith("Thymesia_")&&(clip.name.Contains("Slash")||clip.name.Contains("DualSlice")||clip.name.Contains("DualStab")||clip.name.Contains("SwordCombo")));
    if(attack&&maxLean>42)throw new Exception(role+" excessive attack torso lean "+clip.name+" "+maxLean);
    report.Add(role+" / "+clip.name+" / max torso lean "+maxLean.ToString("F1")+" degrees / finite mesh");graph.DestroyPlayable(playable);
   }}finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(root);}
  }
  File.WriteAllLines("D:/67/CindersBackups/2026-11-08-thymesia-thirdfloor/class-animation-audit.txt",report);
  string result="PASS six classes, "+report.Count+" class/clip combinations, "+total+" poses; imported active attacks below 42 degrees torso lean; no invalid mesh samples.";Debug.Log("[AnimationAudit] "+result);return result;
 }
}

