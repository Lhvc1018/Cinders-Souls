using System;
using System.Collections;
using System.Collections.Generic;
using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class PlayerMechanics : MonoBehaviour
    {
        public event Action Changed;
        public bool UltimateActive { get; private set; }
        public bool Invulnerable => Time.time < invulnerableUntil;
        public bool ShieldActive => (GetComponent<PlayerCombat>()?.Blocking ?? false) || (Time.time < shieldUntil && player.State.ActiveWeapon?.kind == WeaponKind.Shield);
        public bool ShadowStep => Time.time < shadowUntil;
        public float Charge => player.State.ActiveWeapon?.ultimateCharge ?? 0;
        PlayerCharacter player;
        float invulnerableUntil, shadowUntil, shieldUntil;
        public bool UltimateQueued => queuedWeapon != null;
        WeaponInstance queuedWeapon;
        int bossHits; WeaponInstance ultimateWeapon; Codice.Presentation.StaffBeamVisual staffBeam;
        void Awake() { player = GetComponent<PlayerCharacter>(); }
        void OnEnable() { player.SpecialRequested += ActivateFromInput; player.State.Died += Cancel; }
        void OnDisable() { player.SpecialRequested -= ActivateFromInput; player.State.Died -= Cancel; Cancel(); }
        void ActivateFromInput() { RequestUltimate(); }
        public bool RequestUltimate()
        {
            if(TryUltimate())return true;
            var weapon=player.State.ActiveWeapon;
            if(weapon!=null&&weapon.ultimateCharge>=100&&GetComponent<PlayerCombat>().IsSwinging&&CanUseUltimate())
            { queuedWeapon=weapon;return true; }
            string reason=weapon==null?"Equipa un arma":weapon.ultimateCharge<100?"Especial: falta carga":weapon.kind==WeaponKind.Staff&&player.State.Mana<CharacterState.StaffUltimateManaCost?"Especial: necesitas 40 de maná":"Especial no disponible en este momento";
            Codice.UI.RunHUD.Instance?.Announce(reason);return false;
        }
        bool CanUseUltimate() => !Codice.UI.RunNavigation.Loading && !(Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.ResultsVisible) && player.State.IsAlive && !player.ControlsBlocked && !player.IsStaggered && Time.timeScale>0 && !UltimateActive;
        public void Cancel()
        { queuedWeapon=null; StopAllCoroutines(); var held=GetComponent<Codice.Presentation.HumanPreviewCamera>()?.HeldWeapon; if(held) held.GetComponent<Codice.Presentation.MortalBladeVisual>()?.End(); if(staffBeam)Destroy(staffBeam.gameObject); UltimateActive = false; ultimateWeapon=null; invulnerableUntil = shadowUntil = shieldUntil = 0; bossHits = 0; }
        void Update()
        {
            if(Codice.Multiplayer.LanSession.Client)return;
            if(queuedWeapon!=null)
            {
                if(player.State.ActiveWeapon!=queuedWeapon||!CanUseUltimate())queuedWeapon=null;
                else if(!GetComponent<PlayerCombat>().IsSwinging){queuedWeapon=null;RequestUltimate();}
            }
            if(UltimateActive&&(player.State.ActiveWeapon!=ultimateWeapon||player.IsStaggered||player.ControlsBlocked||Codice.UI.RunNavigation.Loading))Cancel();
            if (!player.State.IsAlive || Time.timeScale == 0 || player.ControlsBlocked) return;
            bool combat = RunDirector.Instance && (RunDirector.Instance.Player == player || Codice.Multiplayer.LanAvatar.IsRemote(this)) && RunDirector.Instance.CombatActive;
            if (combat) AddCharge(1.5f * Time.deltaTime);
        }
        public void AddCharge(float amount)
        {
            var weapon = player.State.ActiveWeapon;
            if (weapon == null || !player.State.IsAlive || UltimateActive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            bool ready = weapon.ultimateCharge >= 100;
            weapon.ultimateCharge = Mathf.Clamp(weapon.ultimateCharge + amount * (player.State.HasPassive(PassiveKind.Overload) ? 1.5f : 1), 0, 100);
            if (!ready && weapon.ultimateCharge >= 100) Debug.Log("Especial listo: Q (" + weapon.kind + ").", this);
            Changed?.Invoke();
        }
        public void EnemyKilled(float charge)
        {
            AddCharge(charge);
            player.State.RelicKill();player.State.BoonKill();
            if (player.State.HasPassive(PassiveKind.VampiricHeart)) player.State.Heal(player.State.MaxHealth * .04f);
        }
        public void BossHit()
        {
            AddCharge(2);
            if (++bossHits % 10 == 0 && player.State.HasPassive(PassiveKind.VampiricHeart)) player.State.Heal(player.State.MaxHealth * .04f);
        }
        public float ModifyIncoming(DamageInfo hit)
        {
            float damage = hit.amount * (player.State.HasPassive(PassiveKind.ThornArmor) ? .8f : 1);
            if(player.State.WardRemaining>0)damage*=.65f;
            if (ShieldActive && Vector3.Dot(transform.forward, -hit.direction.normalized) >= .5f) damage *= .5f;
            return damage;
        }
        public void AfterDamage(DamageInfo hit)
        {
            if (hit.melee && !hit.secondary && hit.source && player.State.HasPassive(PassiveKind.ThornArmor))
                hit.source.GetComponentInParent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo { amount = 35, source = gameObject, direction = -hit.direction, secondary = true, reflected = true });
        }
        public void RaiseShield() { shieldUntil = Time.time + .65f; }
        public float RollBasicDamage(WeaponInstance weapon)
        {
            bool critical = player.ConsumePerfectDodge() || ShadowStep || (weapon.kind == WeaponKind.Dagger && UnityEngine.Random.value < .25f) || (weapon.kind == WeaponKind.Bow && UnityEngine.Random.value < .1f) || UnityEngine.Random.value < ClassArsenal.CritBonus(player.State, weapon) + player.State.BoonCrit;
            return weapon.Damage * player.State.ClassDamageMultiplier * ClassArsenal.DamageFactor(player.State, weapon) * player.State.BoonDamage * (critical ? 2 : 1)*player.State.BlessingDamage*(GetComponent<RunSpellbook>()?.DamageFactor??1);
        }
        public bool TryUltimate()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            var weapon = player.State.ActiveWeapon;
            if (Codice.UI.RunNavigation.Loading || (Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.ResultsVisible) || GetComponent<PlayerCombat>().IsSwinging || !player.State.IsAlive || player.ControlsBlocked || player.IsStaggered || Time.timeScale == 0 || UltimateActive || weapon == null || weapon.ultimateCharge < 100) return false;
            if(weapon.kind==WeaponKind.Staff&&!player.State.SpendMana(CharacterState.StaffUltimateManaCost))return false;
            queuedWeapon=null; weapon.ultimateCharge = 0; ultimateWeapon=weapon; UltimateActive = true; Changed?.Invoke();
            StartCoroutine(Execute(weapon.kind, weapon.tier)); return true;
        }
        IEnumerator Execute(WeaponKind kind,int tier)
        {
            float damage=Mathf.Lerp(180,450,(tier-1)/3f);
            var pose=GetComponent<Codice.Presentation.ActorPresentation>();
            if(kind==WeaponKind.Sword&&player.State.ActiveWeapon.katana){
                // Hoja mortal: draw a crimson blade (0.75 s, protected), then one wide cut that pierces the line
                // and finishes any broken posture. Every target left standing has its posture broken.
                invulnerableUntil=Time.time+2.4f;
                var grip=GetComponent<Codice.Presentation.HumanPreviewCamera>()?.HeldWeapon;
                var mortal=Codice.Presentation.MortalBladeVisual.Begin(grip);
                pose?.Play("MortalDraw",.75f,true);Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Magic);
                var hand=grip?grip.position:transform.position+Vector3.up*1.1f;
                RelicPulse.Spawn(transform.position+Vector3.up*.08f,2.2f,new Color(.9f,.08f,.05f));
                Codice.Presentation.ImportedAttackVfx.Burst(hand,true,.45f);
                Codice.Presentation.LordsAttackVfx.Cast(hand,transform.forward);
                yield return new WaitForSeconds(.75f);
                // Lords two-handed sweep: strike lands at ~34% of the clip (played at 2.4x).
                pose?.Play("MortalSlash",1.9f,true);Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Heavy);
                yield return new WaitForSeconds(.6f);
                Vector3 origin=transform.position+Vector3.up*1.05f;Vector3 forward=Vector3.ProjectOnPlane(player.view.transform.forward,Vector3.up).normalized;
                if(forward.sqrMagnitude<.01f)forward=transform.forward;
                float length=WorldRange(origin,forward,11);
                for(int k=0;k<4;k++)Codice.Presentation.LordsAttackVfx.SlashTinted(origin+forward*(1+k*length/4),forward,new Color(1,.12f,.06f,.95f),2.6f-k*.35f,-8+k*6,.4f);
                Codice.Presentation.SpecialVisuals.Slash(origin,forward,length,false,true);
                var struck=new List<Component>(Line(origin,forward,length,2.4f));
                foreach(var t in CombatTargets.InRadius(transform.position+Vector3.up,3.2f))if(!struck.Contains(t))struck.Add(t);
                foreach(var target in struck){
                    float mult=EnemyPosture.Deathblow(target);
                    CombatTargets.Hit(target,player,damage*1.8f*mult,true,4);
                    if(target&&HealthOf(target)>0)EnemyPosture.Add(target,200);
                    var at=target?target.transform.position+Vector3.up*1.2f:origin;
                    Codice.Presentation.ImpactVfx.Spawn("VFX_Critical_01",at,-forward,.6f);Codice.Presentation.ImpactVfx.Spawn("VFX_Blood_02",at,forward,.45f);
                }
                Codice.Presentation.HitStop.Do(struck.Count>0?.12f:.06f);
                yield return new WaitForSeconds(.7f);
                if(mortal)mortal.End();
            }else if(kind==WeaponKind.Sword){
                pose?.Special(kind,.7f);yield return new WaitForSeconds(.3f);Slash(damage,10,3,false);yield return new WaitForSeconds(.4f);
            }else if(kind==WeaponKind.Staff){
                pose?.Play("StaffChannel",2.5f,true);
                staffBeam=Codice.Presentation.StaffBeamVisual.Create(player);
                Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.SpellArcane,transform.position);
                // Aim exactly where the beam is drawn (works in first and third person): 26 ticks over 2.6 s, ~2.9x total damage.
                for(int i=0;i<26;i++){
                    Vector3 origin=Codice.Presentation.HumanPreviewCamera.Origin(player),forward=Codice.Presentation.HumanPreviewCamera.Aim(player);
                    float length=WorldRange(origin,forward,28);
                    foreach(var target in Line(origin,forward,length,1.3f)){CombatTargets.Hit(target,player,damage/9,true,0,true);if(i%3==0)Codice.Presentation.PresentationVfx.Impact(target.transform.position+Vector3.up,-forward);}
                    foreach(var col in Physics.OverlapCapsule(origin,origin+forward*length,1.3f,~0,QueryTriggerInteraction.Ignore)){var dummy=col.GetComponentInParent<Codice.Prologue.TrainingDummy>();if(dummy&&i%2==0)dummy.ReceiveDamage(new DamageInfo{amount=damage/9,direction=forward,source=gameObject});}
                    if(i%5==4)Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.SpellLightning,origin+forward*length);
                    yield return new WaitForSeconds(.1f);
                }
            }else if(kind==WeaponKind.Hammer||kind==WeaponKind.Axe){
                pose?.Special(kind,.8f);yield return new WaitForSeconds(.4f);
                Vector3 origin=transform.position+Vector3.up*.15f;
                Codice.Presentation.SpecialVisuals.Slash(origin,transform.forward,7,true,false);
                Codice.Presentation.PresentationVfx.Explosion(origin);
                foreach(var target in CombatTargets.InRadius(origin+Vector3.up*.5f,7)){CombatTargets.Hit(target,player,damage,true,5);CombatTargets.Stun(target,2);}
                yield return new WaitForSeconds(.4f);
            }else if(kind==WeaponKind.Dagger){
                for(int i=0;i<3;i++){pose?.Special(kind,.45f);yield return new WaitForSeconds(.18f);Slash(damage/3,3,1.1f,false);yield return new WaitForSeconds(.27f);}
            }else if(kind==WeaponKind.Scythe){
                pose?.Special(kind,.8f);yield return new WaitForSeconds(.25f);
                Vector3 origin=transform.position+Vector3.up;
                Codice.Presentation.SpecialVisuals.Slash(origin,transform.forward,7,true,true);
                foreach(var target in CombatTargets.InRadius(origin,7))CombatTargets.Hit(target,player,damage,true,3);
                yield return new WaitForSeconds(.55f);
            }else if(kind==WeaponKind.Musket){
                for(int i=0;i<8;i++){
                    pose?.Attack(true,.18f);Vector3 origin=player.view.transform.position,forward=player.view.transform.forward;float length=WorldRange(origin,forward,60);
                    Codice.Presentation.SpecialVisuals.Beam(origin+forward*.6f,origin+forward*length,.1f,true);
                    Codice.Presentation.PresentationVfx.Muzzle(origin+forward*.8f,forward);Codice.Presentation.PresentationVfx.GunSmoke(origin+forward*.8f,forward);
                    foreach(var target in Line(origin,forward,length,.35f))CombatTargets.Hit(target,player,damage/4,true);
                    yield return new WaitForSeconds(.18f);
                }
            }else if(kind==WeaponKind.Bow){
                for(int volley=0;volley<3;volley++){
                    pose?.Attack(true,.3f);
                    for(int i=-2;i<=2;i++){
                        var shot=new GameObject("Ultimate volley").AddComponent<PlayerProjectile>();shot.transform.position=player.view.transform.position;
                        shot.Initialize(gameObject,Quaternion.AngleAxis(i*5,Vector3.up)*player.view.transform.forward,damage/8,~0);
                        if(player.State.ActiveWeapon.shuriken)shot.AddShurikenVisual();else shot.AddArrowVisual();
                    }
                    yield return new WaitForSeconds(.3f);
                }
            }else if(kind==WeaponKind.Shield){
                pose?.Special(kind,.7f);invulnerableUntil=Time.time+4;yield return new WaitForSeconds(4);Area(transform.position+Vector3.up,6,damage,true,8);
            }else{pose?.Special(kind,.5f);Slash(damage,12,1.2f,false);yield return new WaitForSeconds(.5f);}
            if(staffBeam)Destroy(staffBeam.gameObject);UltimateActive=false;ultimateWeapon=null;Changed?.Invoke();
        }
        void Slash(float damage,float range,float width,bool blood)
        {
            var forward=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;var origin=transform.position+Vector3.up*1.1f+forward*.7f;
            float length=WorldRange(origin,forward,range);
            Codice.Presentation.SpecialVisuals.Slash(origin,forward,Mathf.Min(length,3.2f),false,blood);
            foreach(var target in Line(origin,forward,length,width))CombatTargets.Hit(target,player,damage,true);
        }
        float WorldRange(Vector3 origin,Vector3 forward,float range)
        {
            foreach(var hit in Physics.RaycastAll(origin,forward,range,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.GetComponentInParent<PlayerCharacter>()&&!CombatTargets.IsEnemy(hit.collider.GetComponentInParent<IDamageReceiver>() as Component))range=Mathf.Min(range,hit.distance);
            return range;
        }
        IEnumerator Bleed(Component target, float total)
        {
            for (int i = 0; i < 3; i++)
            { yield return new WaitForSeconds(1); if (!target || !player.State.IsAlive) yield break; CombatTargets.Hit(target, player, total / 3, true, 0, true); }
        }
        static float HealthOf(Component target)
        {
            if (!target) return 0;
            if (target is Codice.Enemies.BasicEnemy enemy) return enemy.Health;
            if (target is Codice.Bosses.BossCharacter boss) return boss.Health;
            return 0;
        }
        void Area(Vector3 center, float range, float damage, bool guard, float push = 0)
        { foreach (var target in CombatTargets.InRadius(center, range)) CombatTargets.Hit(target, player, damage, guard, push); }
        List<Component> Line(Vector3 origin, Vector3 direction, float range, float width)
        {
            // Stop the corridor at solid world geometry, while allowing penetration through enemies.
            foreach (var hit in Physics.RaycastAll(origin, direction, range, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.GetComponentInParent<PlayerCharacter>() && !CombatTargets.IsEnemy(hit.collider.GetComponentInParent<IDamageReceiver>() as Component)) range = Mathf.Min(range, hit.distance);
            var result = new List<Component>();
            direction=direction.normalized;
            // Test the actual collider volume, including large enemies at the end of the beam.
            foreach (var collider in Physics.OverlapCapsule(origin,origin+direction*range,width,~0,QueryTriggerInteraction.Ignore))
            {
                var target=collider.GetComponentInParent<IDamageReceiver>() as Component;
                if(!CombatTargets.IsEnemy(target)||result.Contains(target))continue;
                float along=Mathf.Clamp(Vector3.Dot(collider.bounds.center-origin,direction),0,range);
                Vector3 point=collider.ClosestPoint(origin+direction*along);
                Vector3 delta=point-origin;
                bool blocked=false;
                foreach(var hit in Physics.RaycastAll(origin,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.collider.GetComponentInParent<PlayerCharacter>()&&!CombatTargets.IsEnemy(hit.collider.GetComponentInParent<IDamageReceiver>() as Component)){blocked=true;break;}
                if(!blocked)result.Add(target);
            }
            return result;
        }
    }
}

