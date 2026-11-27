using UnityEngine;
using UnityEngine.InputSystem;
using Codice.Presentation;
using Codice.Mechanics;
namespace Codice.Characters
{
    [DefaultExecutionOrder(-50)]
    public sealed class ShinobiGuard:MonoBehaviour
    {
        public float Posture{get;private set;}=100;
        /// Sekiro-style deflect, timed from the PRESS (a tap counts, holding is not required):
        ///  - perfect  : hit lands <= PerfectWindow after the press -> no HP, no posture lost, big enemy posture, counter.
        ///  - imperfect: hit lands inside DeflectWindow but later  -> no HP, your posture drops fast, small enemy posture.
        ///  - block    : guard held past the window              -> no HP, heavy posture loss.
        ///  - posture broken -> 1.4 s helpless, incoming hits x1.5 ("vendido").
        public const float PerfectWindow=.2f,DeflectWindow=.42f,ParryWindow=DeflectWindow;
        public bool Guarding{get;private set;}
        public bool Perfect=>Time.time-pressAt<=PerfectWindow;
        public bool InDeflect=>Time.time-pressAt<=DeflectWindow;
        public bool Broken=>Time.time<brokenUntil;
        // Class identity: the Shinobi always shows posture (fills toward the centre as it is lost).
        public bool ShowPosture=>player&&player.State.IsAlive&&(Posture<99f||Guarding||Broken);   /* Sekiro-style: only while guarding or damaged */
        float counterUntil;static readonly string[] deflects={"ShinobiDeflectR","ShinobiDeflectL","ShinobiDeflectRUp","ShinobiDeflectLUp"};int deflectIndex;
        // A perfect deflect opens a short counter window: the next katana cut is a riposte.
        public bool ConsumeCounter(){bool ok=Time.time<counterUntil;counterUntil=0;return ok;}
        public bool RecentPerfect=>Time.time<flashUntil;
        float showUntil,flashUntil;
        bool wasHeld; PlayerCharacter player;float perfectUntil=-1,nextParry,brokenUntil,regenAt,pressAt=-10;
        void Awake(){player=GetComponent<PlayerCharacter>();}
        public void SetGuard(bool held)
        {
            bool pressed=held&&!wasHeld;wasHeld=held;
            bool sword=player.State.ActiveWeapon!=null&&(player.State.ActiveWeapon.kind==WeaponKind.Sword||player.State.ActiveWeapon.kind==WeaponKind.Dagger);
            bool allowed=player.State.Class==PlayerClass.Shinobi&&sword&&!player.IsDashing&&GetComponent<PlayerCombat>().CanDefend&&Posture>0&&player.State.IsAlive&&!player.ControlsBlocked&&!player.IsStaggered&&!Broken&&Time.timeScale>0&&!Codice.UI.RunNavigation.Loading&&!(Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.ResultsVisible)&&!GetComponent<PlayerMechanics>().UltimateActive;
            if(pressed&&allowed&&Time.time>=nextParry){pressAt=Time.time;perfectUntil=Time.time+DeflectWindow;nextParry=Time.time+.2f;}   /* short anti-mash, not a lockout */
            Guarding=held&&allowed;
            if(pressed&&Guarding)GetComponent<ActorPresentation>()?.Play("ShinobiGuard",0,true);
        }
        void Update(){if(Codice.Multiplayer.LanSession.Client){SetGuard((Cursor.lockState==CursorLockMode.Locked||GameInput.UsingGamepad)&&GameInput.Held(GameAction.Block));return;}   /* local guard pose; the host resolves the deflect */SetGuard(Codice.Multiplayer.LanAvatar.IsRemote(this)?GetComponent<Codice.Multiplayer.LanAvatar>().block:Cursor.lockState==CursorLockMode.Locked&&Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Block));if(player.State.IsAlive&&!player.ControlsBlocked&&!Guarding&&Time.time>=regenAt)Posture=Mathf.Min(100,Posture+20*Time.deltaTime);}
        internal void SetContactTiming(Codice.Multiplayer.DefenseTiming timing)
        {
            if(Broken||Posture<=0)return;
            Guarding=true;
            pressAt=timing==Codice.Multiplayer.DefenseTiming.Perfect?Time.time:
                timing==Codice.Multiplayer.DefenseTiming.Deflect?Time.time-(PerfectWindow+DeflectWindow)*.5f:-10;
        }
        public float Resolve(DamageInfo hit,bool ownerFacing=false)
        {
            if(hit.amount<=0||hit.secondary)return hit.amount;
            if(Broken)return hit.amount*1.5f;   // posture broken: helpless
            // Facing: toward the attacker (not the attack's forward), wide 110 degree front so side hits still count.
            Vector3 toAttacker=hit.source?Vector3.ProjectOnPlane(hit.source.transform.position-transform.position,Vector3.up):-hit.direction;
            bool front=ownerFacing||toAttacker.sqrMagnitude<.0001f||Vector3.Dot(Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized,toAttacker.normalized)>-.15f;
            bool deflecting=InDeflect&&hit.melee&&front&&Posture>0&&player.State.Class==PlayerClass.Shinobi;
            if(!deflecting&&(!Guarding||!front))return hit.amount;
            showUntil=Time.time+2.5f;
            if(deflecting&&Perfect){perfectUntil=-1;pressAt=-10;nextParry=Time.time+.12f;Posture=Mathf.Min(100,Posture+15);regenAt=Time.time+.35f;flashUntil=Time.time+.45f;var pose=GetComponent<ActorPresentation>();string anim=hit.amount>=30?"ShinobiDeflectLarge":deflects[deflectIndex++%deflects.Length];if(pose&&pose.Body&&!pose.Body.HasState(0,Animator.StringToHash(anim)))anim="ShinobiDeflect";pose?.Play(anim,.34f,true);GameAudio.Play(SoundCue.Parry);counterUntil=Time.time+1.3f;HitStop.Do(.10f);GameInput.Rumble(.45f,.9f,.14f);var blade=GetComponent<HumanPreviewCamera>()?.HeldWeapon;var contact=blade?blade.position+blade.up*.45f:transform.position+Vector3.up+transform.forward*.6f;PresentationVfx.PerfectDeflect(contact,hit.direction);if(hit.source){var enemy=hit.source.GetComponentInParent<IDamageReceiver>() as Component;if(CombatTargets.IsEnemy(enemy)){if(!EnemyPosture.Add(enemy,44))CombatTargets.Stun(enemy,enemy is Codice.Bosses.BossCharacter?.55f:.8f);}}return 0;}
            if(deflecting)
            {
                // Imperfect deflect: no HP lost, but your posture takes the blow quickly.
                pressAt=-10;perfectUntil=-1;nextParry=Time.time+.1f;regenAt=Time.time+.9f;
                Posture=Mathf.Max(0,Posture-Mathf.Max(10,hit.amount*.6f));
                GetComponent<ActorPresentation>()?.Play(deflects[deflectIndex++%deflects.Length],.3f,true);GameAudio.Play(SoundCue.Parry);HitStop.Do(.05f);GameInput.Rumble(.3f,.5f,.1f);
                var blade=GetComponent<HumanPreviewCamera>()?.HeldWeapon;PresentationVfx.Impact(blade?blade.position+blade.up*.45f:transform.position+Vector3.up+transform.forward*.6f,-hit.direction);
                if(hit.source){var enemy=hit.source.GetComponentInParent<IDamageReceiver>() as Component;if(CombatTargets.IsEnemy(enemy))EnemyPosture.Add(enemy,18);}
                if(Posture<=0)BreakPosture();
                return 0;
            }
            // Plain block: no HP, heavy posture cost.
            GetComponent<ActorPresentation>()?.Play("ShinobiGuardHit",.3f,true);if(hit.source){var attacker=hit.source.GetComponentInParent<IDamageReceiver>() as Component;if(CombatTargets.IsEnemy(attacker))EnemyPosture.Add(attacker,6);}
            Posture=Mathf.Max(0,Posture-Mathf.Max(18,hit.amount*1.1f));regenAt=Time.time+1.5f;
            if(Posture<=0)BreakPosture();
            return 0;
        }
        void BreakPosture()
        {
            brokenUntil=Time.time+1.4f;Guarding=false;pressAt=-10;player.ApplyBossKnockdown();
            GameAudio.Play(SoundCue.Heavy);GameInput.Rumble(.8f,1f,.35f);Codice.UI.RunHUD.Instance?.Announce("POSTURA ROTA · estás expuesto");
        }
        /// A bolt met inside the deflect window is sent back (any deflect, perfect or not: bolts are hard to read).
        public bool ConsumePerfectForProjectile()
        {
            if(!InDeflect||Broken)return false;
            perfectUntil=-1;pressAt=-10;nextParry=Time.time+.12f;Posture=Mathf.Min(100,Posture+10);regenAt=Time.time+.35f;flashUntil=Time.time+.45f;counterUntil=Time.time+1.3f;showUntil=Time.time+2.5f;
            GetComponent<ActorPresentation>()?.Play("ShinobiDeflect",.34f,true);return true;
        }
        void OnDisable(){Guarding=false;perfectUntil=-1;}
    }
}

