using System.Linq;using System.Text;using UnityEditor;using UnityEditor.Animations;using UnityEngine;
// Shinobi finisher / parries / mortal blade and the Assasin twin-sword chain, from the humanoid Thymesia archive.
public static class AgentCombatStates {
 const string H="Assets/Resources/Presentation/Human/";const string T="Assets/07_Animacion/Thymesia/";
 public static string Apply(){
  if(EditorApplication.isPlaying)return "stop play first";var sb=new StringBuilder();
  var shinobi=AssetDatabase.LoadAssetAtPath<AnimatorController>(H+"Shinobi.controller");
  Set(shinobi,"KatanaFinisher","Corvus_SD_LAttack5",sb);
  Set(shinobi,"MortalDraw","Corvus_PW_VargSword_Start",sb);
  Set(shinobi,"MortalSlash","Corvus_PW_UrdSword_B_End_Ver2",sb);
  Set(shinobi,"ShinobiDeflectL","Corvus_SD_ParryDeflect_L",sb);
  Set(shinobi,"ShinobiDeflectR","Corvus_SD_ParryDeflect_R",sb);
  Set(shinobi,"ShinobiDeflectLUp","Corvus_SD_ParryDeflect_LUP",sb);
  Set(shinobi,"ShinobiDeflectRUp","Corvus_SD_ParryDeflect_RUp",sb);
  Set(shinobi,"ShinobiDeflectLarge","Corvus_SD_ParryDeflect_Large",sb);
  Set(shinobi,"ShinobiGuardHit","Corvus_SD_Defense_Hurt",sb);
  var assasin=AssetDatabase.LoadAssetAtPath<AnimatorController>(H+"Berserker.controller");
  Set(assasin,"DualSlice","Corvus_PW_TwinSwords_1",sb);
  Set(assasin,"DualStab","Corvus_PW_TwinSwords_2",sb);
  Set(assasin,"DualCut3","Corvus_PW_TwinSwords_3",sb);
  Set(assasin,"DualFinisher","Corvus_PW_TwinSwords_4",sb);
  Set(assasin,"Hit","Corvus_SD_HurtSF_1",sb);
  AssetDatabase.SaveAssets();
  sb.AppendLine(AgentShinobiCuts.Export());
  return sb.ToString();
 }
 // User feedback 2026-11-10: raw Corvus clips looked broken. Back to the curated clips; Lords for dual blades.
 public static string Revert(){
  if(EditorApplication.isPlaying)return "stop play first";var sb=new StringBuilder();
  var shinobi=AssetDatabase.LoadAssetAtPath<AnimatorController>(H+"Shinobi.controller");const string TH="Assets/Resources/Presentation/Thymesia/",LO="Assets/Resources/Presentation/Lords/";
  SetPath(shinobi,"KatanaFinisher",TH+"Thymesia_KatanaSlashA.anim",sb);
  SetPath(shinobi,"MortalDraw",TH+"Thymesia_ShinobiGuard.anim",sb);
  SetPath(shinobi,"MortalSlash",LO+"AS_US2H_DodgeLightAttack.anim",sb);
  foreach(var d in new[]{"ShinobiDeflectL","ShinobiDeflectR","ShinobiDeflectLUp","ShinobiDeflectRUp","ShinobiDeflectLarge"})SetPath(shinobi,d,TH+"Thymesia_ShinobiDeflect.anim",sb);
  SetPath(shinobi,"ShinobiGuardHit",TH+"Thymesia_ShinobiGuardHit.anim",sb);
  var assasin=AssetDatabase.LoadAssetAtPath<AnimatorController>(H+"Berserker.controller");
  SetPath(assasin,"DualSlice",LO+"AS_DWLT_LightAttack01.anim",sb);
  SetPath(assasin,"DualStab",LO+"AS_DWLT_LightAttack02.anim",sb);
  SetPath(assasin,"DualCut3",TH+"Thymesia_DualSlice.anim",sb);
  SetPath(assasin,"DualFinisher",TH+"Thymesia_DualSlice.anim",sb);
  SetPath(assasin,"Hit",TH+"Thymesia_Hit.anim",sb);
  AssetDatabase.SaveAssets();sb.AppendLine(AgentShinobiCuts.Export());return sb.ToString();
 }
 static void SetPath(AnimatorController c,string state,string path,StringBuilder sb){
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){sb.AppendLine("MISSING "+path);return;}
  var sm=c.layers[0].stateMachine;var s=sm.states.FirstOrDefault(x=>x.state.name==state).state;if(s==null)s=sm.AddState(state);
  var old=s.motion?s.motion.name:"-";s.motion=clip;s.speedParameterActive=true;s.speedParameter="ActionSpeed";EditorUtility.SetDirty(c);sb.AppendLine($"{c.name}.{state}: {old} -> {clip.name}");
 }
 static void Set(AnimatorController c,string state,string clipName,StringBuilder sb){
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(T+clipName+".anim");if(!clip){sb.AppendLine("MISSING "+clipName);return;}
  var settings=AnimationUtility.GetAnimationClipSettings(clip);if(settings.loopTime){settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);}
  var sm=c.layers[0].stateMachine;var s=sm.states.FirstOrDefault(x=>x.state.name==state).state;bool added=s==null;
  if(added)s=sm.AddState(state,new Vector3(600,60*sm.states.Length%1200,0));
  var old=s.motion?s.motion.name:"-";s.motion=clip;s.speedParameterActive=true;s.speedParameter="ActionSpeed";s.iKOnFeet=false;s.writeDefaultValues=true;
  EditorUtility.SetDirty(c);sb.AppendLine($"{c.name}.{state}: {old} -> {clip.name}{(added?" (new)":"")}");
 }
}
