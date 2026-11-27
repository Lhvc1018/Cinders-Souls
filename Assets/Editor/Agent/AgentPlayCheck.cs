using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Text;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;
using Codice.Bosses;using Codice.Characters;using Codice.Enemies;using Codice.Mechanics;using Codice.Presentation;
// Play Mode checks for this delivery (laboratory scene). Measures, does not assert balance.
public static class AgentPlayCheck {
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static async Task<PlayerCharacter> Lab(){for(int i=0;i<100&&(!RunDirector.Instance||!RunDirector.Instance.Player);i++)await Task.Delay(100);var p=RunDirector.Instance.Player;if(!p)throw new Exception("lab player missing");return p;}
 static BasicEnemy Spawn(string path,PlayerCharacter p,Vector3 offset){var prefab=Resources.Load<BasicEnemy>(path);if(!NavMesh.SamplePosition(p.transform.position+offset,out var hit,4,NavMesh.AllAreas))throw new Exception("no navmesh for "+path);var e=UnityEngine.Object.Instantiate(prefab,hit.position,Quaternion.LookRotation(-offset));e.Initialize(p,1);return e;}
 public static async Task<string> Shinobi(){
  var p=await Lab();var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();room.ClearEnemies();
  if(!room.ChangeClass(PlayerClass.Shinobi))throw new Exception("class switch");await Task.Delay(400);p.SetNarrativeControlsBlocked(false);
  var combat=p.GetComponent<PlayerCombat>();var pres=p.GetComponent<ActorPresentation>();var sb=new StringBuilder();
  var dummy=Spawn("ExpeditionEnemies/MuckResidents/Salteador de la cripta",p,p.transform.forward*1.7f);await Task.Delay(200);
  float hitAt=-1;Action<CombatFeedback.Result> on=r=>{if(r.target==dummy&&hitAt<0)hitAt=Time.time;};CombatFeedback.Resolved+=on;
  var tryAttack=typeof(PlayerCombat).GetMethod("TryAttack",F);var next=typeof(PlayerCombat).GetField("nextAttack",F);
  try{
   for(int step=0;step<3;step++){
    // face the dummy so the cone test is meaningful
    var dir=Vector3.ProjectOnPlane(dummy.transform.position-p.transform.position,Vector3.up).normalized;p.transform.rotation=Quaternion.LookRotation(dir);p.view.transform.rotation=Quaternion.LookRotation(dir);dummy.ApplyStun(2);
    hitAt=-1;float t0=Time.time;bool ok=(bool)tryAttack.Invoke(combat,null);float dur=(float)next.GetValue(combat)-t0;
    await Task.Delay(30);string state=pres.CurrentState;bool authored=pres.TryAuthoredContactWindow(out float a,out float b);
    while(combat.IsSwinging&&Time.time-t0<3)await Task.Delay(15);
    sb.AppendLine($"step{step+1} ok={ok} combo={combat.Combo} state={state} duration={dur:F2}s window={(authored?a.ToString("F2")+"-"+b.ToString("F2"):"generic")} damageAt={(hitAt<0?"none":((hitAt-t0)/dur).ToString("F2"))}");
    await Task.Delay(60);
   }
   // buffered input: a click at 45% of the swing must start the next cut without a second click
   await Task.Delay(900);tryAttack.Invoke(combat,null);int first=combat.Combo;float s0=Time.time;float d0=(float)next.GetValue(combat)-s0;
   await Task.Delay((int)(d0*450));bool immediate=combat.RequestAttack();int after=first;float w=Time.time;while(Time.time-w<1.2f){await Task.Delay(15);if(combat.Combo!=first){after=combat.Combo;break;}}
   sb.AppendLine($"buffer: request mid-swing immediate={immediate} chained={(after!=first)} ({first}->{after}); impact VFX live="+UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Impact •")));
  }finally{CombatFeedback.Resolved-=on;if(dummy)UnityEngine.Object.Destroy(dummy.gameObject);p.SetNarrativeControlsBlocked(true);}
  return sb.ToString();
 }
 static float Lowest(GameObject go){float y=float.PositiveInfinity;foreach(var f in go.GetComponentsInChildren<MeshFilter>()){var mb=f.sharedMesh.bounds;for(int c=0;c<8;c++){var v=mb.center+Vector3.Scale(mb.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1));y=Mathf.Min(y,f.transform.TransformPoint(v).y);}}return y;}
 public static async Task<string> Spear(){
  var p=await Lab();var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();room.ClearEnemies();var sb=new StringBuilder();
  var b=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("ExpeditionEnemies/Lanza del Infierno")).GetComponent<BossCharacter>();
  try{
   b.Initialize(p,1);b.enabled=false;NavMesh.SamplePosition(p.transform.position+p.transform.forward*9,out var hit,6,NavMesh.AllAreas);b.GetComponent<NavMeshAgent>().Warp(hit.position);await Task.Delay(300);
   float ground=b.transform.position.y;var pres=b.GetComponent<ActorPresentation>();var spear=pres.Body.GetComponentsInChildren<MeshFilter>(true).First(m=>m.name.Contains("Spear")).gameObject;
   sb.AppendLine("guard on spear="+(spear.GetComponent<WeaponGroundGuard>()!=null));
   foreach(var clip in new[]{"Spin","Melee","Slam","Shoot"}){pres.Play(clip,1.6f,true);float min=99;float t=Time.time;while(Time.time-t<1.6f){await Task.Yield();min=Mathf.Min(min,Lowest(spear)-ground);}sb.AppendLine($"{clip}: spear lowest above floor={min:F3} m");}
   // thrown spear
   var tick=typeof(BossCharacter).GetMethod("TickWarden",F);typeof(BossCharacter).GetField("attackTimer",F).SetValue(b,0f);typeof(BossCharacter).GetField("sequence",F).SetValue(b,0);
   tick.Invoke(b,new object[]{.01f});sb.AppendLine("attack="+b.CurrentAttack);tick.Invoke(b,new object[]{9f});
   float lowThrown=99;int seen=0;float t1=Time.time;while(Time.time-t1<1.5f){await Task.Yield();foreach(var w in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(x=>x.name=="Arma nativa arrojada")){seen++;lowThrown=Mathf.Min(lowThrown,Lowest(w.gameObject)-ground);}}
   sb.AppendLine($"thrown spears frames={seen} lowest above floor={lowThrown:F3} m");
  }finally{if(b)UnityEngine.Object.Destroy(b.gameObject);}
  return sb.ToString();
 }
 public static async Task<string> Residents(){
  var p=await Lab();var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();room.ClearEnemies();var sb=new StringBuilder();
  string[] names={"Lobo de hollin","Salteador de la cripta","Golem funerario","Hondero de la cripta"};
  foreach(var n in names){
   var e=Spawn("ExpeditionEnemies/MuckResidents/"+n,p,(Quaternion.Euler(0,40,0)*p.transform.forward)*(n.StartsWith("Hondero")?9:4.5f));
   var hits=new List<CombatFeedback.Result>();Action<CombatFeedback.Result> on=r=>{if(r.hit.source==e.gameObject||(r.hit.source&&r.hit.source.transform.IsChildOf(e.transform)))hits.Add(r);};CombatFeedback.Resolved+=on;
   int rings=0;float t=Time.time;Vector3 start=e.transform.position;float closest=99;
   try{while(Time.time-t<6){await Task.Delay(50);if(!e)break;closest=Mathf.Min(closest,Vector3.Distance(e.transform.position,p.transform.position));rings=Mathf.Max(rings,UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Length);}
    sb.AppendLine($"{n}: resident={e.resident} kind={e.kind} hp={e.MaxHealth:F0} hits={hits.Count} dmg={hits.Sum(h=>h.amount):F0} closest={closest:F1}m lineRenderers={rings} projectileHits={hits.Count(h=>!h.hit.melee)}");}
   finally{CombatFeedback.Resolved-=on;if(e)UnityEngine.Object.Destroy(e.gameObject);}
   await Task.Delay(300);
  }
  return sb.ToString();
 }
}
