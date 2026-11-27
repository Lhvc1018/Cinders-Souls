using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class BuildShieldComboStep{
 public static string Run(){
 const string root="Assets/Resources/Presentation/Human/",human="Assets/Kevin Iglesias/Human Animations/Animations/Male/";
 var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"HumanPlayer.controller");
 foreach(var spec in new[]{("ShieldStrike","Combat/Shield/HumanM@AttackShield01.fbx"),("QuickStep","Movement/Run/HumanM@Run01_Forward.fbx")}){
 var source=AssetDatabase.LoadAllAssetsAtPath(human+spec.Item2).OfType<AnimationClip>().First(x=>!x.name.StartsWith("__"));if(!source.isHumanMotion)throw new Exception("Missing humanoid source");var clip=UnityEngine.Object.Instantiate(source);clip.name=spec.Item1;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.heightFromFeet=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
 foreach(var b in AnimationUtility.GetCurveBindings(clip))if(b.propertyName=="RootT.x"||b.propertyName=="RootT.z"){var curve=AnimationUtility.GetEditorCurve(clip,b);AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,curve.Evaluate(0)));}
 var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+spec.Item1+".anim");if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);clip=old;}else AssetDatabase.CreateAsset(clip,root+spec.Item1+".anim");EditorUtility.SetDirty(clip);var state=c.layers[0].stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==spec.Item1)??c.layers[0].stateMachine.AddState(spec.Item1);state.motion=clip;state.speed=1;state.speedParameter="ActionSpeed";state.speedParameterActive=spec.Item1=="ShieldStrike";state.iKOnFeet=false;EditorUtility.SetDirty(state);
 }
 EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();return "PASS native shield strike; short step uses unaccelerated Human run excerpt, not paid dodge";
 }
}
