using System.IO;using System.Linq;using System.Text;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;
using Codice.Characters;using Codice.Expedition;using Codice.Mechanics;using Codice.UI;
// Play check: seals close/open, hazards, brazier puzzle → hidden vault → secret boss, Ash Boon altar.
public static class AgentDepths{
 static string Arg=>File.ReadAllText(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();
 public static string Load(){int floor=int.Parse(Arg);int s=1000;while(new ExpeditionLayout(unchecked(s+(floor-1)*104729),floor).VaultRoom<0)s++;ExpeditionFloor.DebugSeed=s;MainMenuUI.UnlockCheats("jam");RunNavigation.StartExpeditionTest(PlayerClass.Warrior,floor);return "floor "+floor+" seed "+s;}
 static void Move(PlayerCharacter p,Vector3 pos){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=pos;cc.enabled=true;}
 public static async Task<string> Run(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new StringBuilder();
  sb.AppendLine($"floor={f.FloorNumber} vault={l.VaultRoom} host={l.VaultHost} bossVault={f.IsVaultBoss(l.VaultRoom)} hazards={Object.FindObjectsByType<FlameVent>(FindObjectsSortMode.None).Length}+{Object.FindObjectsByType<AshPool>(FindObjectsSortMode.None).Length} freeRoam={JamDebug.FreeRoam}");
  // 1) seals: enter the first combat room and count active portcullises
  int first=l.Links.First(e=>e.x==0&&e.y!=l.SecretRoom&&e.y!=l.VaultRoom).y;NavMesh.SamplePosition(f.Center(first),out var h,4,NavMesh.AllAreas);Move(p,h.position+Vector3.up*.1f);await Task.Delay(1500);
  int closed=Object.FindObjectsByType<SealDrop>(FindObjectsSortMode.None).Length;sb.AppendLine($"room {first} encounter={f.InEncounter} sealsClosed={closed} title={f.RoomTitle}");
  foreach(var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))e.ReceiveDamage(new DamageInfo{amount=1e6f,source=p.gameObject,direction=Vector3.forward});
  await Task.Delay(1800);sb.AppendLine($"after clear: encounter={f.InEncounter} sealsClosed={Object.FindObjectsByType<SealDrop>(FindObjectsSortMode.None).Length} altars={Object.FindObjectsByType<AshBoonAltar>(FindObjectsSortMode.None).Length} orbs={Object.FindObjectsByType<AshBoonOrb>(FindObjectsSortMode.None).Length}");
  var orb=Object.FindObjectsByType<AshBoonOrb>(FindObjectsSortMode.None).FirstOrDefault();if(orb){float hp=p.State.MaxHealth;Move(p,orb.transform.position-Vector3.up*1.2f+Vector3.back);orb.Interact(p);sb.AppendLine("took boon -> "+p.State.BoonSummary+" maxHp "+hp+"->"+p.State.MaxHealth);}
  // 2) puzzle: clear the host route, solve wrong then right
  for(int i=0;i<f.Cleared.Length;i++)if(i!=l.VaultRoom){f.Cleared[i]=true;f.Visited[i]=true;}
  var puzzle=f.Puzzle;if(!puzzle)return sb+"NO PUZZLE";var braziers=Object.FindObjectsByType<PuzzleBrazier>(FindObjectsSortMode.None).OrderBy(b=>b.Offerings).ToArray();
  Move(p,braziers.Last().transform.position+Vector3.back*1.5f);braziers.Last().Interact(p);sb.AppendLine($"wrong first: lit={braziers.Count(b=>b.Lit)} solved={puzzle.Solved}");
  foreach(var b in braziers){Move(p,b.transform.position+Vector3.back*1.5f);b.Interact(p);await Task.Delay(150);}
  sb.AppendLine($"ordered: braziers={braziers.Length} solved={puzzle.Solved} vaultOpen={f.VaultOpen}");
  await Task.Delay(600);
  // 3) walk into the vault through the opened door
  var vc=f.Center(l.VaultRoom);NavMesh.SamplePosition(p.transform.position,out var a,3,NavMesh.AllAreas);NavMesh.SamplePosition(vc,out var bpos,4,NavMesh.AllAreas);var path=new NavMeshPath();NavMesh.CalculatePath(a.position,bpos.position,NavMesh.AllAreas,path);sb.AppendLine("path to vault="+path.status);
  Move(p,bpos.position+Vector3.up*.1f);await Task.Delay(2500);var boss=f.context.arena.ActiveBoss;
  sb.AppendLine($"vault entered: current={f.CurrentRoom} encounter={f.InEncounter} title={f.RoomTitle} boss={(boss?boss.name+" hp="+boss.Health:"none")}");
  if(boss){boss.ReceiveDamage(new DamageInfo{amount=1e7f,source=p.gameObject,direction=Vector3.forward,ignoreGuard=true});await Task.Delay(2500);}
  sb.AppendLine($"after vault: encounter={f.InEncounter} cleared={f.Cleared[l.VaultRoom]} floorFinished={f.Finished} arenaPrefab={(f.context.arena.expeditionPrefab?f.context.arena.expeditionPrefab.name:"-")} chests={Object.FindObjectsByType<TreasureChest>(FindObjectsSortMode.None).Length} altars={Object.FindObjectsByType<AshBoonAltar>(FindObjectsSortMode.None).Length} gold={p.State.Gold}");
  return sb.ToString();}
 public static string VaultPath(){var f=RunDirector.Instance.Expedition;var l=f.Layout;NavMesh.SamplePosition(f.Center(l.VaultHost),out var a,4,NavMesh.AllAreas);NavMesh.SamplePosition(f.Center(l.VaultRoom),out var b,4,NavMesh.AllAreas);var path=new NavMeshPath();NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path);
  var end=path.corners.Length>0?path.corners[path.corners.Length-1]:Vector3.zero;var cols=Physics.OverlapSphere(end,2.5f).Select(c=>c.name).Distinct();
  return $"{path.status} host={a.position} vault={b.position} end={end} roomAtEnd={l.RoomAt(end)} near=[{string.Join(",",cols)}]";}
 public static string VaultLine(){var f=RunDirector.Instance.Expedition;var l=f.Layout;var a=f.Center(l.VaultHost);var b=f.Center(l.VaultRoom);var sb=new System.Text.StringBuilder();
  for(int k=0;k<=22;k++){var p=Vector3.Lerp(a,b,k/22f)+Vector3.up*1.2f;var hit=NavMesh.SamplePosition(p,out var h,1.5f,NavMesh.AllAreas);var cols=Physics.OverlapSphere(p,.6f).Where(c=>!c.isTrigger).Select(c=>c.name+(c.gameObject.activeInHierarchy?"":"(off)")).Distinct();sb.Append($"\n x={p.x:0} nav={hit} room={l.RoomAt(p)} [{string.Join(",",cols)}]");}
  return sb.ToString();}
 public static string State()=>$"scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} playing={Application.isPlaying} dir={(RunDirector.Instance!=null)} exp={(RunDirector.Instance!=null&&RunDirector.Instance.Expedition!=null)} t={Time.time:0}";
 public static string Unpause(){var was=UnityEditor.EditorApplication.isPaused;UnityEditor.EditorApplication.isPaused=false;return "paused was "+was;}
 public static string Errors(){var path=Application.consoleLogPath;string txt;using(var fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(fs))txt=r.ReadToEnd();var lines=txt.Split('\n');var idx=new System.Collections.Generic.List<int>();for(int i=0;i<lines.Length;i++)if(lines[i].Contains("Exception:")&&!lines[i].Contains("UIElements"))idx.Add(i);
  var sb=new StringBuilder();foreach(var i in idx.Skip(Mathf.Max(0,idx.Count-4)))for(int j=i;j<Mathf.Min(lines.Length,i+8);j++)sb.AppendLine(lines[j]);return sb.ToString();}
 public static string VaultSteps(){var f=RunDirector.Instance.Expedition;var l=f.Layout;var a=f.Center(l.VaultHost);var b=f.Center(l.VaultRoom);NavMesh.SamplePosition(a,out var ha,3,NavMesh.AllAreas);var sb=new StringBuilder();
  for(int k=4;k<=22;k++){var p=Vector3.Lerp(a,b,k/22f);NavMesh.SamplePosition(p,out var h,2,NavMesh.AllAreas);var path=new NavMeshPath();NavMesh.CalculatePath(ha.position,h.position,NavMesh.AllAreas,path);sb.Append($"\n x={p.x:0} y={h.position.y:0.00} z={h.position.z:0.0} {path.status}");}
  var g=GameObject.Find("Galería transitable");if(g){var c=g.GetComponent<Collider>();sb.Append($"\n gallery {g.transform.parent?.name} bounds={c?.bounds}");}
  return sb.ToString();}
 public static string ErrLines(){var path=Application.consoleLogPath;string txt;using(var fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(fs))txt=r.ReadToEnd();var lines=txt.Split('\n');
  var sel=lines.Select((l,i)=>(l,i)).Where(x=>(x.l.Contains("rror")||x.l.Contains("Exception")||x.l.Contains("Assert"))&&!x.l.Contains("UIElements")&&!x.l.Contains("System.Exception&")).Select(x=>x.i).ToList();
  var sb=new StringBuilder();foreach(var i in sel.Skip(Mathf.Max(0,sel.Count-6)))for(int j=i;j<Mathf.Min(lines.Length,i+4);j++)sb.AppendLine(lines[j]);return sb.ToString();}
}
