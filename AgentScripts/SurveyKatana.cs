using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Codice.Presentation;
using Codice.Characters;
public static class SurveyKatana {
 public static string Run(){
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Presentation/Classes/Shinobi.prefab"));
  var a=go.GetComponentInChildren<Animator>(); a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  var g=PlayableGraph.Create();g.SetTimeUpdateMode(DirectorUpdateMode.Manual);var o=AnimationPlayableOutput.Create(g,"sample",a);g.Play();
  var w=HumanoidWeaponGrip.Attach(a,new WeaponInstance(WeaponKind.Sword){katana=true},false).transform;
  var t=new List<string>();var candidates=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/EEJANAI_Team/FreeSwordAnimations/Animations"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("/slash")&&p.EndsWith(".anim")).ToList();
  candidates.AddRange(System.IO.Directory.GetFiles("Assets/03_Models/Mixamo/GreatSword","*slash*.fbx"));candidates.AddRange(System.IO.Directory.GetFiles("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H","*Attack*.fbx"));
  candidates = System.IO.Directory.GetFiles("Assets/Resources/Presentation/Human","Shinobi_Katana*.anim").ToList(); foreach(var path in candidates){var c=AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\','/')).OfType<AnimationClip>().FirstOrDefault(x=>x.isHumanMotion&&!x.name.StartsWith("__"));if(!c)continue;
   var p=AnimationClipPlayable.Create(g,c);o.SetSourcePlayable(p);var points=new List<string>();float foot=0,lean=0,spread=0;for(int i=0;i<=20;i++){
    p.SetTime(c.length*i/20f);g.Evaluate(0);var tip=w.position+w.up*.9f;var l=a.GetBoneTransform(HumanBodyBones.LeftFoot);var r=a.GetBoneTransform(HumanBodyBones.RightFoot);
    foot=Mathf.Max(foot,Mathf.Min(l.position.y,r.position.y));lean=Mathf.Max(lean,Vector3.Angle(Vector3.up,a.GetBoneTransform(HumanBodyBones.Head).position-a.GetBoneTransform(HumanBodyBones.Hips).position));spread=Mathf.Max(spread,(HumanoidWeaponGrip.Socket(a,true).position-w.position).magnitude);
    points.Add("feet="+Mathf.Min(l.position.y,r.position.y).ToString("F2")+" pelvisY="+a.GetBoneTransform(HumanBodyBones.Hips).position.y.ToString("F2")+" root="+a.transform.position+" rot="+a.transform.eulerAngles);
   }t.Add(System.IO.Path.GetFileNameWithoutExtension(path)+" "+c.length.ToString("F2")+" lean "+lean.ToString("F0")+" foot "+foot.ToString("F2")+" hands "+spread.ToString("F2")+" tip(x,z): "+string.Join(" ",points));g.DestroyPlayable(p);}
  g.Destroy();UnityEngine.Object.DestroyImmediate(go);return string.Join("\n",t);
 }}



