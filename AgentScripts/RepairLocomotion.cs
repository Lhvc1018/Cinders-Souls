using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class RepairLocomotion
{
 public static string Run()
 {
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Presentation/Human/HumanPlayer.controller");
  string jump="Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/";
  foreach(var pair in new[]{("Dash","Assets/03_Models/Mixamo/QuickRoll.fbx"),("JumpRise",jump+"HumanM@Jump01 - Begin.fbx"),("Fall",jump+"HumanM@Fall01.fbx"),("Land",jump+"HumanM@Jump01 - Land.fbx")})
  {
   var source=AssetDatabase.LoadAllAssetsAtPath(pair.Item2).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
   if(!source.isHumanMotion)throw new Exception("Non humanoid clip: "+pair.Item2);
   var copy=UnityEngine.Object.Instantiate(source);copy.name=pair.Item1;
   var settings=AnimationUtility.GetAnimationClipSettings(copy);
   settings.loopTime=pair.Item1=="Fall";settings.loopBlendOrientation=true;settings.loopBlendPositionY=true;settings.loopBlendPositionXZ=true;
   settings.keepOriginalOrientation=true;settings.keepOriginalPositionY=false;settings.heightFromFeet=true;
   AnimationUtility.SetAnimationClipSettings(copy,settings);AnimationUtility.SetAnimationEvents(copy,new AnimationEvent[0]);
   foreach(var binding in AnimationUtility.GetCurveBindings(copy))
    if(binding.type==typeof(Transform)&&binding.propertyName.StartsWith("m_LocalScale"))AnimationUtility.SetEditorCurve(copy,binding,null);
  foreach(var binding in AnimationUtility.GetCurveBindings(copy))if(binding.propertyName=="RootT.x"||binding.propertyName=="RootT.z"){var curve=AnimationUtility.GetEditorCurve(copy,binding);AnimationUtility.SetEditorCurve(copy,binding,AnimationCurve.Constant(0,copy.length,curve.Evaluate(0)));}
   string path="Assets/Resources/Presentation/Human/"+pair.Item1+".anim";
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
   if(clip){EditorUtility.CopySerialized(copy,clip);UnityEngine.Object.DestroyImmediate(copy);}else{AssetDatabase.CreateAsset(copy,path);clip=copy;}
   var state=controller.layers[0].stateMachine.states.Select(x=>x.state).FirstOrDefault(x=>x.name==pair.Item1)??controller.layers[0].stateMachine.AddState(pair.Item1);
   state.motion=clip;state.speed=1;state.speedParameter="ActionSpeed";state.speedParameterActive=pair.Item1=="Dash"||pair.Item1=="Land";state.iKOnFeet=false;
   EditorUtility.SetDirty(state);
  }
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "Dash root baked; JumpRise/Fall/Land humanoid states saved";
 }
}

