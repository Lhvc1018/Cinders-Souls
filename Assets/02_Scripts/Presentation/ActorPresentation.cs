using System;
using System.Linq;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
namespace Codice.Presentation
{
    // Animation observes combat; animation events never apply damage or change cooldowns.
    [DefaultExecutionOrder(100)]
    public sealed class ActorPresentation : MonoBehaviour
    {
        public Animator Body {get;private set;}
        public Animator Arms {get;private set;}
        public Transform WeaponSocket {get;private set;}
        public string CurrentState {get;private set;}="Idle";
        /* Duration of the last timed action and time left on it (sent to coop guests so they replay it at the right speed). */
        public float ActionDuration {get;private set;}
        public float FreezeUntil;
        public float ActionLeft=>Mathf.Max(0,lockedUntil-Time.time);
        public bool LaboratoryPreviewActive {get;set;}
        PlayerCharacter player;BasicEnemy enemy;BossCharacter boss;NavMeshAgent agent;
        float remoteSpeed;
        static readonly string[] LocomotionStates={"Idle","Walk","Movement","Run","HeavyWalk","HeavyRun","Patrol","Chase","Ready"};
        /// Animation states worth mirroring from the host onto a guest's own character (not locomotion or idles).
        public static bool IsActionState(string s)=>!string.IsNullOrEmpty(s)&&!s.EndsWith("Idle")&&System.Array.IndexOf(LocomotionStates,s)<0&&s!="JumpRise"&&s!="Fall"&&s!="Land";
        GameObject armsRoot;int selectedClass=-1;float lockedUntil,oldHealth,stepDistance;Vector3 previous;
        bool dead,weaponsCalibrated;WeaponKind previousWeapon=(WeaponKind)(-1);
        GameObject nockedArrow,offhand,heldStar; WeaponKind offhandKind; Quaternion offhandRest; int offhandTier=-1; float throwUntil,throwRelease; bool dualWasVisible; int katanaSlash; float bowShotAt=-100,bowCycle=.8f,musketShotAt=-100;
        public static ActorPresentation Install(GameObject actor)
        {
            if(!PresentationLibrary.Load())return null;
            var p=actor.GetComponent<ActorPresentation>();return p?p:actor.AddComponent<ActorPresentation>();
        }
        void Start()
        {
            player=GetComponent<PlayerCharacter>();enemy=GetComponent<BasicEnemy>();boss=GetComponent<BossCharacter>();agent=GetComponent<NavMeshAgent>();previous=transform.position;
            if(player){oldHealth=player.State.Health;if(Codice.Multiplayer.LanAvatar.IsRemote(player))SetupRemote();else SetupPlayer();}
            else if(enemy){Body=enemy.animator;if(!enemy.useNativeAnimator)Configure(Body,PresentationLibrary.Load().medium);else if(Body){Body.applyRootMotion=false;Body.cullingMode=AnimatorCullingMode.AlwaysAnimate;}oldHealth=enemy.Health;}
            else if(boss){Body=boss.visual.GetComponentInChildren<Animator>();if(!Body)Body=boss.visual.GetChild(0).gameObject.AddComponent<Animator>();if(!boss.useNativeAnimator)Configure(Body,boss.kind==BossKind.GoblinKing?PresentationLibrary.Load().goblin:PresentationLibrary.Load().demon);else {Body.applyRootMotion=false;Body.cullingMode=AnimatorCullingMode.AlwaysAnimate;}oldHealth=boss.Health;AttachBossWeapons();}
            // Extracted creature clips keep events for scripts that are not part of this project.
            if(Body&&((enemy&&enemy.useNativeAnimator)||(boss&&boss.useNativeAnimator)))Body.fireEvents=false;
        }
        static void Configure(Animator a,RuntimeAnimatorController controller){if(!a)return;a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.speed=1;if(a.isActiveAndEnabled&&controller){a.Rebind();a.SetFloat("ActionSpeed",1);}}
        static Transform Bone(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase));
        public void RefreshLaboratoryClass(){LaboratoryPreviewActive=false;selectedClass=-1;lockedUntil=0;dead=false;CurrentState="";katanaSlash=0;SetupPlayer();oldHealth=player.State.Health;}
        /// A guest's avatar on the host: the same current humanoid class model the guest sees on its own screen.
        /// It used to animate player.classVisuals (the old chibi beta models), so guests looked tiny and outdated.
        void SetupRemote()
        {
            foreach (Transform child in player.transform) if (child.name == "Humanoid pilgrim") Destroy(child.gameObject);   // cloned from the host's own model
            if (player.classVisuals != null) foreach (var v in player.classVisuals) if (v) v.SetActive(false);
            var human = Resources.Load<GameObject>("Presentation/Classes/" + player.State.Class) ?? Resources.Load<GameObject>("Presentation/Human/HumanPlayer");
            if (!human) { Body = player.classVisuals[(int)player.State.Class].GetComponentInChildren<Animator>(); Configure(Body, ClassAnimationControllers.Load(player.State.Class)); return; }
            var visual = Instantiate(human, player.transform, false); visual.name = "Humanoid pilgrim";
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>().Take(1).ToArray(); if (renderers.Length > 0) { var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds); visual.transform.localScale *= 1.8f / Mathf.Max(.1f, bounds.size.y); }
            Body = visual.GetComponentInChildren<Animator>(true); Configure(Body, ClassAnimationControllers.Load(player.State.Class));
            (GetComponent<HumanGroundClearance>() ?? gameObject.AddComponent<HumanGroundClearance>()).Initialize(visual.transform, Body);
            selectedClass = (int)player.State.Class;
        }
        void SetupPlayer()
        {
            if(player.classVisuals==null||player.classVisuals.Length<3||!player.view)return;
            int index=(int)player.State.Class;if(index==selectedClass)return;selectedClass=index;
            if(Body&&Body.gameObject.name=="Humanoid pilgrim"){Body.gameObject.SetActive(false);Destroy(Body.gameObject);Body=null;}
            if(armsRoot){DetachWeapons();Destroy(armsRoot);}
            var combatModels=player.GetComponent<PlayerCombat>();
            if(index==(int)PlayerClass.Shinobi&&combatModels.weaponModels[0].name!="Katana"){var blade=Resources.Load<GameObject>("Katana");if(blade){Destroy(combatModels.weaponModels[0]);combatModels.weaponModels[0]=Instantiate(blade);combatModels.weaponModels[0].name="Katana";}}
            var visual=player.classVisuals[index<player.classVisuals.Length?index:0];if(!visual)return;
            var firstPersonVisual=visual;
            var human=Resources.Load<GameObject>("Presentation/Classes/"+player.State.Class)??Resources.Load<GameObject>("Presentation/Human/HumanPlayer");
            if(human)
            {
                visual.SetActive(false);visual=Instantiate(human,player.transform,false);visual.name="Humanoid pilgrim";
                var renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>().Take(1).ToArray();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);visual.transform.localScale*=1.8f/Mathf.Max(.1f,bounds.size.y);}
            }
            bool newHuman=visual.name=="Humanoid pilgrim";
            var playerController=newHuman?ClassAnimationControllers.Load(player.State.Class):PresentationLibrary.Load().medium;
            Body=visual.GetComponentInChildren<Animator>(true);Configure(Body,playerController);
            if(newHuman)(GetComponent<HumanGroundClearance>()??gameObject.AddComponent<HumanGroundClearance>()).Initialize(visual.transform,Body);
            armsRoot=Instantiate(firstPersonVisual,player.view.transform);armsRoot.name="First person arms — "+player.State.Class;armsRoot.SetActive(true);
            armsRoot.transform.localPosition=new Vector3(.08f,-.40f,.65f);armsRoot.transform.localRotation=Quaternion.identity;
            armsRoot.transform.localScale=Vector3.one*.55f;
            if(newHuman){foreach(var r in visual.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;var mode=GetComponent<HumanPreviewCamera>()??gameObject.AddComponent<HumanPreviewCamera>();mode.RefreshBody();if(!GetComponent<ThirdPersonPresentation>())gameObject.AddComponent<ThirdPersonPresentation>();Body.gameObject.AddComponent<HumanoidSupportHand>().Initialize(player);}
            foreach(var renderer in armsRoot.GetComponentsInChildren<Renderer>(true)){
                renderer.enabled=renderer.name.IndexOf("Arm",StringComparison.OrdinalIgnoreCase)>=0;
                renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            Arms=armsRoot.GetComponentInChildren<Animator>(true);Configure(Arms,PresentationLibrary.Load().medium);
            WeaponSocket=Bone(armsRoot.transform,"handslot.r");previousWeapon=(WeaponKind)(-1);AttachPlayerWeapons();
        }
        void DetachWeapons(){var combat=player.GetComponent<PlayerCombat>();if(combat&&combat.weaponModels!=null)foreach(var model in combat.weaponModels)if(model)model.transform.SetParent(player.view.transform,true);}
        void AttachPlayerWeapons()
        {
            var combat=player.GetComponent<PlayerCombat>();if(!WeaponSocket||!combat||combat.weaponModels==null)return;
            for(int i=0;i<combat.weaponModels.Length;i++){
                var model=combat.weaponModels[i];if(!model)continue;
                var hand=i==(int)WeaponKind.Bow||i==(int)WeaponKind.Shield?Bone(armsRoot.transform,"handslot.l"):WeaponSocket;
                model.transform.SetParent(hand,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
                // Prefab wrappers were centred for a floating camera prop. Restore the model's grip origin.
                if(!weaponsCalibrated&&i!=(int)WeaponKind.Bow&&model.transform.childCount>0){var mesh=model.transform.GetChild(0);mesh.localPosition=i==(int)WeaponKind.Shield?Vector3.zero:mesh.localPosition-Vector3.up*.1f;}
                // Dagger1_3_5 has its guard near the origin and the grip along +Y.
                // Centre that grip in the palm, independently of the generic bottom alignment.
                if(i==(int)WeaponKind.Dagger&&model.transform.childCount>0){var mesh=model.transform.GetChild(0);mesh.localPosition=-Vector3.up*(.08f*mesh.localScale.y);}
                model.transform.localScale=Vector3.one/Mathf.Max(.01f,hand.lossyScale.x);
                if(i==(int)WeaponKind.Musket)model.transform.localRotation=Quaternion.Euler(90,0,0);
                // Dagger1_3_5 points down its local Y axis, opposite to the stab clip.
                if(i==(int)WeaponKind.Dagger)model.transform.localRotation=Quaternion.Euler(0,0,180);
                // Blink blades face local X, while the grip animation expects local Z.
                if(i==(int)WeaponKind.Axe||i==(int)WeaponKind.Scythe)model.transform.localRotation=Quaternion.Euler(0,-90,0);
            }
            weaponsCalibrated=true;
        }
        void LateUpdate()
        {
            if(!player||!player.view||!WeaponSocket||!armsRoot||Codice.Multiplayer.LanAvatar.IsRemote(player))return;
            var combat=player.GetComponent<PlayerCombat>();
            if(!combat||combat.weaponModels==null||combat.weaponModels.Length<3)return;
            var bow=combat.weaponModels[(int)WeaponKind.Bow];
            bool throwing=player.State.ActiveWeapon?.shuriken==true;
            bool drawing=player.State.IsAlive&&player.State.ActiveWeapon?.kind==WeaponKind.Bow&&!throwing;
            if(throwing&&bow)bow.SetActive(false);
            if(!heldStar){var star=Resources.Load<GameObject>("Shuriken");if(star){heldStar=Instantiate(star,WeaponSocket);heldStar.transform.localPosition=Vector3.zero;heldStar.transform.localScale=Vector3.one/Mathf.Max(.01f,WeaponSocket.lossyScale.x);}}
            if(heldStar)heldStar.SetActive(!(player.GetComponent<HumanPreviewCamera>()?.ThirdPerson??false)&&throwing&&player.State.IsAlive&&(Time.time<throwRelease||Time.time>=throwUntil));
            bool dual=player.State.DualWield&&!combat.Blocking&&player.State.IsAlive;
            if(dual&&(!offhand||offhandKind!=player.State.ActiveWeapon.kind||offhandTier!=player.State.GetWeapon(1-player.State.ActiveSlot).tier)){if(offhand)Destroy(offhand);offhandKind=player.State.ActiveWeapon.kind;offhand=Instantiate(combat.weaponModels[(int)offhandKind],Bone(armsRoot.transform,"handslot.l"));offhandTier=player.State.GetWeapon(1-player.State.ActiveSlot).tier;WeaponVisualCatalog.Apply(offhand,player.State.GetWeapon(1-player.State.ActiveSlot));offhand.name="Dual offhand";offhand.transform.localPosition=Vector3.zero;offhandRest=combat.weaponModels[(int)offhandKind].transform.localRotation;offhand.transform.localRotation=offhandRest;}
            if(offhand){offhand.SetActive(dual);if(dual)offhand.transform.localRotation=offhandRest;}
            var view=player.view.transform;
            if(dual||dualWasVisible){
                var primary=combat.weaponModels[(int)player.State.ActiveWeapon.kind];
                if(primary)primary.transform.localRotation=Quaternion.identity;
                if(offhand)offhand.transform.localRotation=Quaternion.identity;
                if(dual&&!combat.IsSwinging&&Time.time>=lockedUntil){
                    // Both grips stay on the animated hands; blades rest apart and up.
                    if(primary)primary.transform.rotation=Quaternion.LookRotation(view.forward,(view.up+view.right*.25f).normalized);
                    if(offhand)offhand.transform.rotation=Quaternion.LookRotation(view.forward,(view.up-view.right*.25f).normalized);
                }
            }
            dualWasVisible=dual;
            // The catalog standardizes barrel direction to +Y. Map it to camera +Z,
            // independently of the imported hand bone's rotation.
            if(player.State.ActiveWeapon?.kind==WeaponKind.Musket){
                var musket=combat.weaponModels[(int)WeaponKind.Musket];
                if(musket){float recoil=Mathf.Exp(-Mathf.Max(0,Time.time-musketShotAt)*16);
                    musket.transform.position=view.TransformPoint(new Vector3(.23f,-.24f,.48f-recoil*.10f));
                    musket.transform.rotation=view.rotation*Quaternion.Euler(90-recoil*5,0,0);
                }
            }
            float elapsed=Time.time-bowShotAt;
            float release=Mathf.Clamp01(elapsed/Mathf.Max(.1f,bowCycle));
            float kick=elapsed<bowCycle?Mathf.Sin(release*Mathf.PI)*.065f:0;
            if(bow&&drawing)
            {
                // A short recoil and return accompany the release, instead of freezing
                // the prop in exactly the same camera pose throughout the animation.
                float sway=Mathf.Sin(Time.time*2)*.004f;
                bow.transform.position=view.TransformPoint(new Vector3(-.28f+sway,-.22f-kick*.35f,.7f-kick));
                bow.transform.rotation=view.rotation*Quaternion.Euler(kick*80,0,-kick*100);
            }
            if(!nockedArrow)
            {
                var prefab=Resources.Load<GameObject>("PlayerArrow");
                if(prefab){nockedArrow=Instantiate(prefab,view);nockedArrow.name="Nocked arrow";}
            }
            if(nockedArrow){
                bool visible=!(player.GetComponent<HumanPreviewCamera>()?.ThirdPerson??false)&&drawing&&player.State.ActiveWeapon.ammunition>0&&elapsed>=bowCycle*.55f;
                nockedArrow.SetActive(visible);
                if(visible){float draw=elapsed<bowCycle?Mathf.SmoothStep(0,1,(release-.55f)/.45f):1;
                    nockedArrow.transform.position=bow.transform.position+view.right*.055f+view.up*.035f+view.forward*(.18f-.12f*draw);
                    nockedArrow.transform.rotation=view.rotation;
                }
            }
        }
        void AttachBossWeapons()
        {
            var right=Bone(Body.transform,"hand_r");var left=Bone(Body.transform,"hand_l");
            foreach(var t in Body.GetComponentsInChildren<Transform>(true))if(t.name=="Puglin_Stick"||t.name=="Imp_Mace")t.gameObject.SetActive(false);
            Attach(boss.meleeWeapon,right);Attach(boss.bowWeapon,left);
            // Native Muck polearms (Chief spear) reach the floor in Spin/Melee; keep them above it.
            foreach(var t in Body.GetComponentsInChildren<Transform>(true))if(t.name.Contains("Spear")&&t.GetComponent<MeshFilter>())WeaponGroundGuard.Add(t.gameObject,boss.transform);
            if(boss.meleeWeapon)WeaponGroundGuard.Add(boss.meleeWeapon,boss.transform);
        }
        static void Attach(GameObject item,Transform hand){if(!item||!hand)return;var scale=item.transform.lossyScale;item.transform.SetParent(hand,false);item.transform.localPosition=Vector3.zero;item.transform.localRotation=Quaternion.identity;item.transform.localScale=scale/Mathf.Max(.01f,hand.lossyScale.x);}
        void Update()
        {
            if(dead&&player&&player.State.IsAlive){dead=false;oldHealth=player.State.Health;lockedUntil=0;CurrentState="";if(armsRoot)armsRoot.SetActive(true);}
            if(dead)return;
            if(player&&!Codice.Multiplayer.LanAvatar.IsRemote(player))SetupPlayer();
            bool alive=player?player.State.IsAlive:enemy?enemy.IsAlive:boss&&boss.IsAlive;
            float health=player?player.State.Health:enemy?enemy.Health:boss?boss.Health:oldHealth;
            if(!alive){Die();return;}
            bool paused=Time.timeScale==0||(enemy&&enemy.Paused)||(boss&&boss.Paused)||(player&&player.ControlsBlocked&&!LaboratoryPreviewActive&&!(player.GetComponent<Codice.Mechanics.EstusFlask>()?.Drinking??false));
            float pace=paused?0:Time.unscaledTime<FreezeUntil?.04f:1;if(Body)Body.speed=pace;if(Arms)Arms.speed=pace;   /* FreezeUntil: per-character hit stop for coop (time scale can't stop there) */
            if(player&&LaboratoryPreviewActive)return;
            if(health<oldHealth&&!(player&&player.GetComponent<PlayerCombat>().IsSwinging&&!player.IsStaggered)&&!(boss&&boss.State==BossState.Windup))Hit();oldHealth=health;
            float distance=Vector3.ProjectOnPlane(transform.position-previous,Vector3.up).magnitude;previous=transform.position;
            if(paused)return;
            float speed=Time.deltaTime>0?distance/Time.deltaTime:0;
            // Remote players move in network steps (many frames of 0, then a jump): smooth the speed or they read as idle.
            if(player&&Codice.Multiplayer.LanAvatar.IsRemote(player)){if(distance>3)speed=remoteSpeed;remoteSpeed=Mathf.Lerp(remoteSpeed,speed,1-Mathf.Exp(-6*Time.deltaTime));speed=remoteSpeed;}
            bool creature=(enemy&&enemy.useNativeAnimator)||(boss&&boss.useNativeAnimator);
            if(creature&&Body&&Body.parameters.Any(p=>p.name=="LocomotionSpeed"))Body.SetFloat("LocomotionSpeed",Mathf.Clamp(speed/(enemy?(enemy.lungingHound?(enemy.IsPatrolling?.40f:2.2f):Mathf.Max(.1f,enemy.locomotionMetersPerSecond)):(boss&&boss.State==BossState.Charging?6.5f:2.8f)),.55f,2.2f));
            if(creature&&((enemy&&enemy.IsStunned)||(boss&&boss.State==BossState.Stunned))){lockedUntil=0;Play("Stun");return;}
            if(player&&distance<2&&speed>.4f){stepDistance+=distance;   /* dust only under players: dozens of enemies hid the view */if(stepDistance>1.7f){stepDistance=0;PresentationVfx.Footstep(transform.position);}}
            if(player&&player.State.ActiveWeapon!=null&&previousWeapon!=player.State.ActiveWeapon.kind){previousWeapon=player.State.ActiveWeapon.kind;if(!player.GetComponent<PlayerCombat>().IsSwinging){lockedUntil=0;CurrentState="";}}
            if(player&&player.GetComponent<ShinobiGuard>()?.Guarding==true){if(Time.time>=lockedUntil)Play("ShinobiGuard");return;}
            if(player&&(player.GetComponent<PlayerCombat>().Blocking||player.GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShieldActive==true)){if(Time.time>=lockedUntil)Play("BlockIdle");return;}
            if(player && Body && Body.HasState(0,Animator.StringToHash("JumpRise")))
            {
                if(player.IsDashing){ if(CurrentState!=player.DodgeAnimation)Play(player.DodgeAnimation,player.ActiveDodgeDuration,true); return; }
                if(!player.Grounded && !player.GetComponent<PlayerCombat>().IsSwinging){Play(player.VerticalSpeed>0?"JumpRise":"Fall");return;}
                if(CurrentState=="JumpRise"||CurrentState=="Fall"){lockedUntil=0;Play("Land",.2f,true);return;}
            }
            if(Time.time<lockedUntil)return;
            string state=boss&&boss.State==BossState.Charging?"Run":speed>.2f?(player?(HeavyPlayerWeapon()?(player.Sprinting?"HeavyRun":"HeavyWalk"):(player.Sprinting?"Movement":"Walk")):enemy&&enemy.IsPatrolling?"Patrol":"Chase"):IdleState();
            if(player&&player.IsDashing){if(CurrentState!=player.DodgeAnimation)Play(player.DodgeAnimation,player.ActiveDodgeDuration,true);return;}
            Play(state);
        }
        bool HeavyPlayerWeapon(){var w=player?player.State.ActiveWeapon:null;return w!=null&&(w.katana||w.kind==WeaponKind.Hammer||w.kind==WeaponKind.Axe||w.kind==WeaponKind.Scythe);}
        string IdleState(){if(player){if(player.GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShieldActive==true)return "BlockIdle";if(player.State.DualWield)return "DualIdle";if(player.State.ActiveWeapon?.shuriken==true)return "Ready";if(player.State.ActiveWeapon?.kind==WeaponKind.Staff)return "StaffIdle";var k=player.State.ActiveWeapon?.kind;if(k==WeaponKind.Bow)return "BowIdle";if(k==WeaponKind.Musket)return "RifleIdle";if(k==WeaponKind.Shield)return "BlockIdle";if(HeavyPlayerWeapon())return "HeavyReady";return "Ready";}return enemy&&enemy.kind==EnemyKind.Crossbowman?"RifleIdle":enemy&&enemy.kind==EnemyKind.Heavy?"BlockIdle":"Idle";}
        string PlayerRole(){
            var w=player.State.ActiveWeapon;if(w==null)return "Sword";if(player.State.DualWield)return "Dual";if(w.katana)return "Katana";
            switch(w.kind){case WeaponKind.Polearm:return "Spear";case WeaponKind.Scythe:return "Katana";default:return w.kind.ToString();}
        }
        string PlayerState(string requested){
            if(!player||!Body||Body.name!="Humanoid pilgrim")return requested;
            if(player.State.Class==PlayerClass.Berserker)return requested;
            string role=PlayerRole(),key=requested;
            if(requested=="Ready"||requested=="HeavyReady"||requested=="BowIdle"||requested=="StaffIdle"||requested=="RifleIdle"||requested=="DualIdle")key=role+"Idle";
            if(requested=="Hit")key=role+"Hit";
            if(requested=="Walk"||requested=="HeavyWalk"||requested=="Movement"||requested=="HeavyRun"){
                key=player.Sprinting?"Movement":"Walk";
            }
            if(player.State.ActiveWeapon?.shuriken==true && (requested=="Ready"||requested=="BowIdle"))key="ThrowIdle";
            return Body.HasState(0,Animator.StringToHash(key))?key:requested;
        }
        public void Play(string state,float duration=0,bool restart=false)
        {
            if(dead&&state!="Death")return;
            state=PlayerState(state);
            // Full-body actions own every bone from the first evaluated frame.
            if(player&&Body&&duration>0)for(int layer=1;layer<Body.layerCount;layer++)Body.SetLayerWeight(layer,0);
            if(duration>0){SetActionSpeed(Body,state,duration);SetActionSpeed(Arms,state,duration);}
            if(CurrentState!=state||restart){CurrentState=state;float blend=state=="Dash"?.035f:state.StartsWith("Shinobi",StringComparison.Ordinal)?.025f:.08f;if(Body&&Body.isActiveAndEnabled&&Body.runtimeAnimatorController){
                    string clipState=state;
                    if(!Body.HasState(0,Animator.StringToHash(clipState))){
                        if(state=="Patrol"||state=="Chase")clipState=Body.HasState(0,Animator.StringToHash("Chase"))?"Chase":"Run";
                        else if(state.EndsWith("Idle",StringComparison.Ordinal)||state=="Ready"||state=="HeavyReady")clipState="Idle";
                    }
                    if(Body.HasState(0,Animator.StringToHash(clipState)))Body.CrossFadeInFixedTime(clipState,blend,0);
                }if(Arms&&Arms.isActiveAndEnabled&&Arms.HasState(0,Animator.StringToHash(state)))Arms.CrossFadeInFixedTime(state,Mathf.Min(blend,.06f),0);}
            if(duration>0){lockedUntil=Time.time+duration;ActionDuration=duration;}
        }
        static void SetActionSpeed(Animator a,string state,float duration){if(!a||!a.isActiveAndEnabled||!a.isInitialized||!a.runtimeAnimatorController)return;var clip=a.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name==state) ?? a.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.EndsWith(state,StringComparison.Ordinal));a.SetFloat("ActionSpeed",clip?clip.length/Mathf.Max(.1f,duration):1);}
        public float MinimumAttackDuration(WeaponInstance weapon,int step){
            if(!Body||Body.name!="Humanoid pilgrim"||!Body.runtimeAnimatorController)return 0;
            string key=weapon.shuriken?"ThrowShuriken":weapon.katana?(step%2==1?"KatanaSlashA":"KatanaSlashB"):weapon.kind==WeaponKind.Sword?"SwordCombo"+step:weapon.kind==WeaponKind.Bow?"ShootBow":weapon.kind==WeaponKind.Polearm?"Thrust":weapon.kind==WeaponKind.Dagger?"DaggerAttack":weapon.kind==WeaponKind.Axe?"AxeAttack":weapon.kind==WeaponKind.Hammer||weapon.kind==WeaponKind.Scythe?"HeavyAttack":"";
            if(player.GetComponent<PlayerCombat>().HasKnightShield&&step==2&&PlayerCombat.SwordCombo(weapon))key="ShieldStrike";
            if(player.State.DualWield)key=step%2==0?"DualStab":"DualSlice";
            if(weapon.katana&&!player.State.DualWield&&step==3&&TryCut("KatanaFinisher",out var fin,out var finClip))return finClip.length/fin.pace*fin.cut;
            if(player.State.DualWield&&TryCut(DualState(step),out var dual,out var dualClip))return dualClip.length/dual.pace*dual.cut;
            if(weapon.katana&&!player.State.DualWield){var motion=StateClip(key);if(motion)return motion.length/KatanaPace(motion.name);}
            var clip=Body.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name==key||c.name=="Lords_"+key||c.name=="Thymesia_"+key);return clip?clip.length/(clip.name.StartsWith("Thymesia_")?1.65f:1.25f):0;
        }
        static readonly System.Collections.Generic.Dictionary<RuntimeAnimatorController,System.Collections.Generic.Dictionary<string,AnimationClip>> stateClips=new System.Collections.Generic.Dictionary<RuntimeAnimatorController,System.Collections.Generic.Dictionary<string,AnimationClip>>();
        // Thymesia clips strike early and carry a long recovery tail. Play them at `pace`, release the
        // action after `cut` of the clip, and resolve damage inside [hit0,hit1] (clip-normalized, measured
        // from blade-tip speed on the dressed avatar). Windows below are converted to the cut duration.
        public struct ClipCut{public float pace,cut,hit0,hit1;public ClipCut(float p,float c,float a,float b){pace=p;cut=c;hit0=a;hit1=b;}}
        public static readonly System.Collections.Generic.Dictionary<string,ClipCut> Cuts=new System.Collections.Generic.Dictionary<string,ClipCut>{
            {"Thymesia_KatanaSlashA",new ClipCut(1.2f,.85f,.11f,.30f)},   // Shinobi finisher: clean katana cut, heavier pace
            {"AS_DWLT_LightAttack01",new ClipCut(2.9f,.62f,.30f,.42f)},   // Assasin 1 (Lords dual wield)
            {"AS_DWLT_LightAttack02",new ClipCut(2.6f,.50f,.14f,.25f)},   // Assasin 2 (Lords dual wield)
            {"Thymesia_DualSlice",new ClipCut(1.7f,.70f,.20f,.38f)},     // Assasin finisher
        };
        public bool TryCut(string state,out ClipCut cut,out AnimationClip clip){clip=StateClip(state);cut=default;return clip&&Cuts.TryGetValue(clip.name,out cut);}
        // Shinobi cuts play faster than their source so both steps last ~0.75 s.
        static float KatanaPace(string clip)=>clip.StartsWith("Thymesia_",StringComparison.Ordinal)?1.65f:clip=="Shinobi_KatanaSlashA"?2.1f:1.6f;
        AnimationClip StateClip(string state)
        {
            if(!Body||!Body.runtimeAnimatorController)return null;
            var controller=Body.runtimeAnimatorController;
            if(!stateClips.TryGetValue(controller,out var map)){
                map=new System.Collections.Generic.Dictionary<string,AnimationClip>();
                var table=Resources.Load<TextAsset>("Presentation/Human/StateClips");
                if(table)foreach(var line in table.text.Split('\n')){var f=line.Trim().Split('|');if(f.Length!=3||f[0]!=controller.name)continue;var clip=controller.animationClips.FirstOrDefault(c=>c.name==f[2]);if(clip)map[f[1]]=clip;}
                stateClips[controller]=map;
            }
            if(map.TryGetValue(state,out var mapped))return mapped;
            // Fallback: the clip currently evaluated by layer 0 (next clip while crossfading).
            var info=Body.IsInTransition(0)?Body.GetNextAnimatorClipInfo(0):Body.GetCurrentAnimatorClipInfo(0);
            return info.Length>0?info[0].clip:null;
        }
        public static string DualState(int step)=>step==1?"DualSlice":step==2?"DualStab":"DualFinisher";
        public bool TryAuthoredContactWindow(out float start,out float end)
        {
            start=0;end=0;
            if(!player||!Body||!Body.runtimeAnimatorController)return false;
            string key=CurrentState;
            var playing=StateClip(key);
            if(playing&&Cuts.TryGetValue(playing.name,out var c)){start=c.hit0/c.cut;end=Mathf.Min(1,c.hit1/c.cut);return true;}
            // Measured on the dressed Shinobi: blade-tip speed above 45% of its peak, padded by ~0.06.
            if(playing&&playing.name=="Shinobi_KatanaSlashA"){start=.28f;end=.46f;return true;}
            if(playing&&playing.name=="Shinobi_KatanaSlashB"){start=.40f;end=.62f;return true;}
            if(playing&&playing.name=="Thymesia_KatanaSlashA"&&key.StartsWith("Katana",StringComparison.Ordinal)){start=.10f;end=.32f;return true;}
            if(playing&&playing.name=="Thymesia_KatanaSlashB"&&key.StartsWith("Katana",StringComparison.Ordinal)){start=.12f;end=.32f;return true;}
            if(Body.runtimeAnimatorController.animationClips.Any(c=>c.name=="Thymesia_"+key)){
                switch(key){
                    case "KatanaSlashA":case "SwordCombo1":start=.18f;end=.48f;return true;
                    case "KatanaSlashB":case "SwordCombo2":start=.22f;end=.54f;return true;
                    case "SwordCombo3":start=.22f;end=.58f;return true;
                    case "DualSlice":case "DualStab":start=.22f;end=.65f;return true;
                    case "HeavyAttack":start=.34f;end=.62f;return true;
                    case "AxeAttack":case "Thrust":case "DaggerAttack":start=.25f;end=.58f;return true;
                }
            }
            if(!Body.runtimeAnimatorController.animationClips.Any(c=>c.name=="Lords_"+key))return false;
            switch(key){
                case "SwordCombo1":start=.46f;end=.65f;return true;
                case "SwordCombo2":start=.28f;end=.52f;return true;
                case "SwordCombo3":start=.43f;end=.65f;return true;
                case "Thrust":start=.25f;end=.48f;return true;
                case "KatanaSlashA":case "KatanaSlashB":start=.32f;end=.56f;return true;
                case "HeavyAttack":start=.47f;end=.69f;return true;
                case "DualSlice":start=.35f;end=.68f;return true;
                case "DualStab":start=.32f;end=.65f;return true;
                default:return false;
            }
        }
        public void Attack(bool ranged,float duration=.5f)
        {
            var weapon=GetComponent<PlayerCharacter>()?.State.ActiveWeapon;
            var cue=weapon!=null?(weapon.kind==WeaponKind.Bow?SoundCue.Bow:weapon.kind==WeaponKind.Musket?SoundCue.Gun:weapon.kind==WeaponKind.Staff?SoundCue.Magic:weapon.kind==WeaponKind.Hammer||weapon.kind==WeaponKind.Axe?SoundCue.Heavy:SoundCue.Swing):GetComponent<BossCharacter>()?SoundCue.Boss:ranged?SoundCue.Bow:SoundCue.Swing;
            if(weapon!=null){if(weapon.kind!=WeaponKind.Staff)GameAudio.Play(cue);}else if(!GetComponent<BossCharacter>())GameAudio.PlayAt(cue,transform.position);
            if(player&&weapon!=null){if(weapon.kind==WeaponKind.Bow&&!weapon.shuriken){bowShotAt=Time.time;bowCycle=Mathf.Max(.2f,duration);}if(weapon.kind==WeaponKind.Musket)musketShotAt=Time.time;}
            string state=ranged?"Shoot":"Melee";
            if(boss&&boss.useNativeAnimator&&boss.CurrentAttack==BossAttack.Slam&&Body&&Body.HasState(0,Animator.StringToHash("Slam")))state="Slam";
            if(player){var k=player.State.ActiveWeapon?.kind;if(k==WeaponKind.Axe)state="AxeAttack";else if(k==WeaponKind.Bow)state="ShootBow";else if(k==WeaponKind.Staff)state="ShootMagic";else if(k==WeaponKind.Hammer||k==WeaponKind.Axe||k==WeaponKind.Scythe)state="HeavyAttack";else if(k==WeaponKind.Polearm||k==WeaponKind.Dagger)state="Thrust";}
            if(player&&player.State.ActiveWeapon?.shuriken==true){state="ThrowShuriken";throwRelease=Time.time+duration*.36f;throwUntil=Time.time+duration*.85f;}
            if(player&&player.State.ActiveWeapon?.kind==WeaponKind.Staff)state="StaffCast";
            if(player&&player.State.ActiveWeapon?.kind==WeaponKind.Dagger&&Body&&Body.HasState(0,Animator.StringToHash("DaggerAttack")))state="DaggerAttack";
            if(player&&player.State.ActiveWeapon?.katana==true){int step=player.GetComponent<PlayerCombat>().Combo;state=step==3&&Body&&Body.HasState(0,Animator.StringToHash("KatanaFinisher"))?"KatanaFinisher":step%2==1?"KatanaSlashA":"KatanaSlashB";}
            if(player&&player.State.DualWield){int step=player.GetComponent<PlayerCombat>().Combo;state=DualState(step);if(!Body||!Body.HasState(0,Animator.StringToHash(state)))state=step%2==0?"DualStab":"DualSlice";}
            if(enemy&&!enemy.useNativeAnimator&&enemy.kind==EnemyKind.Mage)state="ShootMagic";
            if(enemy&&enemy.kind==EnemyKind.Heavy)state="HeavyAttack";
            if(player)GetComponent<ThirdPersonPresentation>()?.AttackOffhand(state,duration);
            if(player&&player.GetComponent<PlayerCombat>().ShieldStrike){Play("ShieldStrike",duration,true);return;}
            if(player&&weapon!=null&&weapon.kind==WeaponKind.Sword&&!weapon.katana&&!player.State.DualWield){int step=player.GetComponent<PlayerCombat>().Combo;string key="SwordCombo"+Mathf.Clamp(step,1,3);if(Body&&Body.HasState(0,Animator.StringToHash(key))){Play(key,duration,true);return;}}
            if(player&&TryCut(state,out var cut,out var _)){Play(state,Mathf.Max(.15f,duration)/cut.cut,true);lockedUntil=Time.time+Mathf.Max(.15f,duration);return;}
            bool creature=(enemy&&enemy.useNativeAnimator)||(boss&&boss.useNativeAnimator);
            // Authored impact frame is 17/25. Keep the recovery visible after damage resolves.
            Play(state,Mathf.Max(.15f,duration)/(creature?2f/3f:1f),true);
        }
        public void Special(WeaponKind kind,float duration){
            string state=player&&player.State.DualWield?"DualFinisher":kind==WeaponKind.Sword?"SwordCombo3":kind==WeaponKind.Dagger?"DaggerAttack":kind==WeaponKind.Polearm?"Thrust":kind==WeaponKind.Shield?"ShieldStrike":"HeavyAttack";
            if(Body&&!Body.HasState(0,Animator.StringToHash(state)))state=kind==WeaponKind.Sword?"SwordCombo1":"Melee";
            Play(state,duration,true);
        }
        public void Hit(){Play("Hit",.25f,true);}
        public void Die()
        {
            if(dead)return;dead=true;Play("Death",4,true);
            if(!GetComponent<BossCharacter>())GameAudio.PlayAt(SoundCue.Death,transform.position);
            if(player){if(armsRoot)armsRoot.SetActive(false);return;}
            if(!Body)Body=GetComponentInChildren<Animator>(true);
            if(!Body)return;
            // Visual-only corpse lets the existing combat system remove the actor immediately.
            var corpse=Instantiate(Body.gameObject,Body.transform.position,Body.transform.rotation);corpse.name="Animated remains";corpse.transform.localScale=Body.transform.lossyScale;
            foreach(var c in corpse.GetComponentsInChildren<Collider>())Destroy(c);
            var animator=corpse.GetComponent<Animator>();
            // Native Muck clips carry animation events for scripts that do not exist here; the corpse is visual only.
            foreach(var behaviour in corpse.GetComponentsInChildren<MonoBehaviour>())Destroy(behaviour);
            animator.enabled=true;animator.fireEvents=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.speed=1;corpse.SetActive(true);animator.Play("Death",0,0);Destroy(corpse,5f);
        }
    }
}




