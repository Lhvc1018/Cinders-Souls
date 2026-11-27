using System.Linq;using System.Text;using UnityEngine;using UnityEditor;using UnityEngine.Playables;using UnityEngine.Animations;
public static class AgentShinobiProbe {
 // Samples right-hand speed through each katana clip on the dressed Shinobi avatar.
 public static string Run(){
  var sb=new StringBuilder();
  var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Presentation/Classes/Shinobi.prefab"));go.transform.position=new Vector3(4000,0,0);
  var animator=go.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.enabled=true;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();sb.AppendLine("animator="+animator.name+" human="+animator.isHuman+" avatar="+(animator.avatar?animator.avatar.name:"none"));
  var item=new Codice.Characters.WeaponInstance(Codice.Characters.WeaponKind.Sword);item.katana=true;var blade=Codice.Presentation.HumanoidWeaponGrip.Attach(animator,item,false);var hand=blade?blade.transform:animator.GetBoneTransform(HumanBodyBones.RightHand);
  var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output=AnimationPlayableOutput.Create(graph,"b",animator);graph.Play();
  var ctrl=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Resources/Presentation/Human/Shinobi.controller");
  sb.AppendLine("clips: "+string.Join(", ",ctrl.animationClips.Select(c=>c.name+"="+c.length.ToString("F2")).Distinct()));
  try{
   foreach(var clip in ctrl.animationClips.Where(c=>c.name.Contains("Katana")||c.name.Contains("Shinobi")).Distinct()){
    var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);p.SetApplyPlayableIK(false);output.SetSourcePlayable(p);
    int n=40;Vector3 prev=Vector3.zero;var speeds=new float[n+1];float minY=99;
    for(int i=0;i<=n;i++){p.SetTime(clip.length*i/n);graph.Evaluate(0);var pos=go.transform.InverseTransformPoint(hand.TransformPoint(Vector3.up*.8f));if(i>0)speeds[i]=(pos-prev).magnitude/(clip.length/n);prev=pos;minY=Mathf.Min(minY,pos.y);}
    float peak=speeds.Max();int start=System.Array.FindIndex(speeds,s=>s>=peak*.45f);int end=System.Array.FindLastIndex(speeds,s=>s>=peak*.45f);
    sb.AppendLine($"{clip.name} len={clip.length:F2} peak={peak:F1}m/s at {System.Array.IndexOf(speeds,peak)/(float)n:F2} window={start/(float)n:F2}-{end/(float)n:F2} handMinY={minY:F2}");
    graph.DestroyPlayable(p);
   }
  }finally{graph.Destroy();Object.DestroyImmediate(go);}
  return sb.ToString();
 }
}
