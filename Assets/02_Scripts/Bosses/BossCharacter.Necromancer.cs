using UnityEngine;
using UnityEngine.AI;
using Codice.Characters;
using Codice.Presentation;
namespace Codice.Bosses {
 public sealed partial class BossCharacter {
  float necroSummon=6;bool necroRitual;GameObject necroWarning;
  void TickNecromancer(float dt){
   if(!agent.enabled||!agent.isOnNavMesh)return;
   if(State==BossState.Stunned){if(necroWarning)Destroy(necroWarning);State=BossState.Recovery;CurrentAttack=BossAttack.None;attackTimer=1;}
   necroSummon-=dt;var delta=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
   if(State==BossState.Windup){agent.isStopped=true;windup-=dt;if(windup>0)return;
    if(necroWarning)Destroy(necroWarning);
    if(necroRitual){RaiseDead();necroSummon=Phase==1?14:10;}
    else if(CurrentAttack==BossAttack.Slam){Codice.Mechanics.RelicPulse.Spawn(aim,3.5f,new Color(.3f,.9f,.6f));if(Vector3.ProjectOnPlane(Target.transform.position-aim,Vector3.up).magnitude<3.5f)Target.ReceiveDamage(new DamageInfo{amount=32,source=gameObject,direction=Vector3.up});}
    else{var origin=transform.position+Vector3.up*2;var direction=(aim-origin).normalized;for(int n=-1;n<=1;n++)NecroBolt.Create(this,origin,Quaternion.Euler(0,n*12,0)*direction);}
    CurrentAttack=BossAttack.None;State=BossState.Recovery;attackTimer=Phase==1?1.5f:1;return;
   }
   if(State==BossState.Recovery){attackTimer-=dt;if(attackTimer<=0)State=BossState.Idle;return;}
   if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),130*dt);
   attackTimer-=dt;
   if(attackTimer<=0&&delta.magnitude<30&&HasLineOfSight(Target.transform.position+Vector3.up)){
    necroRitual=necroSummon<=0&&LivingSummons<8;CurrentAttack=necroRitual?BossAttack.None:sequence++%3==2?BossAttack.Slam:BossAttack.Bow;
    aim=Target.transform.position;windup=necroRitual?1.8f:CurrentAttack==BossAttack.Slam?1.4f:1.05f;State=BossState.Windup;agent.isStopped=true;
    GetComponent<ActorPresentation>()?.Play(necroRitual?"Summon":CurrentAttack==BossAttack.Slam?"Slam":"Shoot",windup+.65f,true);
    necroWarning=!necroRitual&&CurrentAttack==BossAttack.Bow?BloodVisual.Beam(transform.position+Vector3.up*2,aim,new Color(.3f,.85f,.65f),.06f):BloodVisual.Ring((necroRitual?transform.position:aim)+Vector3.up*.08f,necroRitual?5:3.5f,new Color(.3f,.85f,.65f),.08f);Track(necroWarning);if(!necroRitual&&CurrentAttack==BossAttack.Bow){var origin=transform.position+Vector3.up*2;for(int i=-1;i<=1;i+=2){var ray=BloodVisual.Beam(origin,origin+Quaternion.Euler(0,i*12,0)*(aim-origin),new Color(.3f,.85f,.65f),.04f);ray.transform.SetParent(necroWarning.transform,true);}}return;
   }
   agent.speed=2.6f;agent.stoppingDistance=10;agent.isStopped=delta.magnitude<=10;State=agent.isStopped?BossState.Idle:BossState.Moving;if(!agent.isStopped)agent.SetDestination(Target.transform.position);
  }
  void RaiseDead(){
   if(summons==null||summons.Length==0)return;int desired=Phase==1?3:4;
   for(int n=0;n<desired&&LivingSummons<8;n++){
    var prefab=summons[n%summons.Length];if(!prefab)continue;Vector3 pos=transform.position+Quaternion.Euler(0,sequence*39+n*360f/desired,0)*Vector3.forward*7;
    if(!NavMesh.SamplePosition(pos,out var hit,2,agent.areaMask)||Vector3.Distance(hit.position,Target.transform.position)<4)continue;
    var path=new NavMeshPath();if(!NavMesh.CalculatePath(hit.position,Target.transform.position,agent.areaMask,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
    var e=Instantiate(prefab,hit.position,Quaternion.identity);e.Initialize(Target,Level);minions.Add(e);Codice.Mechanics.RelicPulse.Spawn(hit.position,1.2f,new Color(.3f,.85f,.65f));
   }
   Debug.Log("[Necromancer] Living summons="+LivingSummons);
  }
 }
 public sealed class NecroBolt:MonoBehaviour{
  BossCharacter owner;Vector3 direction;float life;
  public static void Create(BossCharacter boss,Vector3 p,Vector3 dir){var g=new GameObject("Necromancer soul bolt");g.transform.position=p;g.transform.rotation=Quaternion.LookRotation(dir);var b=g.AddComponent<NecroBolt>();b.owner=boss;b.direction=dir;boss.Track(g);SpellFlightVisual.Add(g,ProjectileStyle.Arcane);foreach(var l in g.GetComponentsInChildren<LineRenderer>()){l.startColor=new Color(.2f,1,.6f);l.endColor=new Color(.05f,.3f,.2f,0);}}
  void Update(){if(!owner||!owner.IsAlive||owner.State==BossState.Stunned||!owner.Target||!owner.Target.State.IsAlive){Destroy(gameObject);return;}if(owner.Paused)return;if((life+=Time.deltaTime)>5){Destroy(gameObject);return;}float distance=13*Time.deltaTime;var hits=Physics.SphereCastAll(transform.position,.16f,direction,distance,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var h in hits){if(h.transform.IsChildOf(owner.transform)||h.collider.GetComponentInParent<Codice.Enemies.BasicEnemy>())continue;var p=h.collider.GetComponentInParent<PlayerCharacter>();if(p)p.ReceiveDamage(new DamageInfo{amount=22,source=owner.gameObject,direction=direction});GetComponent<SpellFlightVisual>()?.Impact(h.point);Destroy(gameObject);return;}transform.position+=direction*distance;}
 }
}
