using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.Playables;using UnityEngine.Animations;
public static class DiagnoseRetargetPlayback {
 public static string Run(){
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Presentation/Classes/Shinobi.prefab"));
 var a=go.GetComponentInChildren<Animator>();a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Presentation/Thymesia/Thymesia_ShinobiGuard.anim");
 var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"pose",a);output.SetSourcePlayable(p);graph.Play();p.SetTime(0.3);graph.Evaluate(0);
 try {using(var h=new HumanPoseHandler(a.avatar,a.transform)){var pose=new HumanPose();h.GetHumanPose(ref pose);return string.Join(";",pose.muscles.Select((v,i)=>new{v,i,n=HumanTrait.MuscleName[i]}).Where(x=>x.n.Contains("Arm")||x.n.Contains("Forearm")).Select(x=>x.n+" actual="+x.v+" curve="+AnimationUtility.GetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),x.n)).Evaluate(0.3f)))+"\n"+string.Join(";",new[]{HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightHand}.Select(b=>b+"="+a.GetBoneTransform(b).position));}}
 finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(go);}
 }
}



