using System;using System.IO;using System.Linq;using System.Text;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;
using Codice.Characters;using Codice.Expedition;using Codice.Mechanics;using Codice.UI;using Codice.Presentation;using Codice.Bosses;
// World 2 (floors 4-6) lighting tour and Big Chunk checks.
public static class AgentWorldTwo {
 static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../MapReview/agent/world2/"));
 static string Arg=>File.ReadAllText(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();
 public static string Load(){var a=Arg.Split(' ');int floor=int.Parse(a[0]);bool giant=a.Length>1&&a[1]=="giant";int seed=1000;
  if(floor==4)while(ExpeditionLayout.GiantBoss(unchecked(seed+(floor-1)*104729),floor)!=giant)seed++;
  ExpeditionFloor.DebugSeed=seed;MainMenuUI.UnlockCheats("jam");RunNavigation.StartExpeditionTest(PlayerClass.Warrior,floor);return "loading floor "+floor+" seed "+seed+" giant="+giant;}
 public static string Info(){var r=RunDirector.Instance;var f=r.Expedition;return "floor="+f.FloorNumber+" seed="+f.Seed+" rooms="+f.Layout.Count+" boss="+ExpeditionFloor.BossResource(f.Seed,f.FloorNumber)+" lights="+UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length;}
 static void Move(PlayerCharacter p,Vector3 pos,Vector3 look){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(Vector3.ProjectOnPlane(look-pos,Vector3.up)));cc.enabled=true;p.view.transform.localRotation=Quaternion.identity;}
 public static async Task<string> Tour(){
  string tag=Arg;Directory.CreateDirectory(Dir);var r=RunDirector.Instance;var f=r.Expedition;var p=r.Player;p.State.LaboratoryImmortal=true;
  for(int i=0;i<f.Cleared.Length;i++){f.Cleared[i]=true;f.Visited[i]=true;}
  var cam=p.GetComponent<HumanPreviewCamera>();if(cam&&!cam.ThirdPerson)cam.Toggle();
  var rooms=new[]{0,Mathf.Min(2,f.Layout.Boss-1),f.Layout.Boss};int n=0;
  foreach(int room in rooms){var c=f.Center(room);float d=f.Layout.Radius(room)*4-3;Move(p,c+new Vector3(0,.2f,-d),c);await Task.Delay(1500);ScreenCapture.CaptureScreenshot(Dir+tag+"_"+(n++)+".png");await Task.Delay(400);
   Move(p,c+new Vector3(0,.2f,0),c+Vector3.right*5);await Task.Delay(900);ScreenCapture.CaptureScreenshot(Dir+tag+"_"+(n++)+".png");await Task.Delay(400);}
  return "captured "+n+" "+tag;}
 public static async Task<string> Boss(){
  var r=RunDirector.Instance;var f=r.Expedition;var p=r.Player;p.State.LaboratoryImmortal=true;Directory.CreateDirectory(Dir);var sb=new StringBuilder();
  for(int i=0;i<f.Layout.Boss;i++){f.Cleared[i]=true;f.Visited[i]=true;}f.Cleared[f.Layout.Boss]=false;f.Visited[f.Layout.Boss]=false;
  var c=f.Center(f.Layout.Boss);Move(p,c+new Vector3(0,.2f,-f.Layout.Radius(f.Layout.Boss)*4+6),c);
  BossCharacter b=null;for(int i=0;i<80&&!b;i++){await Task.Delay(100);b=f.context.arena.ActiveBoss;}
  if(!b)return "NO BOSS spawned room="+f.CurrentRoom;
  await Task.Delay(2500);ScreenCapture.CaptureScreenshot(Dir+"boss_alive.png");await Task.Delay(300);
  var col=b.GetComponent<CapsuleCollider>();sb.AppendLine($"boss={b.name} kind={b.kind} hp={b.Health:F0} height={(col?col.height:0)} state={b.State} onNav={b.GetComponent<NavMeshAgent>().isOnNavMesh} dist={Vector3.Distance(b.transform.position,p.transform.position):F1}");
  var attacks=new System.Collections.Generic.HashSet<string>();float t=Time.time;while(Time.time-t<9){await Task.Delay(100);if(!b)break;if(b.CurrentAttack!=BossAttack.None)attacks.Add(b.CurrentAttack.ToString());}
  sb.AppendLine("attacks seen="+string.Join(",",attacks));
  if(b){b.ReceiveDamage(new DamageInfo{amount=1e7f,source=p.gameObject,direction=Vector3.forward,ignoreGuard=true});}
  await Task.Delay(700);var corpse=GameObject.Find("Animated remains");
  if(!corpse)sb.AppendLine("corpse MISSING");else{var a=corpse.GetComponent<Animator>();var st=a.GetCurrentAnimatorStateInfo(0);var hips=corpse.GetComponentsInChildren<Transform>().Skip(3).First();var p0=hips.position;await Task.Delay(700);sb.AppendLine($"corpse death={st.IsName("Death")} t={a.GetCurrentAnimatorStateInfo(0).normalizedTime:F2} boneMoved={(hips.position-p0).magnitude:F3} renderers={corpse.GetComponentsInChildren<Renderer>().Count(x=>x.enabled)}");ScreenCapture.CaptureScreenshot(Dir+"boss_death.png");}
  await Task.Delay(1500);sb.AppendLine("bossAlive="+(bool)f.context.arena.ActiveBoss+" completed="+f.context.arena.Completed+" cleared="+f.Cleared[f.Layout.Boss]+" portal="+(f.context.portal&&f.context.portal.gameObject.activeInHierarchy));
  return sb.ToString();}
}
