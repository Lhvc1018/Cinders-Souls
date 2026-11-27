using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class FinalizeRoleAnimations
{
 const string Root="Assets/Resources/Presentation/Human/",Human="Assets/Kevin Iglesias/Human Animations/Animations/Male/";
 static AnimatorController controller;
 static AnimationClip Source(string path){
  var importer=AssetImporter.GetAtPath(path) as ModelImporter;
  if(importer&&importer.animationType!=ModelImporterAnimationType.Human){importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.SaveAndReimport();}
  var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));if(!clip.isHumanMotion)throw new Exception("Not humanoid: "+path);return clip;
 }
 static AnimationClip Save(string name,AnimationClip clip,bool loop=false){
  clip.name=name;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlendPositionXZ=true;settings.loopBlendOrientation=true;settings.heightFromFeet=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
  foreach(var b in AnimationUtility.GetCurveBindings(clip))if(b.propertyName=="RootT.x"||b.propertyName=="RootT.z"){var curve=AnimationUtility.GetEditorCurve(clip,b);AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,curve.Evaluate(0)));}
  var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+name+".anim");if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);clip=old;}else AssetDatabase.CreateAsset(clip,Root+name+".anim");EditorUtility.SetDirty(clip);return clip;
 }
 static void State(string name,Motion motion,bool action=false,bool gait=false){var sm=controller.layers[0].stateMachine;var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);state.motion=motion;state.speed=1;state.speedParameter=action?"ActionSpeed":"LocomotionRate";state.speedParameterActive=action||gait;EditorUtility.SetDirty(state);}
 static void Native(string name,string path,bool loop=false)=>State(name,Save(name,UnityEngine.Object.Instantiate(Source(path)),loop),!loop);
 public static string Run(){
  controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"HumanPlayer.controller");
  foreach(string name in new[]{"SwordCombo1","SwordCombo2","SwordCombo3","AxeAttack","DaggerAttack","DualSlice"})Native(name,Human+"Combat/1H/HumanM@Attack1H01_R.fbx");
  Native("DualStab",Human+"Combat/1H/HumanM@Attack1H01_L.fbx");
  foreach(string name in new[]{"KatanaSlashA","KatanaSlashB","HeavyAttack"})Native(name,Human+"Combat/2H/HumanM@Attack2H01.fbx");
  Native("ThrowShuriken",Human+"Combat/Thrown/Weapon/HumanM@ThrowWeapon01_R.fbx");
  Native("ThrowIdle",Human+"Combat/1H/HumanM@CombatIdle1H01.fbx",true);
  // Compose publisher-authored bow phases. Gameplay releases at the start of Release (65%).
  var clips=new[]{"Load","Hold","Release"}.Select(s=>Source(Human+"Combat/Bow/HumanM@BowShot01 - "+s+".fbx")).ToArray();
  var shot=UnityEngine.Object.Instantiate(clips[0]);var starts=new[]{0f,.55f,.65f};var durations=new[]{.55f,.10f,.35f};
  foreach(var binding in AnimationUtility.GetCurveBindings(shot)){
   var curves=clips.Select(c=>AnimationUtility.GetEditorCurve(c,binding)).ToArray();var keys=new Keyframe[121];
   for(int i=0;i<=120;i++){float time=i/120f;int part=time<.55f?0:time<.65f?1:2;float local=Mathf.Clamp01((time-starts[part])/durations[part]);float value=curves[part]?.Evaluate(local*clips[part].length)??curves[0].Evaluate(clips[0].length);keys[i]=new Keyframe(time,value);}
   var curve=new AnimationCurve(keys);for(int i=0;i<keys.Length;i++)curve.SmoothTangents(i,0);AnimationUtility.SetEditorCurve(shot,binding,curve);
  }
  var settings=AnimationUtility.GetAnimationClipSettings(shot);settings.startTime=0;settings.stopTime=1;AnimationUtility.SetAnimationClipSettings(shot,settings);State("ShootBow",Save("ShootBow",shot),true);
  Native("DaggerAttack",Human+"Combat/1H/HumanM@Attack1H01_R.fbx");
  Native("ShinobiGuard","Assets/03_Models/Mixamo/GreatSword/great sword blocking.fbx",true);
  Native("ShinobiDeflect","Assets/03_Models/Mixamo/GreatSword/great sword blocking (2).fbx");
  Native("ShinobiGuardHit","Assets/03_Models/Mixamo/GreatSword/great sword blocking (3).fbx");
  foreach(string state in new[]{"Walk","Movement"}){
   bool run=state=="Movement";string gait=run?"Run":"Walk";
   var existing=controller.layers[0].stateMachine.states.First(s=>s.state.name==state).state.motion as BlendTree;
   var tree=existing?existing:new BlendTree();if(!existing)AssetDatabase.AddObjectToAsset(tree,controller);
   tree.name=state+" Human Basic Motions";tree.blendType=BlendTreeType.FreeformDirectional2D;tree.blendParameter="MoveX";tree.blendParameterY="MoveY";tree.useAutomaticThresholds=false;tree.children=new ChildMotion[0];
   foreach(var direction in new[]{("Forward",new Vector2(0,1)),("Backward",new Vector2(0,-1)),("Left",new Vector2(-1,0)),("Right",new Vector2(1,0)),("ForwardLeft",new Vector2(-.707f,.707f)),("ForwardRight",new Vector2(.707f,.707f)),("BackwardLeft",new Vector2(-.707f,-.707f)),("BackwardRight",new Vector2(.707f,-.707f))})tree.AddChild(Save("Native"+gait+direction.Item1,UnityEngine.Object.Instantiate(Source(Human+"Movement/"+gait+"/HumanM@"+gait+"01_"+direction.Item1+".fbx")),true),direction.Item2);
   State(state,tree,false,true);EditorUtility.SetDirty(tree);
  }
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "PASS native bow Load/Hold/Release, dagger, three guard states and eight-direction Human walk/run";
 }
}
