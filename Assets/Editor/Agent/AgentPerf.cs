using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading.Tasks;using Unity.Profiling;using UnityEngine;using Codice.Mechanics;
// Repeatable perf sample: frame time + managed allocations while the player fights in the first combat room.
public static class AgentPerf{
 static string Arg=>System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();
 public static string Load(){var a=Arg.Split(' ');int floor=int.Parse(a[0]);var cls=a.Length>1?(Codice.Characters.PlayerClass)System.Enum.Parse(typeof(Codice.Characters.PlayerClass),a[1]):Codice.Characters.PlayerClass.Warrior;
  Codice.Expedition.ExpeditionFloor.DebugSeed=4242;Codice.UI.MainMenuUI.UnlockCheats("jam");Codice.UI.RunNavigation.StartExpeditionTest(cls,floor);return $"load floor {floor} {cls}";}
 static async Task<string> Sample(string label,float seconds){
  using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");using var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",15);
  var ms=new List<float>();var alloc=new List<long>();int gc0=System.GC.CollectionCount(0);float end=Time.realtimeSinceStartup+seconds;int f=Time.frameCount;
  while(Time.realtimeSinceStartup<end){await Task.Yield();if(Time.frameCount==f)continue;f=Time.frameCount;ms.Add(Time.unscaledDeltaTime*1000);alloc.Add(gc.LastValue);}
  ms.Sort();var avg=ms.Average();var p95=ms[(int)(ms.Count*.95f)];var p99=ms[(int)(ms.Count*.99f)];
  return $"{label}: frames={ms.Count} avg={avg:0.0}ms p95={p95:0.0} p99={p99:0.0} max={ms.Last():0.0} spikes>33={ms.Count(x=>x>33)} gc/frame avg={alloc.Average()/1024:0.0}KB max={alloc.Max()/1024:0}KB gcCollections={System.GC.CollectionCount(0)-gc0}";}
 public static async Task<string> Run(){var r=RunDirector.Instance;if(!r||!r.Expedition)return "no run";var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new StringBuilder();
  sb.AppendLine($"floor={f.FloorNumber} rooms={l.Count} renderers={Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length} lights={Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length} tmp={Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Length} colliders={Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length}");
  sb.AppendLine(await Sample("idle start room",5));
  int first=l.Links.First(e=>e.x==0).y;UnityEngine.AI.NavMesh.SamplePosition(f.Center(first),out var h,4,UnityEngine.AI.NavMesh.AllAreas);var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=h.position+Vector3.up*.1f;cc.enabled=true;
  await Task.Delay(1500);sb.AppendLine($"enemies={Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Length}");
  var combat=p.GetComponent<Codice.Characters.PlayerCombat>();float t=Time.realtimeSinceStartup;var task=Sample("combat room",8);while(!task.IsCompleted){combat.RequestAttack();await Task.Delay(250);}sb.AppendLine(await task);
  return sb.ToString();}
 static async Task<(float ms,float kb)> Window(int frames){using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");long sum=0;float t=0;int n=0;int f=Time.frameCount;while(n<frames){await Task.Yield();if(Time.frameCount==f)continue;f=Time.frameCount;sum+=gc.LastValue;t+=Time.unscaledDeltaTime;n++;}return (t*1000/n,sum/1024f/n);}
 /// Attribution: disable every behaviour of one type for a while and see how much allocation / frame time drops.
 public static async Task<string> Blame(){var sb=new StringBuilder();var baseline=await Window(90);sb.AppendLine($"baseline {baseline.ms:0.00}ms {baseline.kb:0.0}KB/frame");
  var groups=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b.enabled&&b.gameObject.activeInHierarchy&&b.GetType().Namespace!=null&&b.GetType().Namespace.StartsWith("Codice")).GroupBy(b=>b.GetType()).ToList();
  var res=new List<(string,float,float,int)>();
  foreach(var g in groups){var list=g.ToList();foreach(var b in list)if(b)b.enabled=false;var w=await Window(45);foreach(var b in list)if(b)b.enabled=true;await Window(10);res.Add((g.Key.Name,baseline.kb-w.kb,baseline.ms-w.ms,list.Count));}
  foreach(var r in res.OrderByDescending(x=>x.Item2).Take(14))sb.AppendLine($"{r.Item1} x{r.Item4}: -{r.Item2:0.0}KB/frame -{r.Item3:0.00}ms");
  sb.AppendLine("-- by time --");foreach(var r in res.OrderByDescending(x=>x.Item3).Take(8))sb.AppendLine($"{r.Item1} x{r.Item4}: -{r.Item3:0.00}ms -{r.Item2:0.0}KB");
  return sb.ToString();}
 static async Task<(float max,float avg,long gc)> Watch(float seconds){using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");float end=Time.realtimeSinceStartup+seconds,max=0,sum=0;int n=0,f=Time.frameCount;long g=0;while(Time.realtimeSinceStartup<end){await Task.Yield();if(Time.frameCount==f)continue;f=Time.frameCount;float ms=Time.unscaledDeltaTime*1000;max=Mathf.Max(max,ms);sum+=ms;n++;g=System.Math.Max(g,gc.LastValue);}return (max,sum/Mathf.Max(1,n),g);}
 /// Walk the floor room by room: hitch when a room is entered (spawn) and when it is cleared (deaths, rewards).
 public static async Task<string> Tour(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new StringBuilder();var cc=p.GetComponent<CharacterController>();
  var order=new List<int>();var q=new Queue<int>();q.Enqueue(0);order.Add(0);while(q.Count>0){int x=q.Dequeue();foreach(var e in l.Links){int y=e.x==x?e.y:e.y==x?e.x:-1;if(y>=0&&!order.Contains(y)&&y!=l.VaultRoom&&y!=l.SecretRoom){order.Add(y);q.Enqueue(y);}}}
  foreach(var room in order.Skip(1).Take(6)){
   UnityEngine.AI.NavMesh.SamplePosition(f.Center(room),out var h,5,UnityEngine.AI.NavMesh.AllAreas);cc.enabled=false;p.transform.position=h.position+Vector3.up*.1f;cc.enabled=true;
   var enter=await Watch(2.5f);int n=Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Count(e=>e.IsAlive);
   foreach(var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))if(e.IsAlive)e.ReceiveDamage(new Codice.Characters.DamageInfo{amount=1e6f,source=p.gameObject,direction=Vector3.forward});
   var clear=await Watch(2.5f);
   sb.AppendLine($"room {room} '{f.RoomTitle}' enemies={n} enter max={enter.max:0} avg={enter.avg:0.0} gcMax={enter.gc/1024}KB | clear max={clear.max:0} avg={clear.avg:0.0} gcMax={clear.gc/1024}KB");
   for(int i=0;i<f.Cleared.Length;i++)if(i==room)f.Cleared[i]=true;}
  return sb.ToString();}
 static readonly string[] Markers={"Instantiate","Instantiate.Copy","Instantiate.Awake","Loading.ReadObject","Loading.AwakeFromLoad","Shader.CreateGPUProgram","CreateGPUProgram","GC.Collect","Physics.SyncColliderTransform","NavMeshManager","AudioClip.LoadAudioData","Camera.Render","Gfx.WaitForPresentOnGfxThread","Inl_RenderLoop","RenderPipelineManager.DoRenderLoop_Internal()","PlayerLoop","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","FixedUpdate.PhysicsFixedUpdate","Initialization.PlayerUpdateTime","GameObject.Activate","Mesh.UploadMeshData","ParticleSystem.Prewarm","Shader.Parse"};
 public static async Task<string> SpikeAnatomy(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var cc=p.GetComponent<CharacterController>();var sb=new StringBuilder();
  var recs=Markers.Select(m=>(m,rec:ProfilerRecorder.StartNew(new ProfilerCategory("Any"),m,4))).ToList();
  var all=new List<(string m,ProfilerRecorder rec)>();foreach(var x in recs)all.Add(x);
  int target=-1;foreach(var e in l.Links)if(e.x==0||e.y==0){int y=e.x==0?e.y:e.x;if(!f.Cleared[y]){target=y;break;}}
  // pick a combat room not visited yet
  for(int y=1;y<f.Cleared.Length;y++)if(!f.Visited[y]&&y!=l.VaultRoom&&y!=l.SecretRoom&&y!=l.Boss){target=y;}
  UnityEngine.AI.NavMesh.SamplePosition(f.Center(target),out var h,5,UnityEngine.AI.NavMesh.AllAreas);cc.enabled=false;p.transform.position=h.position+Vector3.up*.1f;cc.enabled=true;
  float end=Time.realtimeSinceStartup+3;int fr=Time.frameCount;float worst=0;string detail="";
  while(Time.realtimeSinceStartup<end){await Task.Yield();if(Time.frameCount==fr)continue;fr=Time.frameCount;float ms=Time.unscaledDeltaTime*1000;
   if(ms>worst){worst=ms;detail=string.Join(" ",all.Where(x=>x.rec.Valid&&x.rec.LastValue>0).Select(x=>$"{x.m}={x.rec.LastValue/1e6:0.0}ms"));}}
  foreach(var x in all)x.rec.Dispose();
  return $"room {target} '{f.RoomTitle}' worst={worst:0}ms :: {detail}";}
 public static string PerfLog(){var path=Application.consoleLogPath;string txt;using(var fs=new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.ReadWrite))using(var r=new System.IO.StreamReader(fs))txt=r.ReadToEnd();return string.Join("\n",txt.Split('\n').Where(l=>l.StartsWith("[PERF]")||l.StartsWith("[Prewarm]")).Reverse().Take(30).Reverse());}
 public static string Grep(){var key=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();var path=Application.consoleLogPath;string txt;using(var fs=new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.ReadWrite))using(var r=new System.IO.StreamReader(fs))txt=r.ReadToEnd();var lines=txt.Split('\n');var sb=new StringBuilder();int n=0;
  for(int i=0;i<lines.Length&&n<3;i++)if(lines[i].Contains(key)){n++;for(int j=i;j<Mathf.Min(lines.Length,i+10);j++)sb.AppendLine(lines[j]);sb.AppendLine("----");}return sb.Length>0?sb.ToString():"not found";}
 /// Enter every room of the floor: report rooms that start an encounter with no living enemy, and enemies that never attack.
 public static async Task<string> RoomAudit(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new StringBuilder();var cc=p.GetComponent<CharacterController>();
  sb.AppendLine($"floor {f.FloorNumber} rooms={l.Count} boss={l.Boss} vault={l.VaultRoom} secret={l.SecretRoom}");
  for(int room=1;room<l.Count;room++){if(room==l.Boss||room==l.VaultRoom||room==l.SecretRoom)continue;
   UnityEngine.AI.NavMesh.SamplePosition(f.Center(room),out var h,6,UnityEngine.AI.NavMesh.AllAreas);cc.enabled=false;p.transform.position=h.position+Vector3.up*.1f;cc.enabled=true;
   float hp0=p.State.Health;await Task.Delay(1200);var alive=Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Where(e=>e.IsAlive).ToList();
   string line=$"room {room} '{f.RoomTitle}' enc={f.InEncounter} alive={alive.Count}";
   if(f.InEncounter){p.State.Heal(999);float before=p.State.Health;var t0=Time.realtimeSinceStartup;int hurt=0;float lastHp=p.State.Health;
     // stand still 6 s near the enemies: count hits taken and which enemies ever entered an attack
     var near=alive.OrderBy(e=>Vector3.Distance(e.transform.position,p.transform.position)).FirstOrDefault();
     for(int k=0;k<24;k++){await Task.Delay(250);if(p.State.Health<lastHp)hurt++;lastHp=p.State.Health;p.State.Heal(999);}
     var still=Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Where(e=>e.IsAlive).ToList();
     var far=still.Where(e=>Vector3.Distance(e.transform.position,p.transform.position)>8).Select(e=>{var ag=e.GetComponent<UnityEngine.AI.NavMeshAgent>();var see=(bool)typeof(Codice.Enemies.BasicEnemy).GetMethod("CanSeeTarget",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(e,null);return e.name.Replace("(Clone)","")+$"@{Vector3.Distance(e.transform.position,p.transform.position):0}m dy={e.transform.position.y-p.transform.position.y:0.0} onNav={ag.isOnNavMesh} ranged={typeof(Codice.Enemies.BasicEnemy).GetProperty("IsRanged",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance)?.GetValue(e)} patrol={e.IsPatrolling} path={ag.pathStatus} v={ag.velocity.magnitude:0.0} stopped={ag.isStopped} see={see}";}).ToList();
     line+=$" hitsTaken={hurt} farAfter6s=[{string.Join(",",far)}]";
     if(hurt==0){var BF=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;object F(object o,string n)=>typeof(Codice.Enemies.BasicEnemy).GetField(n,BF)?.GetValue(o);
      foreach(var e in still){var ag=e.GetComponent<UnityEngine.AI.NavMeshAgent>();line+=$"\n   {e.name.Replace("(Clone)","")} d={Vector3.Distance(e.transform.position,p.transform.position):0.0} dy={e.transform.position.y-p.transform.position.y:0.0} target={(F(e,"target")!=null)} attacking={F(e,"attacking")} cd={F(e,"cooldown"):0.0} stag={F(e,"stagger"):0.0} rec={F(e,"recovery"):0.0} onNav={ag.isOnNavMesh} stopped={ag.isStopped} v={ag.velocity.magnitude:0.0} paused={e.Paused} enabled={e.enabled} resident={F(e,"resident")} kind={F(e,"kind")}";}}
     foreach(var e in still)e.ReceiveDamage(new Codice.Characters.DamageInfo{amount=1e6f,source=p.gameObject,direction=Vector3.forward});await Task.Delay(600);
     if(f.InEncounter){line+=" STILL_IN_ENCOUNTER";JamDebug.ClearEncounter();}}
   sb.AppendLine(line);}
  return sb.ToString();}
 public static string RoomProbe(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;var sb=new StringBuilder();
  for(int room=0;room<Mathf.Min(4,l.Count);room++){var c=f.Center(room);UnityEngine.AI.NavMesh.SamplePosition(c,out var h,6,UnityEngine.AI.NavMesh.AllAreas);sb.AppendLine($"room {room} center={c} nav={h.position} roomAt={l.RoomAt(h.position)} inside={l.Inside(room,h.position)} cleared={f.Cleared[room]}");}
  sb.AppendLine($"player={p.transform.position} playerRoom={f.PlayerRoom} cur={f.CurrentRoom} inEnc={f.InEncounter} finished={f.Finished} phase={r.Phase} alive={p.State.IsAlive}");
  foreach(var e in l.Links.Take(6))sb.Append($"{e.x}-{e.y} ");return sb.ToString();}
 public static string NoErrorPause(){var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");var get=t.GetMethod("get_consoleFlags",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);var set=t.GetMethod("SetConsoleFlag",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
  int before=(int)get.Invoke(null,null);set.Invoke(null,new object[]{4,false});int after=(int)get.Invoke(null,null);UnityEditor.EditorApplication.isPaused=false;return $"console flags {before} -> {after} (ErrorPause bit 4 off)";}
}
