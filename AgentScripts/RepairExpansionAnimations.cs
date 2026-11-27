using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class RepairExpansionAnimations
{
 const string root="Assets/Resources/Presentation/Human/";
 static int Sanitize(AnimationClip clip)
 {
  int count=0;var muscles=new HashSet<string>(HumanTrait.MuscleName);
  foreach(var binding in AnimationUtility.GetCurveBindings(clip))
  {
   if(binding.type!=typeof(Animator)||!(muscles.Contains(binding.propertyName)||binding.propertyName.StartsWith("LeftHand.")||binding.propertyName.StartsWith("RightHand.")))continue;
   var source=AnimationUtility.GetEditorCurve(clip,binding);bool outside=source.keys.Any(k=>!float.IsFinite(k.value)||Mathf.Abs(k.value)>1);
   for(float t=0;!outside&&t<clip.length;t+=1/120f){float value=source.Evaluate(t);outside=!float.IsFinite(value)||Mathf.Abs(value)>1;}
   if(!outside)continue;
   // Resample Hermite curves, including tangent overshoot, into bounded linear muscle values.
   var curve=new AnimationCurve();int frames=Mathf.CeilToInt(clip.length*60);
   for(int i=0;i<=frames;i++){float t=Mathf.Min(clip.length,i/60f);float v=source.Evaluate(t);curve.AddKey(t,float.IsFinite(v)?Mathf.Clamp(v,-1,1):0);}
   for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
   AnimationUtility.SetEditorCurve(clip,binding,curve);count++;
  }
  EditorUtility.SetDirty(clip);return count;
 }
 public static string Run()
 {
  var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"ShinobiDeflect.anim");
  if(source.length>.4f){var clip=UnityEngine.Object.Instantiate(source);foreach(var b in AnimationUtility.GetCurveBindings(source)){var src=AnimationUtility.GetEditorCurve(source,b);var c=new AnimationCurve();for(int i=0;i<=17;i++){float t=Mathf.Min(.28f,i/60f);c.AddKey(t,src.Evaluate(.25f+t));}AnimationUtility.SetEditorCurve(clip,b,c);}var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=.28f;settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.CopySerialized(clip,source);source.name="ShinobiDeflect";UnityEngine.Object.DestroyImmediate(clip);}
  var stance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/EEJANAI_Team/FreeSwordAnimations/Animations/deffensive stance.anim"));stance.name="ShinobiGuard";
  foreach(var b in AnimationUtility.GetCurveBindings(stance)){if(b.type!=typeof(Animator)){AnimationUtility.SetEditorCurve(stance,b,null);continue;}var c=AnimationUtility.GetEditorCurve(stance,b);AnimationUtility.SetEditorCurve(stance,b,AnimationCurve.Constant(0,.5f,c.Evaluate(.1f)));}
  var guardSettings=AnimationUtility.GetAnimationClipSettings(stance);guardSettings.startTime=0;guardSettings.stopTime=.5f;guardSettings.loopTime=true;AnimationUtility.SetAnimationClipSettings(stance,guardSettings);EditorUtility.CopySerialized(stance,AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"ShinobiGuard.anim"));UnityEngine.Object.DestroyImmediate(stance);
  int repaired=0;foreach(var guid in AssetDatabase.FindAssets("t:AnimationClip",new[]{root.TrimEnd('/')})){
   var path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("Berserker_")||path.Contains("Shinobi"))repaired+=Sanitize(AssetDatabase.LoadAssetAtPath<AnimationClip>(path));
  }
  var berserker=AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"Berserker.controller");var normal=AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"HumanPlayer.controller");var roll=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"Dash.anim");
  foreach(var controller in new[]{berserker,normal}){
   var state=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Dash").state;state.motion=roll;state.speed=1;state.speedParameter="ActionSpeed";state.speedParameterActive=true;state.iKOnFeet=false;EditorUtility.SetDirty(state);
  }
  foreach(var name in new[]{"KatanaSlashA","KatanaSlashB","Shoot"}){var state=berserker.layers[0].stateMachine.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??berserker.layers[0].stateMachine.AddState(name);state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"Berserker_Melee.anim");state.speedParameter="ActionSpeed";state.speedParameterActive=true;}
  // Build a real dual-sword portrait instead of reusing the knight's shield portrait.
  var portrait=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(root+"HumanPlayer.prefab"));portrait.name="Class_Berserker";var animator=portrait.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=berserker;animator.Rebind();animator.Update(0);
  var item=new Codice.Characters.WeaponInstance(Codice.Characters.WeaponKind.Sword);
  foreach(bool left in new[]{false,true}){var hand=Codice.Presentation.HumanoidWeaponGrip.Socket(animator,left);var prefab=Codice.Presentation.WeaponVisualCatalog.Load().Get(item);var weapon=UnityEngine.Object.Instantiate(prefab,hand,false);weapon.transform.localScale=Vector3.one/Mathf.Max(.01f,hand.lossyScale.x);foreach(var col in weapon.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);}
  PrefabUtility.SaveAsPrefabAsset(portrait,"Assets/Resources/CindersMenu/Class_Berserker.prefab");UnityEngine.Object.DestroyImmediate(portrait);
  EditorUtility.SetDirty(berserker);EditorUtility.SetDirty(normal);AssetDatabase.SaveAssets();return "Repaired muscle curves="+repaired+"; both controllers share original Dash.anim from Mixamo QuickRoll; dual sword portrait saved";
 }
}
