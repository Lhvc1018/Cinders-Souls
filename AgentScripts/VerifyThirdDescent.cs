using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.AI;using Codice.Expedition;using Codice.Mechanics;using Codice.Bosses;using Codice.Characters;
public static class VerifyThirdDescent {
 public static string Layouts(){
  for(int seed=-128;seed<128;seed++)for(int floor=1;floor<=3;floor++){
   var a=new ExpeditionLayout(seed,floor);var b=new ExpeditionLayout(seed,floor);
   if(!a.Connected()||!a.Cells.SetEquals(b.Cells)||!a.Links.SequenceEqual(b.Links))throw new Exception("Connectivity/reproducibility "+seed+" / "+floor);
   if(floor==3){if(a.Count!=3||a.Boss!=2||a.SecretRoom!=-1||a.Neighbors(0).Single()!=1||a.Neighbors(2).Single()!=1)throw new Exception("Third floor shape");}
   else if(a.Count<6||a.Count>9)throw new Exception("Earlier floors changed");
  }
  if(!Resources.Load<BossCharacter>("ExpeditionEnemies/Gurdi del Abismo"))throw new Exception("Missing final guardian");
  Debug.Log("[ThirdDescentTest] PASS 768 seeded layouts, deterministic and connected; floor 3 exactly 3 rooms; guardian present");return "PASS 768 layouts";
 }
 static async Task WaitFor(Func<bool> predicate){for(int i=0;i<400;i++){if(predicate())return;await Task.Delay(50);}throw new Exception("Timed out waiting for scene");}
 public static async Task<string> Runtime(){
  var run=RunDirector.Instance;if(!run)throw new Exception("Play mode required");
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  typeof(RunDirector).GetField("<FloorNumber>k__BackingField",flags).SetValue(run,2);
  var op=SceneManager.LoadSceneAsync("Descenso");await WaitFor(()=>op.isDone&&run.Expedition&&run.Player);await Task.Delay(700);
  var floor=run.Expedition;
  if(floor.FloorNumber!=2||floor.context.arena.expeditionPrefab.kind!=BossKind.AngelKnight)throw new Exception("Second floor setup");
  // Complete the boss encounter through the room's normal completion callback; scene traversal is exercised below.
  typeof(ExpeditionFloor).GetField("<CurrentRoom>k__BackingField",flags).SetValue(floor,floor.Layout.Boss);
  typeof(ExpeditionFloor).GetField("<InEncounter>k__BackingField",flags).SetValue(floor,true);
  var completed=typeof(ExpeditionFloor).GetMethods(flags).First(m=>m.Name=="CompleteRoom");completed.Invoke(floor,null);
  if(!floor.Finished||!floor.GetComponentInChildren<DescentPortal>()||run.Phase!=RunPhase.Cleared)throw new Exception("Second boss did not open portal");
  var portal=floor.GetComponentInChildren<DescentPortal>();
  run.Player.State.AddGold(321);int carriedGold=run.Player.State.Gold;
  typeof(DescentPortal).GetField("armedAt",flags).SetValue(portal,Time.time-1);
  typeof(DescentPortal).GetMethod("TryCross",flags).Invoke(portal,new object[]{run.Player.GetComponent<CharacterController>()});
  if(run.FloorNumber!=3)throw new Exception("Third floor portal rejected player");
  await WaitFor(()=>run.FloorNumber==3&&run.Expedition&&run.Expedition!=floor&&run.Player);await Task.Delay(700);
  if(run.Player.State.Gold!=carriedGold)throw new Exception("Portal lost inventory gold");
  floor=run.Expedition;if(floor.RoomCount!=3||floor.context.arena.expeditionPrefab.kind!=BossKind.StoneGuardian)throw new Exception("Wrong third floor/boss");
  for(int room=0;room<3;room++){
   if(!NavMesh.SamplePosition(floor.Center(room),out var target,4,NavMesh.AllAreas))throw new Exception("No walkable centre "+room);
   if(room>0){NavMesh.SamplePosition(floor.Center(room-1),out var start,4,NavMesh.AllAreas);var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Blocked third-floor route "+room);}
  }
  typeof(ExpeditionFloor).GetField("<CurrentRoom>k__BackingField",flags).SetValue(floor,2);
  typeof(ExpeditionFloor).GetField("<InEncounter>k__BackingField",flags).SetValue(floor,true);completed.Invoke(floor,null);
  if(run.Phase!=RunPhase.Victory||floor.GetComponentInChildren<DescentPortal>()||run.TravelNextFloor())throw new Exception("Final victory/portal gate failed");
  Debug.Log("[ThirdDescentTest] PASS second-boss completion -> portal -> floor 3; all 3 rooms reachable; final victory, no fourth floor");return "PASS actual scene transition and navmesh; encounter completion simulated";
 }
}

