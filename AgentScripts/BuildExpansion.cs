using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using UnityEngine.AI;using Codice.Bosses;
public static class BuildExpansion
{
 const string human="Assets/Resources/Presentation/Human/";
 static AnimationClip Source(string path,string name=null)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__")&&(name==null||c.name==name));
 static AnimationClip Clip(AnimationClip source,string path,string name,bool loop,bool humanoid=true)
 {
  var c=UnityEngine.Object.Instantiate(source);c.name=name;var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=loop;settings.loopBlend=loop;settings.heightFromFeet=humanoid;AnimationUtility.SetAnimationClipSettings(c,settings);AnimationUtility.SetAnimationEvents(c,new AnimationEvent[0]);
  if(humanoid)foreach(var b in AnimationUtility.GetCurveBindings(c))if(b.propertyName=="RootT.x"||b.propertyName=="RootT.z"){var curve=AnimationUtility.GetEditorCurve(c,b);AnimationUtility.SetEditorCurve(c,b,AnimationCurve.Constant(0,c.length,curve.Evaluate(0)));}
  var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(old){EditorUtility.CopySerialized(c,old);UnityEngine.Object.DestroyImmediate(c);c=old;}else AssetDatabase.CreateAsset(c,path);EditorUtility.SetDirty(c);return c;
 }
 static AnimatorState State(AnimatorController controller,string name,AnimationClip clip,bool action)
 {
  var sm=controller.layers[0].stateMachine;var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??sm.AddState(name);state.motion=clip;state.iKOnFeet=false;state.speed=1;state.speedParameter="ActionSpeed";state.speedParameterActive=action;state.writeDefaultValues=true;EditorUtility.SetDirty(state);return state;
 }
 static AnimatorController Controller(string path)
 {
  var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(c)return c;
  c=AnimatorController.CreateAnimatorControllerAtPath(path);c.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);c.AddParameter("LocomotionSpeed",AnimatorControllerParameterType.Float);c.AddParameter("MoveX",AnimatorControllerParameterType.Float);c.AddParameter("MoveY",AnimatorControllerParameterType.Float);c.AddParameter("LocomotionRate",AnimatorControllerParameterType.Float);return c;
 }
 // After regenerating, run RepairExpansionAnimations.Run to normalize imported muscles and rebuild the dual-sword preview.
 public static string Run()
 {
  var player=AssetDatabase.LoadAssetAtPath<AnimatorController>(human+"HumanPlayer.controller");
  foreach(var pair in new[]{("SwordCombo1","slash1"),("SwordCombo3","slash3")})State(player,pair.Item1,Clip(Source("Assets/EEJANAI_Team/FreeSwordAnimations/Animations/"+pair.Item2+".anim"),human+pair.Item1+".anim",pair.Item1,false),true);
  EditorUtility.SetDirty(player);
  var berserker=Controller(human+"Berserker.controller");
  const string bundle="Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@";
  var map=new Dictionary<string,string>{{"Idle","Idle"},{"Ready","Idle"},{"DualIdle","Idle"},{"HeavyReady","Idle"},{"BlockIdle","Idle"},{"Walk","WalkSlow"},{"Movement","Run"},{"HeavyWalk","Walk"},{"HeavyRun","Run"},{"JumpRise","Jump"},{"Fall","Fall"},{"Land","Idle"},{"Dash","Jump"},{"QuickStep","Walk"},{"DualSlice","Attack1"},{"DualStab","Attack1"},{"Melee","Attack1"},{"Shoot","Attack1"},{"KatanaSlashA","Attack1"},{"KatanaSlashB","Attack1"},{"Hit","Fall"},{"Death","Fall"},{"SwordCombo1","Attack1"},{"SwordCombo2","Attack1"},{"SwordCombo3","Attack1"},{"Conjure","Attack1"},{"Drink","Idle"},{"Heal","Idle"},{"ShootBow","Attack1"},{"ShootMagic","Attack1"},{"StaffCast","Attack1"},{"ThrowShuriken","Attack1"},{"Thrust","Attack1"},{"HeavyAttack","Attack1"},{"AxeAttack","Attack1"},{"DaggerAttack","Attack1"},{"StaffIdle","Idle"},{"BowIdle","Idle"},{"RifleIdle","Idle"}};
  foreach(var pair in map){if(pair.Key=="Dash"){State(berserker,"Dash",AssetDatabase.LoadAssetAtPath<AnimationClip>(human+"Dash.anim"),true);continue;}bool loop=pair.Value=="Idle"||pair.Value=="Run"||pair.Value=="Walk"||pair.Value=="WalkSlow";var clip=Clip(Source(bundle+pair.Value+".FBX"),human+"Berserker_"+pair.Key+".anim",pair.Key,loop);var state=State(berserker,pair.Key,clip,!loop);state.mirror=pair.Key=="DualStab";}
  berserker.layers[0].stateMachine.defaultState=berserker.layers[0].stateMachine.states.First(x=>x.state.name=="Idle").state;EditorUtility.SetDirty(berserker);
  var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CindersMenu/Class_Warrior.prefab");
  var previewInstance=UnityEngine.Object.Instantiate(preview);previewInstance.name="Class_Berserker";PrefabUtility.SaveAsPrefabAsset(previewInstance,"Assets/Resources/CindersMenu/Class_Berserker.prefab");UnityEngine.Object.DestroyImmediate(previewInstance);
  const string bossRoot="Assets/Resources/ExpeditionEnemies/";
  var controller=Controller(bossRoot+"AngelKnight.controller");
  string fbx="Assets/Angel_Knight_Anims/Meshs/Angel Knight/LowPoly_AngelKnight.fbx";
  foreach(var pair in new[]{("Idle","Stand_Idle"),("Chase","Stand_Run"),("Run","Stand_Run"),("Melee","Stand_Attack"),("SlashB","Attack_1.2_To_Idle_1"),("Slam","Attack_3.3_To_Idle_3"),("Shoot","Spell"),("Nova","Stand_Power_Up"),("Hit","Stand_Damage"),("Stun","Stand_Damage"),("Death","Stand_Death")}){
   bool loop=pair.Item1=="Idle"||pair.Item1=="Chase"||pair.Item1=="Run";State(controller,pair.Item1,Clip(Source(fbx,pair.Item2),bossRoot+"Angel_"+pair.Item1+".anim",pair.Item1,loop,false),!loop);
  }
  controller.layers[0].stateMachine.defaultState=controller.layers[0].stateMachine.states.First(x=>x.state.name=="Idle").state;EditorUtility.SetDirty(controller);
  var root=new GameObject("Angel Knight · Custodio del segundo descenso");root.SetActive(false);
  var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Angel_Knight_Anims/Prefabs/LowPoly_AngelKnight.prefab"),root.transform);model.name="Angel visual";
  foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
  foreach(var col in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(col);
  foreach(var audio in model.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(audio);
  foreach(var ps in model.GetComponentsInChildren<ParticleSystem>(true)){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;}
  var anim=model.GetComponentInChildren<Animator>();anim.runtimeAnimatorController=controller;anim.applyRootMotion=false;
  // Native generic tracks preserve the wing skeleton, unlike humanoid retargeting.
  var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);model.transform.localScale*=2.7f/Mathf.Max(.1f,bounds.size.y);
  var boss=root.AddComponent<BossCharacter>();boss.visual=model.transform;boss.kind=BossKind.AngelKnight;boss.useNativeAnimator=true;boss.baseHealth=1800;boss.goldReward=600;boss.summons=new Codice.Enemies.BasicEnemy[0];
  var agent=root.GetComponent<NavMeshAgent>();agent.radius=.65f;agent.height=2.7f;agent.speed=4.2f;agent.acceleration=18;agent.angularSpeed=360;agent.stoppingDistance=2.5f;
  var capsule=root.GetComponent<CapsuleCollider>();capsule.radius=.65f;capsule.height=2.7f;capsule.center=Vector3.up*1.35f;
  root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,bossRoot+"AngelKnight.prefab");UnityEngine.Object.DestroyImmediate(root);
  AssetDatabase.SaveAssets();return "Built: sword slash1/slash3, Berserker Bundle3-only controller, native generic Angel Knight with 11 states.";
 }
}

