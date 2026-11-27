using UnityEngine;
using Codice.Characters;
using Codice.Presentation;
namespace Codice.Bosses
{
    public sealed partial class BossCharacter
    {
        GameObject angelWarning; Vector3 angelImpact; float angelRecovery; float angelChargeRemaining; bool angelChargeHit;
        readonly Color angelGold=new Color(1,.78f,.35f);
        void TickAngel(float dt)
        {
            if(!agent.enabled||!agent.isOnNavMesh)return;
            if(State==BossState.Stunned){State=BossState.Recovery;attackTimer=.45f;}
            if(State==BossState.Charging){TickAngelCharge(dt);return;}
            if(State==BossState.Windup)
            {
                agent.isStopped=true;windup-=dt;if(windup>0)return;
                if(CurrentAttack==BossAttack.Charge){if(angelWarning)Destroy(angelWarning);angelChargeRemaining=.6f;angelChargeHit=false;State=BossState.Charging;GetComponent<ActorPresentation>()?.Play("Run",.6f,true);return;}
                ResolveAngelAttack();CurrentAttack=BossAttack.None;State=BossState.Recovery;attackTimer=angelRecovery;return;
            }
            if(State==BossState.Recovery){agent.isStopped=true;attackTimer-=dt;if(attackTimer<=0)State=BossState.Idle;return;}
            Vector3 delta=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
            if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),360*dt);
            attackTimer-=dt;repath-=dt;agent.speed=5.6f*SpeedMultiplier;agent.acceleration=26;
            if(attackTimer<=0&&Vector3.Angle(transform.forward,delta)<4f&&delta.magnitude<23&&HasLineOfSight(Target.transform.position+Vector3.up))
            {
                bool distant=delta.magnitude>4;
                CurrentAttack=sequence%5==4?BossAttack.RadiantRain:Phase>=2&&sequence%4==3?BossAttack.BloodNova:distant?(sequence%2==0&&delta.magnitude<16?BossAttack.Charge:BossAttack.Bow):sequence%3==2?BossAttack.Slam:BossAttack.Sweep;
                sequence++;facing=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;transform.rotation=Quaternion.LookRotation(facing);aim=Target.transform.position+Vector3.up;
                angelImpact=CurrentAttack==BossAttack.Slam?transform.position+facing*2:transform.position;
                windup=CurrentAttack==BossAttack.RadiantRain?.85f:CurrentAttack==BossAttack.Charge?.65f:CurrentAttack==BossAttack.BloodNova?.95f:CurrentAttack==BossAttack.Slam?.75f:CurrentAttack==BossAttack.Bow?.8f:.48f;
                angelRecovery=CurrentAttack==BossAttack.Slam?1.1f:CurrentAttack==BossAttack.Sweep?.65f:.85f;
                if(angelWarning)Destroy(angelWarning);
                angelWarning=CurrentAttack==BossAttack.Bow||CurrentAttack==BossAttack.Charge?BloodVisual.Beam(transform.position+Vector3.up*1.5f,aim,angelGold,.045f):CurrentAttack==BossAttack.Sweep?BloodVisual.Arc(angelImpact+Vector3.up*.08f,3.8f,facing,130,angelGold):BloodVisual.Ring(angelImpact+Vector3.up*.08f,CurrentAttack==BossAttack.Slam?3:5,angelGold,.06f);
                Track(angelWarning);State=BossState.Windup;agent.isStopped=true;
                string clip=CurrentAttack==BossAttack.RadiantRain?"Nova":CurrentAttack==BossAttack.Charge?"Melee":CurrentAttack==BossAttack.Bow?"Shoot":CurrentAttack==BossAttack.Slam?"Slam":CurrentAttack==BossAttack.BloodNova?"Nova":sequence%2==0?"Melee":"SlashB";
                float impactFraction=CurrentAttack==BossAttack.Sweep?.58f:.68f;angelRecovery=windup/impactFraction-windup;
                GetComponent<ActorPresentation>()?.Play(clip,windup+angelRecovery,true);
                Debug.Log($"[Angel] Windup {CurrentAttack}; phase={Phase}; impactIn={windup:0.00}");return;
            }
            agent.isStopped=delta.magnitude<2.8f;State=agent.isStopped?BossState.Idle:BossState.Moving;
            if(!agent.isStopped&&repath<=0){repath=.25f;agent.SetDestination(Target.transform.position);}
        }
        void TickAngelCharge(float dt)
        {
            agent.isStopped=true;angelChargeRemaining-=dt;
            Vector3 destination=transform.position+facing*(12*dt);
            if(UnityEngine.AI.NavMesh.Raycast(transform.position,destination,out var wall,agent.areaMask)){destination=wall.position;angelChargeRemaining=0;}
            agent.Move(destination-transform.position);
            var delta=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
            if(!angelChargeHit&&delta.magnitude<2.4f&&Vector3.Dot(facing,delta.normalized)>0&&HasLineOfSight(Target.transform.position+Vector3.up))
            {angelChargeHit=true;Target.ReceiveDamage(new DamageInfo{amount=35,source=gameObject,direction=facing,melee=true});PresentationVfx.SwordSwing(transform.position+Vector3.up,facing);}
            if(angelChargeRemaining<=0){CurrentAttack=BossAttack.None;State=BossState.Recovery;attackTimer=.4f;Debug.Log("[Angel] Charge finished; recovery=.4");}
        }
        void ResolveAngelAttack()
        {
            if(angelWarning)Destroy(angelWarning);
            if(!IsAlive||Paused||!Target||!Target.State.IsAlive||State!=BossState.Windup)return;
            var origin=transform.position+Vector3.up*1.5f+facing*.8f;
            if(CurrentAttack==BossAttack.RadiantRain){for(int i=0;i<3+Phase;i++){var center=aim-Vector3.up+Quaternion.Euler(0,i*137.5f,0)*Vector3.forward*(i==0?0:2.5f);AngelJudgment.Create(this,center,.55f+i*.16f);} }
            else if(CurrentAttack==BossAttack.Bow||CurrentAttack==BossAttack.BloodNova)
            {
                bool nova=CurrentAttack==BossAttack.BloodNova;int count=nova?12:Phase==1?1:3;
                for(int i=0;i<count;i++){var direction=nova?Quaternion.Euler(0,i*360f/count,0)*Vector3.forward:Quaternion.Euler(0,(i-(count-1)*.5f)*12,0)*(aim-origin).normalized;AngelBolt.Create(this,origin,direction);}
            }
            else
            {
                var delta=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
                bool slam=CurrentAttack==BossAttack.Slam;
                bool inRange=slam?Vector3.ProjectOnPlane(Target.transform.position-angelImpact,Vector3.up).magnitude<=3:delta.magnitude<=3.8f&&Vector3.Dot(facing,delta.normalized)>=.42f;
                if(inRange&&HasLineOfSight(Target.transform.position+Vector3.up))Target.ReceiveDamage(new DamageInfo{amount=slam?42:30,source=gameObject,direction=facing,melee=true});
                PresentationVfx.SwordSwing(origin,facing);
                Codice.Mechanics.RelicPulse.Spawn(angelImpact+Vector3.up*.1f,slam?3:1.5f,angelGold);
            }
            var fx=ImportedAttackVfx.Burst(origin,false,.8f);if(fx)Track(fx);
            Debug.Log("[Angel] Impact "+CurrentAttack);
        }
    }
    public sealed class AngelJudgment:MonoBehaviour
    {
        BossCharacter owner;float remaining;GameObject warning;
        public static void Create(BossCharacter boss,Vector3 point,float delay){var go=new GameObject("Angel · juicio marcado");go.transform.position=point;var attack=go.AddComponent<AngelJudgment>();attack.owner=boss;attack.remaining=delay;attack.warning=BloodVisual.Ring(point+Vector3.up*.08f,1.45f,new Color(1,.65f,.2f),.07f);boss.Track(go);boss.Track(attack.warning);}
        void Update(){if(!owner||!owner.IsAlive||owner.State==BossState.Stunned||!owner.Target||!owner.Target.State.IsAlive){Destroy(gameObject);return;}if(owner.Paused)return;remaining-=Time.deltaTime;if(remaining>0)return;
            var point=transform.position;var target=owner.Target;
            if(Vector3.ProjectOnPlane(target.transform.position-point,Vector3.up).magnitude<=1.45f&&owner.HasLineOfSight(target.transform.position+Vector3.up))target.ReceiveDamage(new DamageInfo{amount=26,source=owner.gameObject,direction=Vector3.up});
            var fx=ImportedAttackVfx.Burst(point+Vector3.up*.2f,false,.9f);if(fx)owner.Track(fx);Codice.Mechanics.RelicPulse.Spawn(point+Vector3.up*.1f,1.45f,new Color(1,.8f,.3f));Destroy(gameObject);
        }
        void OnDestroy(){if(warning)Destroy(warning);}
    }
    public sealed class AngelBolt:MonoBehaviour
    {
        BossCharacter owner;Vector3 direction;float age;
        public static GameObject Create(BossCharacter boss,Vector3 origin,Vector3 direction)
        {
            var go=new GameObject("Angel · radiant lance");go.transform.position=origin;go.transform.rotation=Quaternion.LookRotation(direction);
            var bolt=go.AddComponent<AngelBolt>();bolt.owner=boss;bolt.direction=direction.normalized;boss.Track(go);
            SpellFlightVisual.Add(go,ProjectileStyle.Arcane);foreach(var line in go.GetComponentsInChildren<LineRenderer>()){line.startColor=new Color(1,.82f,.38f);line.endColor=new Color(1,.45f,.08f,0);}var trail=go.GetComponent<TrailRenderer>();trail.startColor=new Color(1,.82f,.38f);trail.endColor=new Color(1,.45f,.08f,0);return go;
        }
        void Update()
        {
            if(!owner||!owner.IsAlive||owner.State==BossState.Stunned){Destroy(gameObject);return;}
            if(owner.Paused)return;if((age+=Time.deltaTime)>4){Destroy(gameObject);return;}
            float distance=16*Time.deltaTime;
            var hits=Physics.SphereCastAll(transform.position,.17f,direction,distance,~0,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){if(hit.transform.IsChildOf(owner.transform)||hit.transform.IsChildOf(transform))continue;
                var player=hit.collider.GetComponentInParent<PlayerCharacter>();if(player)player.ReceiveDamage(new DamageInfo{amount=24,source=owner.gameObject,direction=direction});
                GetComponent<SpellFlightVisual>()?.Impact(hit.point);Destroy(gameObject);return;}
            transform.position+=direction*distance;
        }
    }
}
