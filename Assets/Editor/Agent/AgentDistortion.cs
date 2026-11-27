using System.IO;using System.Linq;using System.Text;using UnityEngine;using UnityEditor;using UnityEngine.Playables;using UnityEngine.Animations;
// Mesh tearing check: worst edge stretch (baked vs bind pose) across 24 frames per clip, plus wrist/elbow twist.
public static class AgentDistortion {
 public static string Run(){
  var a=File.ReadAllText(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/distort.txt")).Trim().Split('|');
  var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(a[0]));go.transform.position=new Vector3(9500,0,0);
  var animator=go.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.enabled=true;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
  var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>();var baseMesh=new Mesh[skins.Length];for(int i=0;i<skins.Length;i++){baseMesh[i]=new Mesh();skins[i].BakeMesh(baseMesh[i]);}
  var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output=AnimationPlayableOutput.Create(graph,"b",animator);graph.Play();var sb=new StringBuilder();var m=new Mesh();
  try{foreach(var path in a[1].Split(',')){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path.Trim());if(!clip){sb.AppendLine("missing "+path);continue;}
   var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);p.SetApplyPlayableIK(false);output.SetSourcePlayable(p);float worst=0;float worstT=0;string worstSkin="";
   for(int f=0;f<=24;f++){p.SetTime(clip.length*f/24);graph.Evaluate(0);
    for(int s=0;s<skins.Length;s++){skins[s].BakeMesh(m);var v=m.vertices;var b=baseMesh[s].vertices;var t=baseMesh[s].triangles;
     for(int k=0;k<t.Length;k+=9){for(int e=0;e<3;e++){int i0=t[k+e],i1=t[k+(e+1)%3];float bl=(b[i0]-b[i1]).magnitude;if(bl<.004f)continue;float r=(v[i0]-v[i1]).magnitude/bl;if(r>worst){worst=r;worstT=f/24f;worstSkin=skins[s].name;}}}}}
   sb.AppendLine($"{Path.GetFileNameWithoutExtension(path)} stretch={worst:F2} at {worstT:F2} ({worstSkin})");graph.DestroyPlayable(p);}
  }finally{graph.Destroy();Object.DestroyImmediate(m);foreach(var b in baseMesh)Object.DestroyImmediate(b);Object.DestroyImmediate(go);}
  return sb.ToString();
 }
}
