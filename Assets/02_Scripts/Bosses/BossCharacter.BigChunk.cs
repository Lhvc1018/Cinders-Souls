using UnityEngine;
using Codice.Characters;
using Codice.Presentation;
namespace Codice.Bosses {
 public sealed partial class BossCharacter {
  void TickBigChunk(float dt){
   if(!agent.enabled||!agent.isOnNavMesh)return;
   if(State==BossState.Stunned){State=BossState.Recovery;attackTimer=.8f;}
   if(State==BossState.Windup){agent.isStopped=true;windup-=dt;if(windup>0)return;
    Vector3 delta=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
    if(CurrentAttack==BossAttack.Bow){for(int i=-1;i<=1;i++)BigChunkRock.Spawn(this,transform.position+Vector3.up*10f,Quaternion.Euler(0,i*12,0)*(aim-transform.position-Vector3.up*10f).normalized);}
    else {float range=CurrentAttack==BossAttack.Slam?16:12.5f;PresentationVfx.Explosion(transform.position+facing*5);ImpactVfx.Slam(transform.position+facing*5);if(Codice.Mechanics.MeleeContact.CanHit(transform,Target,range,CurrentAttack==BossAttack.Slam?.35f:0f))Target.ReceiveDamage(new DamageInfo{amount=CurrentAttack==BossAttack.Slam?55:35,source=gameObject,direction=delta.normalized,melee=true,breaksGuard=CurrentAttack==BossAttack.Slam,knockback=6});}
    State=BossState.Recovery;CurrentAttack=BossAttack.None;attackTimer=1.4f;return;
   }
   if(State==BossState.Recovery){attackTimer-=dt;if(attackTimer<=0)State=BossState.Idle;return;}
   Vector3 offset=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
   if(offset.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(offset),70*dt);
   attackTimer-=dt;agent.speed=3.4f;agent.stoppingDistance=9;
   if(attackTimer<=0&&offset.magnitude<45&&HasLineOfSight(Target.transform.position+Vector3.up)){
    CurrentAttack=offset.magnitude>16?BossAttack.Bow:sequence++%2==0?BossAttack.Slam:BossAttack.Sweep;
    facing=offset.normalized;aim=Target.transform.position+Vector3.up;windup=CurrentAttack==BossAttack.Slam?1.4f:1.05f;State=BossState.Windup;agent.isStopped=true;
    GetComponent<ActorPresentation>()?.Play(CurrentAttack==BossAttack.Slam?"Slam":CurrentAttack==BossAttack.Bow?"Shoot":"Melee",windup+1.1f,true);
    var cue=CurrentAttack==BossAttack.Bow?BloodVisual.Beam(transform.position+Vector3.up*10,aim,new Color(1,.55f,.15f),.12f):BloodVisual.Arc(transform.position+Vector3.up*.08f,CurrentAttack==BossAttack.Slam?16:12.5f,facing,CurrentAttack==BossAttack.Slam?139:180,new Color(1,.55f,.15f));Track(cue);cue.AddComponent<BossWarningLifetime>().owner=this;Destroy(cue,windup);return;
   }
   agent.isStopped=offset.magnitude<9;State=agent.isStopped?BossState.Idle:BossState.Moving;if(!agent.isStopped)agent.SetDestination(Target.transform.position);
  }
 }
 public sealed class BigChunkRock:MonoBehaviour {
  BossCharacter owner;Vector3 velocity;float life;
  public static void Spawn(BossCharacter boss,Vector3 point,Vector3 direction){var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Big Chunk rock";Destroy(go.GetComponent<Collider>());go.transform.position=point;go.transform.localScale=Vector3.one*1.3f;var r=go.AddComponent<BigChunkRock>();r.owner=boss;r.velocity=direction*18;r.life=4;go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("ExpeditionEnemies/Terremotos_Basalto");}
  void Update(){if(owner&&owner.Paused)return;if(!owner||!owner.IsAlive||(life-=Time.deltaTime)<=0){Destroy(gameObject);return;}float distance=velocity.magnitude*Time.deltaTime;foreach(var hit in Physics.SphereCastAll(transform.position,.6f,velocity.normalized,distance,~0,QueryTriggerInteraction.Ignore)){if(hit.transform.IsChildOf(owner.transform))continue;var p=hit.collider.GetComponentInParent<PlayerCharacter>();if(p)p.ReceiveDamage(new DamageInfo{amount=30,source=owner.gameObject,direction=velocity.normalized});Destroy(gameObject);return;}transform.position+=velocity*Time.deltaTime;transform.Rotate(100*Time.deltaTime,50*Time.deltaTime,0);}
 }
}
