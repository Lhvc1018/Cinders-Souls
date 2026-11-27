using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Codice.Bosses;
using Codice.Expedition;
using Codice.Characters;
using Codice.UI;
public static class ValidateNova
{
 static void Set(BossCharacter b,string property,object value)=>typeof(BossCharacter).GetProperty(property).SetValue(b,value);
 public static async Task<string> Run()
 {
  var f=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();var p=f.context.player;p.enabled=false;RunHUD.Instance.SetPaused(false);Time.timeScale=1;
  for(int i=0;i<f.Layout.Boss;i++)f.Cleared[i]=true;
  var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=f.Center(f.Layout.Boss)+Vector3.back*6+Vector3.up*.1f;cc.enabled=true;
  if(!f.EnterRoom(f.Layout.Boss))throw new Exception("Boss entry failed");await Task.Delay(200);
  var b=f.context.arena.ActiveBoss;Set(b,"Health",b.MaxHealth*.6f);Set(b,"State",BossState.Idle);
  typeof(BossCharacter).GetField("sequence",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(b,3);
  typeof(BossCharacter).GetField("attackTimer",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(b,-1f);
  for(int i=0;i<100&&b.CurrentAttack!=BossAttack.BloodNova;i++)await Task.Delay(30);
  if(b.CurrentAttack!=BossAttack.BloodNova)throw new Exception("Nova not selected");
  for(int i=0;i<100&&b.State!=BossState.Recovery;i++)await Task.Delay(20);
  int count=UnityEngine.Object.FindObjectsByType<InquisitorBolt>().Length;if(count!=10)throw new Exception("Expected 10 nova bolts, got "+count);
  b.ApplyStun(1);await Task.Delay(100);if(UnityEngine.Object.FindObjectsByType<InquisitorBolt>().Length!=0)throw new Exception("Nova not cancelled by stun");
  Debug.Log("[Upgrade validation] Boss room, phase 2 nova windup, 10 bolts, recovery and stun cleanup passed");Time.timeScale=0;return "Nova validation passed";
 }
}
