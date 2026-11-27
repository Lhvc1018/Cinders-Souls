using System;using System.Linq;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.Animations;using Codice.Characters;using Codice.Mechanics;using Codice.Presentation;using Codice.Enemies;using Codice.UI;
public static class VerifyCombatLootArena
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static string Assets()
 {
  foreach(PlayerClass role in Enum.GetValues(typeof(PlayerClass))){var state=new CharacterState();state.Initialize(role);state.Damage(10);float armor=state.Armor;state.TickMana(120);Check(state.Armor==armor,"Passive armor regeneration: "+role);Check(state.RestoreArmor(3)&&state.Armor==armor+3,"Armor fragment failed");}
  const string path="Assets/Resources/Presentation/Human/";var roll=AssetDatabase.LoadAssetAtPath<AnimationClip>(path+"Dash.anim");
  foreach(var controllerName in new[]{"HumanPlayer","Berserker"}){var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path+controllerName+".controller");Check(c.layers[0].stateMachine.states.First(x=>x.state.name=="Dash").state.motion==roll,"Roll source differs by class");}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path+"HumanPlayer.prefab");var go=UnityEngine.Object.Instantiate(prefab);var animator=go.GetComponentInChildren<Animator>();animator.enabled=false;
  try{
   foreach(string file in new[]{"Berserker_DualSlice","Berserker_DualStab","Berserker_Walk","Berserker_Movement","ShinobiGuard","ShinobiDeflect"}){
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path+file+".anim");
    foreach(var binding in AnimationUtility.GetCurveBindings(clip)){bool muscle=HumanTrait.MuscleName.Contains(binding.propertyName)||binding.propertyName.StartsWith("LeftHand.")||binding.propertyName.StartsWith("RightHand.");if(!muscle)continue;var curve=AnimationUtility.GetEditorCurve(clip,binding);for(float t=0;t<clip.length;t+=1/60f)Check(Mathf.Abs(curve.Evaluate(t))<=1.001f,"Out-of-range muscle "+file+":"+binding.propertyName);}
    for(int frame=0;frame<12;frame++){clip.SampleAnimation(go,clip.length*frame/12);foreach(var bone in new[]{HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.Head}){var point=animator.GetBoneTransform(bone).position;Check(float.IsFinite(point.x)&&Vector3.Distance(point,go.transform.position)<3.5f,"Exploded retargeted bone "+file);}}
   }
  }finally{UnityEngine.Object.DestroyImmediate(go);}
  Debug.Log("[CombatLootTest] PASS six classes without armor regen, bounded retarget poses, shared original roll");return "PASS armor/roll and sampled full-body animation bounds";
 }
 public static async Task<string> Chest()
 {
  var p=RunDirector.Instance.Player;p.enabled=false;var chest=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("CindersDungeon/RewardChest"),p.transform.position+Vector3.forward,Quaternion.identity).GetComponent<TreasureChest>();chest.price=0;chest.expeditionReward=true;chest.rewardSeed=91;
  p.State.Damage(20);float armor=p.State.Armor;Check(chest.TryOpen(p),"Chest refused");Check(!chest.TryOpen(p),"Chest duplicates rewards");await Task.Delay(800);
  var orbs=UnityEngine.Object.FindObjectsByType<LootPickup>().Where(x=>x.kind==LootKind.Armor).ToArray();Check(orbs.Length>0||p.State.Armor>armor,"No armor spheres");foreach(var orb in orbs){orb.transform.position=p.transform.position;orb.TryCollect(p);}Check(p.State.Armor>armor,"Orbs did not replenish armor");UnityEngine.Object.Destroy(chest.gameObject);Debug.Log("[CombatLootTest] PASS chest opens once, armor spheres restore armor");return "PASS guaranteed armor loot";
 }
 public static async Task<string> ArenaStart()
 {
  RunNavigation.StartEndless(PlayerClass.Warrior);for(int i=0;i<200&&(!RunDirector.Instance||!RunDirector.Instance.Endless);i++)await Task.Delay(100);var run=RunDirector.Instance;Check(run&&run.Endless,"Endless mode missing");var arena=run.Endless;run.Player.enabled=false;run.Player.GetComponent<PlayerCombat>().enabled=false;
  Check(arena.Doors.Length==4,"Expected four spawn gates");Check(!arena.ContinueNextWave(),"Can skip live wave");
  for(int i=0;i<100&&UnityEngine.Object.FindObjectsByType<BasicEnemy>().Length==0;i++)await Task.Delay(100);
  var enemies=UnityEngine.Object.FindObjectsByType<BasicEnemy>();Check(enemies.Length>0,"Door never released enemy");Check(arena.Doors.Any(x=>x.IsOpen||x.Busy),"Spawn gate never animated");
  foreach(var e in enemies){Check(arena.Doors.Min(d=>Vector3.Distance(d.transform.position,e.transform.position))<6,"Enemy did not emerge from gate");var path=new NavMeshPath();Check(NavMesh.CalculatePath(e.transform.position,run.Player.transform.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Arena enemy cannot reach player");}
  Debug.Log("[ArenaTest] PASS reused expanded Coliseo, four moving doors, navigable enemy spawn");return "PASS arena geometry/gates/spawns";
 }
 public static async Task<string> ArenaAll(){await ArenaStart();return await ArenaClear();}
 public static async Task<string> ArenaClear()
 {
  var run=RunDirector.Instance;var arena=run.Endless;Check(arena,"Missing arena");var p=run.Player;
  for(int n=0;n<180&&!arena.BetweenWaves;n++){foreach(var e in UnityEngine.Object.FindObjectsByType<BasicEnemy>()){e.ReceiveDamage(new DamageInfo{amount=e.MaxHealth+1,source=p.gameObject});}await Task.Delay(100);}
  Check(arena.BetweenWaves&&arena.Defeated==arena.Total&&run.Phase==RunPhase.Cleared,"Wave did not complete: wave="+arena.Wave+" defeated="+arena.Defeated+" total="+arena.Total+" hp="+p.State.Health+" phase="+run.Phase+" paused="+EditorApplication.isPaused+" scale="+Time.timeScale);var chest=arena.GetComponentInChildren<TreasureChest>();Check(chest,"No wave reward");var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=chest.transform.position+Vector3.right;cc.enabled=true;Check(chest.TryOpen(p),"Wave chest refused");int wave=arena.Wave;Check(arena.ContinueNextWave(),"Next endless wave refused");await Task.Delay(1500);Check(arena.Wave==wave+1&&run.Phase==RunPhase.Combat,"No next wave");
  p.State.Damage(p.State.MaxHealth+p.State.MaxArmor+100);Check(run.Phase==RunPhase.Defeated,"Endless defeat missing");int remaining=UnityEngine.Object.FindObjectsByType<BasicEnemy>().Length;await Task.Delay(1600);Check(UnityEngine.Object.FindObjectsByType<BasicEnemy>().Length<=remaining,"Spawning continued after death");Debug.Log("[ArenaTest] PASS wave reward, next unlimited wave, defeat cancels spawning");return "PASS endless wave loop and defeat";
 }
}

