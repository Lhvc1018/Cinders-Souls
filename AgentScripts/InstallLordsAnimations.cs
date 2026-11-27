using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class InstallLordsAnimations {
 const string Root="Assets/Resources/Presentation/Lords/";
 static AnimationClip Clip(string source,string name,float start,float end,bool loop=false){
  var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+source+".anim");if(!original||!original.isHumanMotion)throw new Exception("Missing humanoid "+source);
  end=Mathf.Min(end,original.length);var clip=new AnimationClip{name="Lords_"+name,frameRate=60};int count=Mathf.CeilToInt((end-start)*60)+1;
  foreach(var binding in AnimationUtility.GetCurveBindings(original)){
   var src=AnimationUtility.GetEditorCurve(original,binding);var keys=new Keyframe[count];
   for(int i=0;i<count;i++){float t=(end-start)*i/(count-1);keys[i]=new Keyframe(t,src.Evaluate(t+start));}
   // A short end blend closes the cycle without a discontinuity at the loop seam.
   if(loop)for(int i=0;i<count;i++){float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(end-start-.08f,end-start,keys[i].time));keys[i].value=Mathf.Lerp(keys[i].value,keys[0].value,weight);}
   var curve=new AnimationCurve(keys);for(int i=0;i<count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,binding,curve);
  }
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;settings.keepOriginalOrientation=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;settings.startTime=0;settings.stopTime=end-start;AnimationUtility.SetAnimationClipSettings(clip,settings);
  var path=Root+"Runtime/"+name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);return old;}AssetDatabase.CreateAsset(clip,path);return clip;
 }
 static void State(AnimatorController c,string name,Motion motion,bool action){var sm=c.layers[0].stateMachine;var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);state.motion=motion;state.speed=1;state.speedParameter=action?"ActionSpeed":"LocomotionRate";state.speedParameterActive=true;state.iKOnFeet=false;EditorUtility.SetDirty(state);}
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Stop play first");Directory.CreateDirectory(Root+"Runtime");AssetDatabase.Refresh();
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Presentation/Human/HumanPlayer.controller");
  State(controller,"SwordCombo1",Clip("AS_SS1H_LightAttack01","SwordCombo1",0,1.15f),true);
  State(controller,"SwordCombo2",Clip("AS_SS1H_LightAttack02","SwordCombo2",0,1.05f),true);
  State(controller,"SwordCombo3",Clip("AS_SS1H_LightAttack03","SwordCombo3",0,1.2f),true);
  State(controller,"Thrust",Clip("AS_SP2H_DodgeLightAttack","Thrust",0,1.05f),true);
  State(controller,"KatanaSlashA",Clip("AS_SS2H_LightAttack01","KatanaSlashA",0,1f),true);
  State(controller,"KatanaSlashB",Clip("AS_SS2H_LightAttack02","KatanaSlashB",0,1f),true);
  State(controller,"HeavyAttack",Clip("AS_US2H_DodgeLightAttack","HeavyAttack",.45f,2f),true);
  State(controller,"DualSlice",Clip("AS_DWLT_LightAttack01","DualSlice",.35f,1.65f),true);
  State(controller,"DualStab",Clip("AS_DWLT_LightAttack02","DualStab",0,1.25f),true);
  var moves=new[]{("Fnt",new Vector2(0,1),.333333f,1f),("Bck",new Vector2(0,-1),.333333f,.9f),("Lft",new Vector2(-1,0),.366667f,1.033333f),("Rht",new Vector2(1,0),.4f,1.133333f)};
  var tree=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Movement").state.motion as BlendTree;
  // Keep the original tree as a subasset for rollback; use a separate authored blend tree.
  tree=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)).OfType<BlendTree>().FirstOrDefault(t=>t.name=="Lords directional run")??new BlendTree{name="Lords directional run"};
  if(!AssetDatabase.Contains(tree))AssetDatabase.AddObjectToAsset(tree,controller);tree.blendType=BlendTreeType.FreeformDirectional2D;tree.blendParameter="MoveX";tree.blendParameterY="MoveY";tree.useAutomaticThresholds=false;tree.children=new ChildMotion[0];
  foreach(var move in moves)tree.AddChild(Clip("AS_GENE_Run_"+move.Item1,"Run"+move.Item1,move.Item3,move.Item4,true),move.Item2);
  State(controller,"Movement",tree,false);State(controller,"Walk",tree,false);
  // Every conversion remains individually selectable in Animator, including the original backward roll.
  foreach(var path in Directory.GetFiles(Root,"*.anim",SearchOption.TopDirectoryOnly)){
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);State(controller,"LordsPreview_"+clip.name,clip,!clip.isLooping);
   var berserker=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Presentation/Human/Berserker.controller");
   if(berserker){State(berserker,"LordsPreview_"+clip.name,clip,!clip.isLooping);EditorUtility.SetDirty(berserker);}
  }
  var berserkerController=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Presentation/Human/Berserker.controller");
  if(berserkerController)foreach(string state in new[]{"ShootBow","StaffCast","ShootMagic","Conjure","Heal","ThrowShuriken","Thrust","KatanaSlashA","KatanaSlashB","HeavyAttack","JumpRise","Fall","Land"}){
   var original=controller.layers[0].stateMachine.states.FirstOrDefault(s=>s.state.name==state).state;
   if(original)State(berserkerController,state,original.motion,original.speedParameterActive);
  }
  EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "Installed 3 sword cuts, 2H spear attack and 4-direction run. Original Shift Dash and Berserker dual attacks preserved. All extracted clips available as LordsPreview states.";
 }
}

