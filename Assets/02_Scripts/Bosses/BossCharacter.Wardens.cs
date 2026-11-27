using System.Linq;
using UnityEngine;
using Codice.Characters;
using Codice.Presentation;
namespace Codice.Bosses
{
    public sealed partial class BossCharacter
    {
        // Native Muck visual rigs and clips; encounter logic belongs to Cinders Souls.
        public bool IsWarden => kind==BossKind.TwinBladeWarden||kind==BossKind.SpearWarden||kind==BossKind.StoneGuardian;
        GameObject wardenWarning;
        readonly Color wardenTint=new Color(.7f,.45f,.95f);
        float wardenElapsed,wardenDuration;
        float[] wardenHits;
        int wardenHitIndex;
        bool wardenSpin;
        void TickWarden(float dt)
        {
            if(!agent.enabled||!agent.isOnNavMesh)return;
            if(State==BossState.Stunned){wardenHits=null;State=BossState.Recovery;attackTimer=.7f;}
            if(State==BossState.Recovery){agent.isStopped=true;attackTimer-=dt;if(attackTimer<=0)State=BossState.Idle;return;}
            if(State==BossState.Windup)
            {
                agent.isStopped=true;wardenElapsed+=dt;
                if(kind==BossKind.StoneGuardian&&CurrentAttack==BossAttack.Bow&&Target){var to=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);if(to.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(to),55*dt);}
                // Every authored strike resolves once; frame hitches cannot skip a strike.
                while(wardenHits!=null&&wardenHitIndex<wardenHits.Length&&wardenElapsed>=wardenHits[wardenHitIndex]*wardenDuration){
                    if(wardenWarning)Destroy(wardenWarning);
                    ResolveWardenAttack();wardenHitIndex++;
                    if(wardenHitIndex<wardenHits.Length)ShowWardenTelegraph();
                }
                if(wardenElapsed>=wardenDuration){CurrentAttack=BossAttack.None;State=BossState.Recovery;attackTimer=.45f;wardenHits=null;}
                return;
            }
            var delta=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
            if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),240*dt);
            attackTimer-=dt;repath-=dt;agent.speed=(kind==BossKind.StoneGuardian?2.8f:4.5f)*SpeedMultiplier;
            if(attackTimer<=0&&delta.magnitude<22&&HasLineOfSight(Target.transform.position+Vector3.up))
            {
                facing=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;aim=Target.transform.position;
                if(kind==BossKind.StoneGuardian)CurrentAttack=delta.magnitude<4?BossAttack.Sweep:sequence%2==0?BossAttack.Slam:BossAttack.Bow;
                else CurrentAttack=delta.magnitude>5?BossAttack.Bow:kind==BossKind.SpearWarden&&sequence%3==2?BossAttack.Slam:BossAttack.Sweep;
                wardenSpin=kind==BossKind.SpearWarden&&CurrentAttack==BossAttack.Sweep&&sequence%2==1;sequence++;
                string animation=wardenSpin?"Spin":CurrentAttack==BossAttack.Bow?"Shoot":CurrentAttack==BossAttack.Slam?"Slam":"Melee";
                var presentation=GetComponent<ActorPresentation>();
                var clip=presentation.Body.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name==animation);
                wardenDuration=clip?clip.length:2;wardenElapsed=0;wardenHitIndex=0;
                wardenHits=wardenSpin?new[]{.32f,.51f,.70f}:kind==BossKind.TwinBladeWarden&&CurrentAttack==BossAttack.Sweep?new[]{.28f,.51f,.73f}:new[]{CurrentAttack==BossAttack.Bow?.57f:CurrentAttack==BossAttack.Slam?.62f:.45f};
                // Gurdi's chest laser follows the Muck clip: ~40 % charging in the chest, then a ~2.8 s tracking beam.
                if(kind==BossKind.StoneGuardian&&CurrentAttack==BossAttack.Bow){wardenDuration=Mathf.Clamp(wardenDuration/1.3f,5f,6.5f);wardenHits=new[]{.4f};}
                windup=wardenDuration*wardenHits[0];
                ShowWardenTelegraph();State=BossState.Windup;agent.isStopped=true;
                presentation.Play(animation,wardenDuration,true);
                Debug.Log($"[NativeBoss] {name} clip={animation} duration={wardenDuration:F2} hits={wardenHits.Length}");return;
            }
            agent.isStopped=delta.magnitude<3.2f;State=agent.isStopped?BossState.Idle:BossState.Moving;
            if(!agent.isStopped&&repath<=0){repath=.25f;agent.SetDestination(Target.transform.position);}
        }
        void ShowWardenTelegraph()
        {
            if(wardenWarning)Destroy(wardenWarning);
            if(kind==BossKind.StoneGuardian&&CurrentAttack==BossAttack.Bow){wardenWarning=GuardianEnergy.Charge(this,wardenDuration*.4f,wardenTint);Track(wardenWarning);return;}
            wardenWarning=CurrentAttack==BossAttack.Sweep?BloodVisual.Arc(transform.position+Vector3.up*.1f,kind==BossKind.SpearWarden?3.5f:3.2f,facing,wardenSpin?360:kind==BossKind.SpearWarden?98.9f:168.5f,wardenTint):CurrentAttack==BossAttack.Slam?BloodVisual.Ring(aim+Vector3.up*.1f,2,wardenTint,.06f):BloodVisual.Beam(transform.position+Vector3.up*1.4f,aim+Vector3.up,wardenTint,.05f);
            Track(wardenWarning);
        }
        void ResolveWardenAttack()
        {
            if(CurrentAttack==BossAttack.Bow)
            {
                if(kind==BossKind.StoneGuardian){
                    GuardianEnergy.Fire(this,Target,wardenDuration*.45f,wardenTint);
                }else if(false){
                    var origin=transform.position+Vector3.up*2.4f;
                    var end=aim+Vector3.up;
                    if(Physics.Raycast(origin,(end-origin).normalized,out var block,Vector3.Distance(origin,end),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))end=block.point;
                    var beam=BloodVisual.Beam(origin,end,wardenTint,.16f);Track(beam);Destroy(beam,.45f);
                    var discharge=ImportedAttackVfx.Burst(end,false,.45f);if(discharge)Track(discharge);
                    var offset=Vector3.ProjectOnPlane(Target.transform.position-aim,Vector3.up);
                    if(offset.magnitude<1.3f&&HasLineOfSight(Target.transform.position+Vector3.up))Target.ReceiveDamage(new DamageInfo{amount=32,source=gameObject,direction=facing});
                }else{
                    int count=kind==BossKind.TwinBladeWarden?2:Phase==1?1:3;
                    for(int i=0;i<count;i++){
                        var projectile=AngelBolt.Create(this,transform.position+Vector3.up*1.5f+facing,Quaternion.Euler(0,(i-(count-1)*.5f)*12,0)*(aim+Vector3.up-(transform.position+Vector3.up*1.5f)).normalized);
                        var flight=projectile.GetComponent<SpellFlightVisual>();if(flight){flight.enabled=false;Destroy(flight);}
                        foreach(Transform child in projectile.transform)Destroy(child.gameObject);
                        if(kind==BossKind.SpearWarden){
                            var trail=projectile.GetComponent<TrailRenderer>();
                            if(trail){trail.startColor=new Color(1,.44f,.12f,.9f);trail.endColor=new Color(.58f,.07f,.01f,0);trail.time=.16f;}
                            var ignition=ImportedAttackVfx.Burst(projectile.transform.position,true,.28f);if(ignition)Track(ignition);
                        }
                        var source=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Contains(kind==BossKind.SpearWarden?"ChiefSpear":"GoblinSword"));
                        if(source){
                            var steel=Instantiate(source.gameObject,projectile.transform,false);steel.name="Arma nativa arrojada";
                            steel.transform.localPosition=Vector3.zero;steel.transform.localRotation=Quaternion.identity;steel.transform.localScale=Vector3.one;steel.SetActive(true);
                            var bounds=new Bounds();bool first=true;
                            foreach(var filter in steel.GetComponentsInChildren<MeshFilter>(true))
                                for(int corner=0;corner<8;corner++){var meshBounds=filter.sharedMesh.bounds;var vertex=meshBounds.center+Vector3.Scale(meshBounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                                    var point=steel.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                                }
                            var size=bounds.size;float length=Mathf.Max(size.x,size.y,size.z);
                            steel.transform.localScale=Vector3.one*((kind==BossKind.SpearWarden?2.2f:1.6f)/Mathf.Max(.001f,length));
                            var axis=size.y>=size.x&&size.y>=size.z?Vector3.up:size.x>=size.z?Vector3.right:Vector3.forward;
                            steel.transform.localRotation=Quaternion.FromToRotation(axis,Vector3.forward);
                            steel.transform.localPosition=-(steel.transform.localRotation*Vector3.Scale(bounds.center,steel.transform.localScale));
                            // The flight sweep runs from the projectile origin; put the tip there so the shaft trails behind and never pierces the floor before impact.
                            float half=(kind==BossKind.SpearWarden?2.2f:1.6f)*.5f;steel.transform.localPosition-=Vector3.forward*half;
                        }
                    }
                }
            }
            else if(CurrentAttack==BossAttack.Slam)
            {
                var delta=Vector3.ProjectOnPlane(Target.transform.position-aim,Vector3.up);
                if(Mathf.Abs(Target.transform.position.y-aim.y)<1.25f&&delta.magnitude<=2&&HasLineOfSight(Target.transform.position+Vector3.up))Target.ReceiveDamage(new DamageInfo{amount=38,source=gameObject,direction=facing});
                var effect=ImportedAttackVfx.Burst(aim+Vector3.up*.2f,kind==BossKind.SpearWarden,.9f);if(effect)Track(effect);
                Codice.Mechanics.RelicPulse.Spawn(aim+Vector3.up*.1f,2,wardenTint);ImpactVfx.Slam(aim);
                if(kind==BossKind.StoneGuardian)GuardianEnergy.Shockwave(this,aim,Target,wardenTint);   /* Muck slam: energy ring along the floor */
            }
            else
            {
                var delta=Vector3.ProjectOnPlane(Target.transform.position-transform.position,Vector3.up);
                float dot=wardenSpin?-1:kind==BossKind.SpearWarden?.65f:.1f;
                if(Codice.Mechanics.MeleeContact.CanHit(transform,Target,kind==BossKind.SpearWarden?3.5f:3.2f,dot))Target.ReceiveDamage(new DamageInfo{amount=wardenSpin?20:26,source=gameObject,direction=facing,melee=true});
                PresentationVfx.SwordSwing(transform.position+Vector3.up*1.2f+facing,facing);
            }
        }
    }
}


