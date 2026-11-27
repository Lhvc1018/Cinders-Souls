using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;using System.Text;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;
using Codice.Characters;using Codice.Enemies;using Codice.Mechanics;using Codice.Presentation;
public static class AgentPlayCheck2 {
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static string Shots=>Path.GetFullPath(Path.Combine(Application.dataPath,"../MapReview/agent/"));
 static async Task<(PlayerCharacter,CombatTestRoom)> Lab(PlayerClass role){for(int i=0;i<100&&(!RunDirector.Instance||!RunDirector.Instance.Player);i++)await Task.Delay(100);var p=RunDirector.Instance.Player;var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();room.ClearEnemies();room.ChangeClass(role);await Task.Delay(500);p.SetNarrativeControlsBlocked(false);return (p,room);}
 static BasicEnemy Dummy(PlayerCharacter p,float d=1.7f){var prefab=Resources.Load<BasicEnemy>("ExpeditionEnemies/MuckResidents/Salteador de la cripta");NavMesh.SamplePosition(p.transform.position+p.transform.forward*d,out var hit,3,NavMesh.AllAreas);var e=UnityEngine.Object.Instantiate(prefab,hit.position,Quaternion.LookRotation(-p.transform.forward));e.Initialize(p,6);return e;}
 static void Face(PlayerCharacter p,Component t){var dir=Vector3.ProjectOnPlane(t.transform.position-p.transform.position,Vector3.up).normalized;p.transform.rotation=Quaternion.LookRotation(dir);p.view.transform.rotation=Quaternion.LookRotation(dir);}
 static async Task<string> Chain(PlayerCharacter p,BasicEnemy dummy,int steps,string tag){
  var combat=p.GetComponent<PlayerCombat>();var pres=p.GetComponent<ActorPresentation>();var tryAttack=typeof(PlayerCombat).GetMethod("TryAttack",F);var next=typeof(PlayerCombat).GetField("nextAttack",F);var sb=new StringBuilder();
  float hitAt=-1,amount=0;Action<CombatFeedback.Result> on=r=>{if(r.target==dummy&&hitAt<0){hitAt=Time.time;amount=r.amount;}};CombatFeedback.Resolved+=on;
  try{for(int i=0;i<steps;i++){if(!dummy){sb.AppendLine("  target died");break;}Face(p,dummy);dummy.ApplyStun(3);hitAt=-1;float t0=Time.time;bool ok=(bool)tryAttack.Invoke(combat,null);float dur=(float)next.GetValue(combat)-t0;await Task.Delay(40);string st=pres.CurrentState;
   if(i==steps-1)ScreenCapture.CaptureScreenshot(Shots+tag+"_finisher.png");
   while(combat.IsSwinging&&Time.time-t0<3)await Task.Delay(10);
   sb.AppendLine($"  step{i+1} ok={ok} state={st} dur={dur:F2}s hit@={(hitAt<0?"none":((hitAt-t0)/dur).ToString("F2"))} dmg={amount:F0}");amount=0;await Task.Delay(30);}}
  finally{CombatFeedback.Resolved-=on;}return sb.ToString();
 }
 public static async Task<string> Shinobi(){
  var (p,room)=await Lab(PlayerClass.Shinobi);var sb=new StringBuilder("SHINOBI\n");var dummy=Dummy(p);await Task.Delay(300);
  try{
   sb.Append(await Chain(p,dummy,3,"shinobi"));
   var guard=p.GetComponent<ShinobiGuard>();Face(p,dummy);await Task.Delay(700);
   guard.SetGuard(false);guard.SetGuard(true);float rem=guard.Resolve(new DamageInfo{amount=20,melee=true,source=dummy.gameObject,direction=-p.transform.forward});
   var post=EnemyPosture.Of(dummy,false);sb.AppendLine($"perfect deflect: damage taken={rem} state={p.GetComponent<ActorPresentation>().CurrentState} enemyPosture={(post?post.Value:0):F0} counter={guard.ConsumeCounter()}");
   guard.SetGuard(false);await Task.Delay(500);
   bool broke=EnemyPosture.Add(dummy,100);await Task.Delay(400);ScreenCapture.CaptureScreenshot(Shots+"shinobi_posture_broken.png");await Task.Delay(200);
   float hp=dummy.Health;sb.Append("deathblow:\n"+await Chain(p,dummy,1,"shinobi_deathblow"));sb.AppendLine($"  broke={broke} hp {hp:F0}->{(dummy?dummy.Health:0):F0}");
   // Mortal blade
   if(dummy){UnityEngine.Object.Destroy(dummy.gameObject);}await Task.Delay(300);
   var a=Dummy(p,4);var b=Dummy(p,7);a.Initialize(p,10);b.Initialize(p,10);await Task.Delay(300);Face(p,a);a.ApplyStun(4);b.ApplyStun(4);
   var mech=p.GetComponent<PlayerMechanics>();p.State.ActiveWeapon.ultimateCharge=100;float ha=a.Health,hb=b.Health;bool started=mech.TryUltimate();var states=new List<string>();float t=Time.time;bool shot1=false,shot2=false;
   while(Time.time-t<2.2f){await Task.Delay(30);var s=p.GetComponent<ActorPresentation>().CurrentState;if(states.Count==0||states.Last()!=s)states.Add(s);if(!shot1&&Time.time-t>.5f){shot1=true;ScreenCapture.CaptureScreenshot(Shots+"mortal_draw.png");}if(!shot2&&Time.time-t>.9f){shot2=true;ScreenCapture.CaptureScreenshot(Shots+"mortal_slash.png");}}
   sb.AppendLine($"mortal blade started={started} states={string.Join(">",states)} near hp {ha:F0}->{(a?a.Health:0):F0} far hp {hb:F0}->{(b?b.Health:0):F0} postureBroken={(a&&EnemyPosture.Of(a,false)?.Broken==true)} timeScale={Time.timeScale}");
   if(a)UnityEngine.Object.Destroy(a.gameObject);if(b)UnityEngine.Object.Destroy(b.gameObject);
  }finally{if(dummy)UnityEngine.Object.Destroy(dummy.gameObject);p.SetNarrativeControlsBlocked(true);}
  return sb.ToString();
 }
 public static async Task<string> ThirdShinobi(){var p=RunDirector.Instance.Player;var cam=p.GetComponent<HumanPreviewCamera>();bool was=cam.ThirdPerson;if(!was)cam.Toggle();await Task.Delay(500);
  var pres=p.GetComponent<ActorPresentation>();string info="third="+cam.ThirdPerson+" body="+(pres.Body?pres.Body.name:"null")+" ctrl="+(pres.Body&&pres.Body.runtimeAnimatorController?pres.Body.runtimeAnimatorController.name:"null")+"\n";
  try{return info+await Shinobi();}finally{if(!was)cam.Toggle();}}
 public static async Task<string> Assasin(){
  var (p,room)=await Lab(PlayerClass.Berserker);var sb=new StringBuilder("ASSASIN dual="+p.State.DualWield+"\n");var dummy=Dummy(p);dummy.Initialize(p,25);await Task.Delay(300);
  try{sb.Append(await Chain(p,dummy,3,"assasin"));await Task.Delay(400);sb.Append("second chain:\n"+await Chain(p,dummy,2,"assasin2"));
   var pres=p.GetComponent<ActorPresentation>();await Task.Delay(900);sb.AppendLine("idle state="+pres.CurrentState+" layer weights="+string.Join(",",Enumerable.Range(0,pres.Body.layerCount).Select(l=>pres.Body.GetLayerName(l)+":"+pres.Body.GetLayerWeight(l).ToString("F1"))));
   ScreenCapture.CaptureScreenshot(Shots+"assasin_idle.png");
  }finally{if(dummy)UnityEngine.Object.Destroy(dummy.gameObject);p.SetNarrativeControlsBlocked(true);}
  return sb.ToString();
 }
}
