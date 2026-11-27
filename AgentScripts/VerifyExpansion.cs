using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;using UnityEngine.SceneManagement;using Codice.Characters;using Codice.Bosses;using Codice.Enemies;using Codice.Expedition;using Codice.Mechanics;using Codice.Presentation;using Codice.UI;
public static class VerifyExpansion
{
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 static void Warp(PlayerCharacter p,Vector3 pos){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=pos;cc.enabled=true;Physics.SyncTransforms();}
 public static async Task<string> All(){await Start();await ClearFloor();await Cross();await Angel();await ClearFloor();return await Restart();}
 public static async Task<string> Start()
 {
  typeof(RunNavigation).GetField("<SelectedClass>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,PlayerClass.Berserker);
  SceneManager.LoadScene("Descenso");for(int i=0;i<150&&(!RunDirector.Instance||!RunDirector.Instance.Expedition||!RunHUD.Instance||!RunHUD.Instance.Player);i++)await Task.Delay(100);await Task.Delay(400);
  var run=RunDirector.Instance;Check(run&&run.Expedition,"Missing expedition");var p=run.Player;var ap=p.GetComponent<ActorPresentation>();
  Check(p.State.Class==PlayerClass.Berserker&&p.State.MaxHealth==200&&p.State.DualWield,"Berserker stats/loadout");
  Check(ap.Body.runtimeAnimatorController.name=="Berserker","Wrong Berserker animation controller");
  Check(ap.Body.runtimeAnimatorController.animationClips.All(c=>UnityEditor.AssetDatabase.GetAssetPath(c).Contains("Berserker_")||c.name=="Dash"),"Foreign animation in Berserker controller");
  p.SetNarrativeControlsBlocked(false);RunHUD.Instance.SetPaused(false);p.enabled=false;
  p.GetComponent<PlayerCombat>().enabled=false;
  foreach(var name in new[]{"DualSlice","DualStab","Movement","Walk","JumpRise","Heal"}){ap.Play(name,.4f,true);await Task.Delay(70);Check(ap.Body.HasState(0,Animator.StringToHash(name)),"Missing state "+name);}
  p.State.AddGold(123);p.GetComponent<RunSpellbook>().Learn(RunSpell.Lightning);p.GetComponent<RunSpellbook>().Learn(RunSpell.InnerFlame);
  Debug.Log("[ExpansionTest] PASS Berserker: HP200, dual swords, Bundle3-only animation states");
  return "PASS Berserker; seeded floor="+run.Expedition.Seed;
 }
 public static async Task<string> ClearFloor()
 {
  var run=RunDirector.Instance;var floor=run.Expedition;var p=run.Player;int expected=run.FloorNumber;Check(expected>=1&&expected<=2,"Invalid floor");p.enabled=false;p.GetComponent<PlayerCombat>().enabled=false;
  int combats=0;for(int room=1;room<=floor.Layout.Boss;room++)
  {
   Warp(p,floor.Center(room)-Vector3.forward*(floor.Layout.Radius(room)*4-2));Check(floor.EnterRoom(room)||floor.Cleared[room]||floor.InEncounter,"Cannot enter room "+room);
   await Task.Delay(90);
   if(floor.IsBossRoom){var boss=floor.context.arena.ActiveBoss;Check(boss,"Missing boss");Check((expected==2)==(boss.kind==BossKind.AngelKnight),"Wrong floor boss");boss.SetPaused(false);boss.ReceiveDamage(new DamageInfo{amount=boss.MaxHealth+1,source=p.gameObject});}
   else if(floor.InEncounter){var enemies=floor.context.dungeon.GetComponentsInChildren<BasicEnemy>();Check(enemies.Length>=3,"Room spawned fewer than 3 enemies");combats++;foreach(var enemy in enemies){Check(Vector3.Distance(enemy.transform.position,p.transform.position)>6.5f,"Enemy spawned at entry");enemy.SetPaused(false);enemy.ReceiveDamage(new DamageInfo{amount=enemy.MaxHealth+1,source=p.gameObject});}}
   await Task.Delay(90);if(floor.Chests[room])Check(Vector3.ProjectOnPlane(floor.Chests[room].transform.position-floor.Center(room),Vector3.up).magnitude<.01f,"Chest off room center");Check(floor.Cleared[room],"Encounter did not complete "+room);
  }
  Check(floor.Finished,"Boss completion missing");Check(run.Phase==(expected==1?RunPhase.Cleared:RunPhase.Victory),"Wrong ending phase");
  Check((UnityEngine.Object.FindFirstObjectByType<DescentPortal>()!=null)==(expected==1),"Portal on incorrect floor");
  if(expected==1){Warp(p,floor.Center(floor.Layout.Boss)-Vector3.right*4);PlayerPrefs.SetString("ExpansionTest.State",p.State.Capture());PlayerPrefs.SetInt("ExpansionTest.Gold",p.State.Gold);PlayerPrefs.SetInt("ExpansionTest.Seed",floor.Seed);}
  Debug.Log($"[ExpansionTest] PASS floor {expected}: {combats} multi-enemy encounters, correct boss, completion and portal gate");return "PASS floor "+expected+" progression; combats="+combats;
 }
 public static async Task<string> Cross()
 {
  var run=RunDirector.Instance;var p=run.Player;Check(run.FloorNumber==2||run.FloorNumber==1&&run.Expedition.Finished,"Floor1 must be cleared");
  if(run.FloorNumber==1){var portal=UnityEngine.Object.FindFirstObjectByType<DescentPortal>();Warp(p,portal.transform.position);}for(int i=0;i<200&&(run.FloorNumber!=2||!run.Expedition||!run.Player||run.Phase==RunPhase.Travelling);i++)await Task.Delay(100);await Task.Delay(500);
  Check(run.FloorNumber==2&&run.Expedition.FloorNumber==2,"Portal did not enter floor2");Check(run.Phase==RunPhase.Cleared,"Floor2 did not start");
  p=run.Player;p.enabled=false;p.GetComponent<PlayerCombat>().enabled=false;
  Check(p.State.Gold==PlayerPrefs.GetInt("ExpansionTest.Gold"),"Lost gold during travel");Check(p.State.Class==PlayerClass.Berserker&&p.State.GetWeapon(0)!=null&&p.State.GetWeapon(1)!=null,"Lost class/equipment");Check(p.GetComponent<RunSpellbook>().Known.Contains(RunSpell.Lightning)&&p.GetComponent<RunSpellbook>().Known.Contains(RunSpell.InnerFlame),"Lost learned spells");
  Check(run.Expedition.Seed!=PlayerPrefs.GetInt("ExpansionTest.Seed"),"Same floor seed");Check(!run.TravelNextFloor(),"Early portal travel allowed");
  for(int i=0;i<run.Expedition.RoomCount;i++){if(i==run.Expedition.Layout.SecretRoom)continue;var path=new NavMeshPath();Check(NavMesh.SamplePosition(run.Expedition.Center(0),out var start,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(run.Expedition.Center(i),out var end,3,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Unreachable chamber "+i);}
  Debug.Log("[ExpansionTest] PASS physical portal crossing; floor2 retains class, dual weapons, gold, spells; NavMesh reaches all rooms");return "PASS actual trigger transition and inventory persistence";
 }
 public static async Task<string> Angel()
 {
  var run=RunDirector.Instance;var p=run.Player;p.enabled=false;var floor=run.Expedition;var pos=floor.Center(0)+Vector3.forward*5;
  var boss=UnityEngine.Object.Instantiate(Resources.Load<BossCharacter>("ExpeditionEnemies/AngelKnight"),pos,Quaternion.identity);boss.Initialize(p,5);for(int n=0;n<60&&!boss.GetComponent<ActorPresentation>().Body;n++)await Task.Delay(100);await Task.Delay(100);
  try{
   Check(boss.GetComponent<ActorPresentation>().Body&&!boss.GetComponent<ActorPresentation>().Body.isHuman,"Angel lost native skeleton: body="+boss.GetComponent<ActorPresentation>().Body+" active="+boss.gameObject.activeInHierarchy);
   boss.enabled=false;var f=BindingFlags.Instance|BindingFlags.NonPublic;var tick=typeof(BossCharacter).GetMethod("TickAngel",f);
   typeof(BossCharacter).GetField("attackTimer",f).SetValue(boss,0f);tick.Invoke(boss,new object[]{.01f});Check(boss.State==BossState.Windup,"Angel lacks attack telegraph");boss.ApplyStun(.25f);float hp=p.State.Health;tick.Invoke(boss,new object[]{.01f});Check(boss.State==BossState.Recovery&&p.State.Health==hp,"Stun allowed delayed impact");
   foreach(int seq in new[]{0,2,3}){typeof(BossCharacter).GetField("<State>k__BackingField",f).SetValue(boss,BossState.Idle);typeof(BossCharacter).GetField("attackTimer",f).SetValue(boss,0f);typeof(BossCharacter).GetField("sequence",f).SetValue(boss,seq);if(seq==3)typeof(BossCharacter).GetField("<Health>k__BackingField",f).SetValue(boss,boss.MaxHealth*.5f);tick.Invoke(boss,new object[]{.01f});Check(boss.State==BossState.Windup,"Missing pattern "+seq);tick.Invoke(boss,new object[]{2f});Check(boss.State==BossState.Recovery,"Attack did not recover");}
   boss.ReceiveDamage(new DamageInfo{amount=99999,source=p.gameObject});await Task.Delay(80);Check(!boss&&UnityEngine.Object.FindObjectsByType<AngelBolt>(FindObjectsSortMode.None).Length==0,"Death left live projectiles");
   Debug.Log("[ExpansionTest] PASS Angel native rig, telegraph, recovery, stagger cancellation, death hazard cleanup");return "PASS Angel cancellation and attack states";
  }finally{if(boss)UnityEngine.Object.Destroy(boss.gameObject);}
 }
 public static async Task<string> Restart()
 {
  var run=RunDirector.Instance;Check(run.Phase==RunPhase.Victory&&!run.TravelNextFloor(),"Unexpected third floor");var previous=run.Expedition;Check(run.RestartRun(),"Restart refused");for(int i=0;i<200&&(!run.Expedition||run.Expedition==previous||!run.Player||run.Phase==RunPhase.Travelling);i++)await Task.Delay(100);await Task.Delay(500);Check(run.FloorNumber==1&&!run.Expedition.Finished&&run.Player.State.Gold==0,"Restart did not reset run");
  PlayerPrefs.DeleteKey("ExpansionTest.State");PlayerPrefs.DeleteKey("ExpansionTest.Gold");PlayerPrefs.DeleteKey("ExpansionTest.Seed");Debug.Log("[ExpansionTest] PASS exactly two floors; restart resets floor, inventory and progression");return "PASS two-floor boundary and restart";
 }
}



