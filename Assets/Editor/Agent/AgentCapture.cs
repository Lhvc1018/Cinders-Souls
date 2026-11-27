using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;
using Codice.Characters;using Codice.Enemies;using Codice.Mechanics;using Codice.Presentation;
// Third-person captures of attacks for visual review (panel hidden, prefs restored).
public static class AgentCapture {
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../MapReview/agent/play/"));
 static async Task<string> Run(PlayerClass role,string tag,int steps,bool ultimate){
  Directory.CreateDirectory(Dir);var p=RunDirector.Instance.Player;var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();room.ClearEnemies();room.ChangeClass(role);await Task.Delay(500);
  typeof(CombatTestRoom).GetMethod("SetPanel",F).Invoke(room,new object[]{false});p.SetNarrativeControlsBlocked(false);
  var cam=p.GetComponent<HumanPreviewCamera>();bool was=cam.ThirdPerson;if(!was)cam.Toggle();await Task.Delay(600);if(!cam.ThirdPerson)cam.Toggle();await Task.Delay(300);
  var prefab=Resources.Load<BasicEnemy>("ExpeditionEnemies/MuckResidents/Salteador de la cripta");NavMesh.SamplePosition(p.transform.position+p.transform.forward*2.6f,out var hit,3,NavMesh.AllAreas);
  var e=UnityEngine.Object.Instantiate(prefab,hit.position,Quaternion.LookRotation(-p.transform.forward));e.Initialize(p,25);await Task.Delay(300);
  var combat=p.GetComponent<PlayerCombat>();var tryAttack=typeof(PlayerCombat).GetMethod("TryAttack",F);var next=typeof(PlayerCombat).GetField("nextAttack",F);int shot=0;
  try{
   if(ultimate){e.ApplyStun(5);p.State.ActiveWeapon.ultimateCharge=100;p.GetComponent<PlayerMechanics>().TryUltimate();foreach(var t in new[]{.4f,.9f,1.25f,1.4f,1.6f}){await WaitUnscaled(t);ScreenCapture.CaptureScreenshot(Dir+tag+"_"+(shot++)+".png",1);} }
   else for(int i=0;i<steps;i++){if(!e)break;e.ApplyStun(3);var dir=Vector3.ProjectOnPlane(e.transform.position-p.transform.position,Vector3.up).normalized;p.transform.rotation=Quaternion.LookRotation(dir);
    float t0=Time.time;tryAttack.Invoke(combat,null);float dur=(float)next.GetValue(combat)-t0;
    foreach(var k in new[]{.15f,.4f,.7f}){while(Time.time-t0<dur*k)await Task.Yield();ScreenCapture.CaptureScreenshot(Dir+tag+"_"+(shot++)+".png",1);}
    while(combat.IsSwinging)await Task.Yield();}
   await Task.Delay(300);
  }finally{if(e)UnityEngine.Object.Destroy(e.gameObject);if(!was)cam.Toggle();typeof(CombatTestRoom).GetMethod("SetPanel",F).Invoke(room,new object[]{true});}
  return "captured "+shot+" frames "+tag;
 }
 static float start;static async Task WaitUnscaled(float t){if(t<.3f)start=Time.unscaledTime;while(Time.unscaledTime-start<t)await Task.Yield();}
 public static Task<string> Shinobi()=>Run(PlayerClass.Shinobi,"shinobiB",3,false);
 public static Task<string> Mortal()=>Run(PlayerClass.Shinobi,"mortalB",0,true);
 public static Task<string> Assasin()=>Run(PlayerClass.Berserker,"assasinB",3,false);
}
