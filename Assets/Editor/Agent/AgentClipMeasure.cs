using System.IO;using System.Linq;using System.Text;using UnityEngine;using UnityEditor;using UnityEngine.Playables;using UnityEngine.Animations;
// For each clip: length, blade-tip speed windows (>45% peak), root travel, and lowest foot (floating check).
public static class AgentClipMeasure {
 public static string Run(){
  var lines=File.ReadAllText(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/measure.txt")).Trim().Split('|');
  string prefab=lines[0];string weapon=lines[1];var clips=lines[2].Split(',');var sb=new StringBuilder();
  var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));go.transform.position=new Vector3(9000,0,0);
  var animator=go.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.enabled=true;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
  var item=new Codice.Characters.WeaponInstance(Codice.Characters.WeaponKind.Sword);item.katana=weapon=="katana";var blade=Codice.Presentation.HumanoidWeaponGrip.Attach(animator,item,false);
  var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output=AnimationPlayableOutput.Create(graph,"b",animator);graph.Play();
  var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var lf=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=animator.GetBoneTransform(HumanBodyBones.RightFoot);
  try{foreach(var path in clips){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path.Trim());if(!clip){sb.AppendLine("missing "+path);continue;}
   var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);p.SetApplyPlayableIK(false);output.SetSourcePlayable(p);int n=60;var sp=new float[n+1];Vector3 prev=Vector3.zero,h0=Vector3.zero;float maxTravel=0,minFoot=99,maxFoot=-99;
   for(int i=0;i<=n;i++){p.SetTime(clip.length*i/n);graph.Evaluate(0);var tip=go.transform.InverseTransformPoint(blade.transform.TransformPoint(Vector3.up*.8f));if(i>0)sp[i]=(tip-prev).magnitude/(clip.length/n);prev=tip;var h=go.transform.InverseTransformPoint(hips.position);if(i==0)h0=h;maxTravel=Mathf.Max(maxTravel,Vector3.ProjectOnPlane(h-h0,Vector3.up).magnitude);float f=Mathf.Min(lf.position.y,rf.position.y)-go.transform.position.y;minFoot=Mathf.Min(minFoot,f);maxFoot=Mathf.Max(maxFoot,f);}
   float peak=sp.Max();var hot=Enumerable.Range(0,n+1).Where(i=>sp[i]>=peak*.45f).Select(i=>i/(float)n).ToArray();
   // contiguous bursts
   var bursts=new System.Collections.Generic.List<string>();float bs=-1,last=-1;foreach(var t in hot){if(bs<0){bs=t;last=t;continue;}if(t-last>.05f){bursts.Add(bs.ToString("F2")+"-"+last.ToString("F2"));bs=t;}last=t;}if(bs>=0)bursts.Add(bs.ToString("F2")+"-"+last.ToString("F2"));
   sb.AppendLine($"{Path.GetFileNameWithoutExtension(path)} len={clip.length:F2} peak={peak:F0} bursts=[{string.Join(" ",bursts)}] travel={maxTravel:F2} foot={minFoot:F2}..{maxFoot:F2}");graph.DestroyPlayable(p);}
  }finally{graph.Destroy();Object.DestroyImmediate(go);}
  return sb.ToString();
 }
}
