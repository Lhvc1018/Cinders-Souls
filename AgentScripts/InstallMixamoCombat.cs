using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class InstallMixamoCombat{
 const string Root="Assets/Resources/Presentation/Human/";
 static AnimatorController controller;
 static AnimationClip Clip(string name,string folder,string source,bool loop=false,bool mirror=false){
 string path="Assets/03_Models/Mixamo/"+folder+"/"+source+".fbx";if(folder=="")path="Assets/03_Models/Mixamo/"+source+".fbx";
 var imp=(ModelImporter)AssetImporter.GetAtPath(path);if(!imp)throw new Exception(path);
 if(imp.animationType!=ModelImporterAnimationType.Human){imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.importAnimation=true;imp.SaveAndReimport();}
 var native=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(x=>!x.name.StartsWith("__"));if(!native.isHumanMotion)throw new Exception("Not humanoid "+path);
 var c=UnityEngine.Object.Instantiate(native);c.name=name;var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=loop;settings.mirror=mirror;settings.loopBlendOrientation=true;settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=loop;settings.keepOriginalOrientation=true;settings.heightFromFeet=true;AnimationUtility.SetAnimationClipSettings(c,settings);AnimationUtility.SetAnimationEvents(c,new AnimationEvent[0]);
 foreach(var b in AnimationUtility.GetCurveBindings(c))if(b.propertyName=="RootT.x"||b.propertyName=="RootT.z"){var curve=AnimationUtility.GetEditorCurve(c,b);AnimationUtility.SetEditorCurve(c,b,AnimationCurve.Constant(0,c.length,curve.Evaluate(0)));}
 var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+name+".anim");if(old){EditorUtility.CopySerialized(c,old);UnityEngine.Object.DestroyImmediate(c);c=old;}else AssetDatabase.CreateAsset(c,Root+name+".anim");EditorUtility.SetDirty(c);return c;
 }
 static void State(string name,Motion motion,bool action=false){var sm=controller.layers[0].stateMachine;var s=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??sm.AddState(name);s.motion=motion;s.speedParameter=action?"ActionSpeed":"LocomotionRate";s.speedParameterActive=action||motion is BlendTree;s.speed=1;EditorUtility.SetDirty(s);}
 static void Gait(string state,string folder,string prefix,bool run){
 var tree=new BlendTree{name=state+" Mixamo",blendType=BlendTreeType.FreeformDirectional2D,blendParameter="MoveX",blendParameterY="MoveY",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);
 string gait=run?"run":"walk";var forward=Clip(state+"Forward",folder,prefix+gait,true);var back=Clip(state+"Back",folder,prefix+gait+" (2)",true);
 var left=Clip(state+"Left",folder,prefix+"strafe"+(run?" (3)":""),true);var right=Clip(state+"Right",folder,prefix+"strafe"+(run?" (4)":" (2)"),true);
 tree.AddChild(forward,new Vector2(0,1));tree.AddChild(back,new Vector2(0,-1));tree.AddChild(left,new Vector2(-1,0));tree.AddChild(right,new Vector2(1,0));State(state,tree);
 }
 public static string Run(){AssetDatabase.Refresh();controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"HumanPlayer.controller");
 foreach(var spec in new[]{("Ready","sword and shield idle"),("BlockIdle","sword and shield block idle")})State(spec.Item1,Clip(spec.Item1,"ProSword",spec.Item2,true));
 foreach(var spec in new[]{("SwordCombo1","sword and shield slash"),("SwordCombo2","sword and shield slash (2)"),("SwordCombo3","sword and shield slash (3)"),("Melee","sword and shield slash"),("Hit","sword and shield impact"),("BlockHit","sword and shield block")})State(spec.Item1,Clip(spec.Item1,"ProSword",spec.Item2),true);
 foreach(var spec in new[]{("KatanaSlashA","great sword slash"),("KatanaSlashB","great sword slash (2)"),("HeavyAttack","great sword slash (3)"),("AxeAttack","great sword slash (2)")})State(spec.Item1,Clip(spec.Item1,"GreatSword",spec.Item2),true);
 State("HeavyReady",Clip("HeavyReady","GreatSword","great sword idle",true));
 State("StaffCast",Clip("StaffCast","","ForwardMagic"),true);State("Conjure",Clip("Conjure","","ForwardMagic",false,true),true);
 Gait("Walk","ProSword","sword and shield ",false);Gait("Movement","ProSword","sword and shield ",true);Gait("HeavyWalk","GreatSword","great sword ",false);Gait("HeavyRun","GreatSword","great sword ",true);
 EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "Mixamo native light/heavy gait, slash chains, impact, block and forward magic installed; no attack time warping";
 }
}
