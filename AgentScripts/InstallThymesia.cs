using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class InstallThymesia {
 const string H="Assets/Resources/Presentation/Human/", A="Assets/07_Animacion/Thymesia/", R="Assets/Resources/Presentation/Thymesia/";
 static AnimationClip Clip(string source,string state,bool loop=false,float end=0){
  var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(A+source+".anim");
  if(!original||!original.isHumanMotion)throw new Exception("Missing humanoid "+source);
  float duration=end>0?Mathf.Min(end,original.length):original.length;
  var clip=new AnimationClip{name="Thymesia_"+state,frameRate=60};int frames=Mathf.CeilToInt(duration*60)+1;
  foreach(var binding in AnimationUtility.GetCurveBindings(original)){
   var src=AnimationUtility.GetEditorCurve(original,binding);var keys=new Keyframe[frames];
   for(int i=0;i<frames;i++){
    float t=duration*i/(frames-1),v=src.Evaluate(t);
    if(binding.propertyName=="RootT.x"||binding.propertyName=="RootT.z")v=Mathf.Clamp(v,-.08f,.08f);
    if(loop){float w=Mathf.SmoothStep(0,1,Mathf.InverseLerp(duration-.06f,duration,t));v=Mathf.Lerp(v,src.Evaluate(0),w);}
    keys[i]=new Keyframe(t,v);
   }
   var c=new AnimationCurve(keys);for(int i=0;i<frames;i++){AnimationUtility.SetKeyLeftTangentMode(c,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(c,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,binding,c);
  }
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;settings.startTime=0;settings.stopTime=duration;settings.keepOriginalOrientation=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
  string path=R+"Thymesia_"+state+".anim";if(!AssetDatabase.LoadAssetAtPath<AnimationClip>(path)&&AssetDatabase.LoadAssetAtPath<AnimationClip>(R+state+".anim"))AssetDatabase.MoveAsset(R+state+".anim",path);var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);return old;}AssetDatabase.CreateAsset(clip,path);return clip;
 }
 static void Set(AnimatorController c,string name,Motion motion,bool action=true){
  var sm=c.layers[0].stateMachine;var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??sm.AddState(name);state.motion=motion;state.speed=1;state.speedParameter=action?"ActionSpeed":"LocomotionRate";state.speedParameterActive=true;state.iKOnFeet=false;EditorUtility.SetDirty(state);
 }
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Stop play first");Directory.CreateDirectory(R);AssetDatabase.Refresh();
  var assassin=AssetDatabase.LoadAssetAtPath<AnimatorController>(H+"Berserker.controller");var shinobi=AssetDatabase.LoadAssetAtPath<AnimatorController>(H+"Shinobi.controller");
  foreach(var spec in new[]{("SwordCombo1","Corvus_SD_LAttack1"),("SwordCombo2","Corvus_SD_LAttack2"),("SwordCombo3","Corvus_SD_LAttack3_New"),("DualSlice","Corvus_PW_TwinSwords_1"),("DualStab","Corvus_PW_TwinSwords_2"),("AxeAttack","Corvus_PW_Axe"),("HeavyAttack","Corvus_PW_Hammer_A"),("Thrust","Corvus_PW_Halberds"),("DaggerAttack","Corvus_PW_Knife_A_V1"),("ThrowShuriken","Corvus_SD_TD2Thorw2"),("ShootBow","Corvus_PW_Bow_B"),("StaffCast","Corvus_PW_Magician_A"),("ShootMagic","Corvus_PW_Magician_A"),("Conjure","Corvus_Raven_FeatherRoundAttack"),("Heal","Corvus_Healing_Little"),("Drink","Corvus_Healing_Little"),("Hit","Corvus_SD_HurtSF_1"),("Death","Corvus_SD_Dead_1"),("JumpRise","Corvus_SD_Fall_Start"),("Fall","Corvus_SD_Fall_Loop"),("Land","Corvus_SD_Fall_End"),("BlockIdle","Corvus_SD_Defense_Loop")}){
   bool loop=spec.Item1=="Fall"||spec.Item1=="BlockIdle";Set(assassin,spec.Item1,Clip(spec.Item2,spec.Item1,loop),!loop);
  }
  foreach(string state in new[]{"Idle","Ready","DualIdle","HeavyReady","SwordIdle","KatanaIdle","SpearIdle","BowIdle","StaffIdle","RifleIdle"})Set(assassin,state,Clip("Corvus_SD_Idle",state,true),false);
  var tree=new BlendTree{name="Thymesia eight-direction run",blendType=BlendTreeType.FreeformDirectional2D,blendParameter="MoveX",blendParameterY="MoveY",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,assassin);
  foreach(var dir in new[]{("F_24",0f,1f),("B",0f,-1f),("L",-1f,0f),("R",1f,0f),("FL",-.7071f,.7071f),("FR",.7071f,.7071f),("BL",-.7071f,-.7071f),("BR",.7071f,-.7071f)})tree.AddChild(Clip("Corvus_SD_Run"+dir.Item1,"Run"+dir.Item1,true),new Vector2(dir.Item2,dir.Item3));
  foreach(string state in new[]{"Walk","Movement","HeavyWalk","HeavyRun"})Set(assassin,state,tree,false);
  foreach(var spec in new[]{("KatanaSlashA","Corvus_SD_LAttack1",1.15f),("KatanaSlashB","Corvus_SD_LAttack2",1.25f),("ShinobiGuard","Corvus_SD_Defense_Loop",0f),("ShinobiDeflect","Corvus_SD_ParryDeflect_L",.4f),("ShinobiGuardHit","Corvus_SD_Defense_Hurt",.4f)}){
   var clip=Clip(spec.Item2,spec.Item1,spec.Item1=="ShinobiGuard",spec.Item3);Set(shinobi,spec.Item1,clip,spec.Item1!="ShinobiGuard");Set(assassin,spec.Item1,clip,spec.Item1!="ShinobiGuard");
  }
  // Keep original Shift roll. Incompatible cinematic and weapon-only actions stay in the imported archive.
  var roll=AssetDatabase.LoadAssetAtPath<AnimationClip>(H+"Dash.anim");Set(assassin,"Dash",roll);Set(shinobi,"Dash",roll);
  foreach(var controller in new[]{assassin,shinobi,AssetDatabase.LoadAssetAtPath<AnimatorController>(H+"ClassicHuman.controller")}){
   foreach(var clip in controller.animationClips.Distinct().Where(c=>c.name.StartsWith("Thymesia_")).ToArray())Set(controller,"Preview_"+clip.name,clip,!clip.isLooping);
   foreach(var path in Directory.GetFiles("Assets/Resources/Presentation/Lords","*.anim",SearchOption.TopDirectoryOnly)){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);Set(controller,"LordsPreview_"+clip.name,clip,!clip.isLooping);}
   EditorUtility.SetDirty(controller);
  }
  EditorUtility.SetDirty(tree);AssetDatabase.SaveAssets();return "Classic knight/tank controllers restored; Shinobi attacks/guard retargeted; Assasin combat, 8-direction locomotion, recovery/casting/healing connected. Original Shift roll retained.";
 }
}
