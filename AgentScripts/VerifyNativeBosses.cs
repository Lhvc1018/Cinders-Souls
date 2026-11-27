using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;using Codice.Bosses;using Codice.Characters;using Codice.Mechanics;using Codice.Presentation;
public static class VerifyNativeBosses {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static async Task<string> Run(){
  for(int ready=0;ready<100&&(!RunDirector.Instance||!RunDirector.Instance.Player);ready++)await Task.Delay(100);var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();var p=RunDirector.Instance.Player;Check(room&&p,"Laboratory not initialized");
  room.ClearEnemies();var flags=BindingFlags.Instance|BindingFlags.NonPublic;int tested=0,clips=0;
  foreach(var prefab in room.enemyPrefabs.Where(x=>x&&x.GetComponent<BossCharacter>()&&x.GetComponent<BossCharacter>().IsWarden)){
   var b=UnityEngine.Object.Instantiate(prefab).GetComponent<BossCharacter>();try{
    b.Initialize(p,1);b.enabled=false;var agent=b.GetComponent<NavMeshAgent>();
    Check(NavMesh.SamplePosition(p.transform.position+p.transform.forward*10,out var hit,8,NavMesh.AllAreas),"No NavMesh");agent.Warp(hit.position);
    await Task.Delay(150);var presentation=b.GetComponent<ActorPresentation>();presentation.enabled=false;var animator=presentation.Body;Check(animator,"Missing animator");
    var transforms=animator.GetComponentsInChildren<Transform>();animator.speed=1;
    foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct()){
     animator.Play(clip.name,0,0);animator.Update(0);var first=transforms.Select(t=>t.localRotation).ToArray();var positions=transforms.Select(t=>t.localPosition).ToArray();
     animator.Update(clip.length*.37f);
     bool moved=transforms.Where((t,i)=>Quaternion.Angle(first[i],t.localRotation)>.1f||Vector3.Distance(positions[i],t.localPosition)>.001f).Any();
     Check(moved,"No bone movement: "+prefab.name+"/"+clip.name);clips++;
    }
    var tick=typeof(BossCharacter).GetMethod("TickWarden",flags);typeof(BossCharacter).GetField("attackTimer",flags).SetValue(b,0f);
    tick.Invoke(b,new object[]{.01f});Check(b.State==BossState.Windup,"No attack selected "+prefab.name);
    tick.Invoke(b,new object[]{9f});Check(b.State==BossState.Recovery,"Attack never finished");if(b.kind!=BossKind.StoneGuardian){var weapons=UnityEngine.Object.FindObjectsByType<Transform>().Where(m=>m.name=="Arma nativa arrojada").ToArray();Check(weapons.Length>0,"Native projectile mesh missing");Check(weapons.All(m=>m.transform.lossyScale.x>.001f),"Projectile inherited hidden weapon scale");}
    b.ApplyStun(.3f);float hp=p.State.Health;tick.Invoke(b,new object[]{.01f});Check(b.State==BossState.Recovery&&p.State.Health==hp,"Stun leaked damage");
    b.ReceiveDamage(new DamageInfo{amount=999999,source=p.gameObject});await Task.Delay(100);Check(!b,"Death cleanup failed");tested++;
    Debug.Log("[NativeBossTest] PASS "+prefab.name+": native bones animated; attack/recovery/stun/death");
   }finally{if(b)UnityEngine.Object.Destroy(b.gameObject);}
  }
  Check(tested==3,"Expected three native bosses");
  Check(p.State.LaboratoryImmortal,"Lab immortality missing");p.State.NetworkRevive();
  return "PASS "+tested+" native bosses; "+clips+" moving animation clips; attack recovery, stun, death and lab immortality";
 }
}


