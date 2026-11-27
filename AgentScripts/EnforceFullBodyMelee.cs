using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class EnforceFullBodyMelee{
 public static string Run(){
 const string root="Assets/Resources/Presentation/Human/",human="Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/";
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"HumanPlayer.controller");var layers=controller.layers;layers[0].avatarMask=null;
 foreach(var spec in new[]{("SwordCombo1","R"),("SwordCombo2","R"),("SwordCombo3","R"),("DualSlice","R"),("DualStab","L")}){
 var source=AssetDatabase.LoadAllAssetsAtPath(human+"HumanM@Attack1H01_"+spec.Item2+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));if(!source.isHumanMotion)throw new Exception("Invalid Human Melee source");
 var clip=UnityEngine.Object.Instantiate(source);clip.name=spec.Item1;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlendPositionXZ=true;settings.heightFromFeet=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
 foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.propertyName=="RootT.x"||binding.propertyName=="RootT.z"){var curve=AnimationUtility.GetEditorCurve(clip,binding);AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,curve.Evaluate(0)));}
 var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+spec.Item1+".anim");EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(saved);var state=layers[0].stateMachine.states.First(x=>x.state.name==spec.Item1).state;state.motion=saved;state.iKOnFeet=false;state.speedParameter="ActionSpeed";state.speedParameterActive=true;EditorUtility.SetDirty(state);
 }
 for(int i=1;i<layers.Length;i++)layers[i].defaultWeight=0;controller.layers=layers;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "PASS five sword actions from Human Melee only; base mask removed; foot IK off; overlay weights zero";
 }
}
