using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using UnityEngine.Animations;using UnityEngine.Playables;
public static class BuildThirdPersonMotion
{
 const string Root="Assets/Resources/Presentation/Human/";
 static AnimatorController controller;static GameObject sample;
 static AnimationClip Make(string name,string path,bool loop=false,bool attack=false,bool mirror=false)
 {
  var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
  if(!source.isHumanMotion)throw new Exception("Not humanoid "+path);
  var clip=UnityEngine.Object.Instantiate(source);clip.name=name;
  if(attack){
   var animator=sample.GetComponent<Animator>();var graph=PlayableGraph.Create("Attack contact sampling");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   var playable=AnimationClipPlayable.Create(graph,source);var output=AnimationPlayableOutput.Create(graph,"Sample",animator);output.SetSourcePlayable(playable);graph.Play();
   float peak=source.length*.4f,maxSpeed=0;Vector3 previous=Vector3.zero;
   for(int i=0;i<=90;i++){float t=source.length*i/90f;playable.SetTime(t);graph.Evaluate(0);Vector3 pos=animator.GetBoneTransform(HumanBodyBones.RightHand).position-animator.GetBoneTransform(HumanBodyBones.Hips).position;float speed=(pos-previous).magnitude;if(i>12&&i<65&&speed>maxSpeed){maxSpeed=speed;peak=t;}previous=pos;}
   graph.Destroy();float start=Mathf.Max(0,peak-.55f),end=Mathf.Min(source.length,peak+.7f);
   foreach(var binding in AnimationUtility.GetCurveBindings(clip)){
    var original=AnimationUtility.GetEditorCurve(clip,binding);var keys=new Keyframe[61];
    for(int i=0;i<=60;i++){float time=i/60f;float src=time<=.36f?Mathf.Lerp(start,peak,time/.36f):Mathf.Lerp(peak,end,(time-.36f)/.64f);keys[i]=new Keyframe(time,original.Evaluate(src));}
    var curve=new AnimationCurve(keys);for(int i=0;i<keys.Length;i++)curve.SmoothTangents(i,0);AnimationUtility.SetEditorCurve(clip,binding,curve);
   }
  }
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.mirror=mirror;settings.loopBlendOrientation=true;settings.loopBlendPositionY=true;settings.loopBlendPositionXZ=true;settings.heightFromFeet=true;settings.keepOriginalPositionY=false;settings.keepOriginalOrientation=true;if(attack){settings.startTime=0;settings.stopTime=1;}
  AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
  foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.propertyName=="RootT.x"||binding.propertyName=="RootT.z"){var curve=AnimationUtility.GetEditorCurve(clip,binding);AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,curve.Evaluate(0)));}
  var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+name+".anim");if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);clip=old;}else AssetDatabase.CreateAsset(clip,Root+name+".anim");return clip;
 }
 static AnimatorState State(string name,Motion motion,bool timed=false){var sm=controller.layers[0].stateMachine;var s=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??sm.AddState(name);s.motion=motion;s.speed=1;s.speedParameter="ActionSpeed";s.speedParameterActive=timed;EditorUtility.SetDirty(s);return s;}
 public static string Run(){
 controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"HumanPlayer.controller");sample=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"HumanPlayer.prefab"));sample.hideFlags=HideFlags.HideAndDontSave;
 try{
  string basePath="Assets/07_Animacion/DoubleL/FBX Unity/One Hand Up/";
  string Attack(string family,int n)=>basePath+"Attack_"+family+"/InPlace/1Hand_Up_Attack_"+family+"_"+n+"_InPlace.fbx";
  var idle=Make("Ready",basePath+"Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx",true);State("Ready",idle);State("DualIdle",Make("DualIdle",basePath+"Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx",true));
  foreach(var spec in new[]{("SwordCombo1","A",1),("SwordCombo2","A",2),("SwordCombo3","A",3),("Melee","A",1),("KatanaSlashA","B",1),("KatanaSlashB","B",2),("DualSlice","A",2),("DualStab","B",3),("HeavyAttack","A",3),("Thrust","B",3)})State(spec.Item1,Make(spec.Item1,Attack(spec.Item2,spec.Item3),false,true),true);
  State("AxeAttack",Make("AxeAttack",Attack("B",1),false,true),true);
  State("Conjure",Make("Conjure","Assets/03_Models/Mixamo/SwordAndShield/sword and shield casting (2).fbx",false,true),true);
  State("StaffCast",Make("StaffCast","Assets/03_Models/Mixamo/SwordAndShield/sword and shield casting.fbx",false,true),true);
  State("ShootBow",Make("ShootBow","Assets/07_Animacion/DoubleL/FBX Unity/Bow/Attack A/Bow_Attack_A_1_All.fbx",false,true),true);
  State("ThrowShuriken",Make("ThrowShuriken","Assets/03_Models/Mixamo/SwordAndShield/sword and shield casting.fbx",false,true),true);
  State("Heal",Make("Heal","Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx",true),true);
  foreach(var parameter in new[]{"MoveX","MoveY","LocomotionRate"})if(!controller.parameters.Any(p=>p.name==parameter))controller.AddParameter(parameter,AnimatorControllerParameterType.Float);
  var tree=new BlendTree{name="Directional locomotion",blendType=BlendTreeType.FreeformDirectional2D,blendParameter="MoveX",blendParameterY="MoveY",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);
  string run="Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_";
  foreach(var spec in new[]{("Forward",new Vector2(0,1)),("Backward",new Vector2(0,-1)),("Left",new Vector2(-1,0)),("Right",new Vector2(1,0)),("ForwardLeft",new Vector2(-.707f,.707f)),("ForwardRight",new Vector2(.707f,.707f)),("BackwardLeft",new Vector2(-.707f,-.707f)),("BackwardRight",new Vector2(.707f,-.707f))})tree.AddChild(Make("Locomotion"+spec.Item1,run+spec.Item1+".fbx",true),spec.Item2);
  var movement=State("Movement",tree);movement.speedParameter="LocomotionRate";movement.speedParameterActive=true;
  // Left-arm layer supplies a distinct mirrored motion while legs and root remain controlled by the base layer.
  if(!controller.layers.Any(l=>l.name=="Dual offhand"))controller.AddLayer("Dual offhand");var layers=controller.layers;layers[0].iKPass=true;int index=Array.FindIndex(layers,l=>l.name=="Dual offhand");var layer=layers[index];
  var mask=new AvatarMask{name="Offhand arm mask"};for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm,true);mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers,true);AssetDatabase.AddObjectToAsset(mask,controller);layer.avatarMask=mask;layer.defaultWeight=0;
  foreach(var child in layer.stateMachine.states)layer.stateMachine.RemoveState(child.state);
  foreach(var name in new[]{"DualIdle","DualSlice","DualStab"}){var state=layer.stateMachine.AddState(name);state.motion=Make("Offhand"+name,name=="DualIdle"?basePath+"Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx":Attack(name=="DualStab"?"B":"A",name=="DualStab"?3:2),name=="DualIdle",name!="DualIdle",true);state.speedParameter="ActionSpeed";state.speedParameterActive=name!="DualIdle";if(name=="DualIdle")layer.stateMachine.defaultState=state;}
  layers[index]=layer;controller.layers=layers;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "8-direction locomotion; weapon-specific attacks normalized to contact at 36%; masked offhand layer saved";
 }finally{UnityEngine.Object.DestroyImmediate(sample);}}
}
