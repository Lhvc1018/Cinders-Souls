using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Codice.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class PlayerCharacter : MonoBehaviour, IDamageReceiver
    {
        public PlayerClass initialClass;
        public Camera view;
        public GameObject[] classVisuals;
        [Min(.01f)] public float mouseSensitivity = .09f;
        public LayerMask interactionMask = ~0;
        [Min(.1f)] public float jumpHeight = 1.25f;
        public bool Grounded => controller && verticalSpeed <= 0 && ProbeGround();
        [SerializeField] CharacterState state = new CharacterState();
        public CharacterState State => state;
        public float DashCooldown => Mathf.Max(0, nextDash - Time.time);
        public float SecondDashCooldown => Mathf.Max(0, secondDashReady - Time.time);
        public bool IsDashing => Time.time < dashEnds;
        bool controlsBlocked, narrativeBlocked, healingBlocked;
        public void SetHealingControlsBlocked(bool blocked){healingBlocked=blocked;}
        public bool ControlsBlocked { get => controlsBlocked || narrativeBlocked || healingBlocked || Codice.Multiplayer.LanSession.WaitingForFloor; private set => controlsBlocked = value; }
        public void SetNarrativeControlsBlocked(bool blocked)
        { narrativeBlocked = blocked; CaptureCursor(!blocked && state.IsAlive); }
        public event Action<Merchant> ShopOpened;
        public event Action SpecialRequested;
        CharacterController controller;
        float pitch, verticalSpeed, dashEnds, nextDash;
        float secondDashReady;
        Vector3 dashDirection;
        Merchant merchant;
        bool initialized;
        float slowUntil, staggerUntil, movementMultiplier = 1;
        public float MovementMultiplier => Time.time < slowUntil ? movementMultiplier : 1;
        public bool IsStaggered => Time.time < staggerUntil || (GetComponent<ElementalStatus>()?.Frozen??false);
        // Boss crowd control uses deadlines so destroyed puddles cannot leave permanent debuffs.
        public void ApplyBossSlow(float duration) { slowUntil = Time.time + duration; movementMultiplier = .65f; }
        /// Guard broken by an empty stamina bar: short stagger with feedback.
        public void GuardBreak() { staggerUntil = Time.time + .75f; Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Heavy, transform.position); Codice.UI.RunHUD.Instance?.Announce("GUARDIA ROTA · sin estamina"); Codice.Mechanics.GameInput.Rumble(.6f, .9f, .25f); }
        public void ApplyBossKnockdown() { if (!IsDashing && !(GetComponent<Codice.Mechanics.PlayerMechanics>()?.Invulnerable ?? false)) staggerUntil = Time.time + .6f; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if(!GetComponent<Codice.Presentation.BowArrowPresentation>())gameObject.AddComponent<Codice.Presentation.BowArrowPresentation>();
            if(!GetComponent<ClassIdentity>())gameObject.AddComponent<ClassIdentity>();
            if(!GetComponent<PlayerLockOn>())gameObject.AddComponent<PlayerLockOn>();
            controller.minMoveDistance = 0; controller.skinWidth = .035f; controller.stepOffset = .25f;
            if (!GetComponent<Codice.Presentation.PlayerVisibilityLight>()) gameObject.AddComponent<Codice.Presentation.PlayerVisibilityLight>();
            initialClass = GetComponent<Codice.Multiplayer.LanAvatar>()?.playerClass ?? Codice.UI.RunNavigation.SelectedClass;
            state.Initialize(initialClass); initialized = true;
            if(!GetComponent<StaffSpells>())gameObject.AddComponent<StaffSpells>();
            if(!GetComponent<Codice.Mechanics.RunRelics>())gameObject.AddComponent<Codice.Mechanics.RunRelics>();
            if(!GetComponent<RunSpellbook>())gameObject.AddComponent<RunSpellbook>();
            if(!GetComponent<Codice.UI.RunStatusHUD>())gameObject.AddComponent<Codice.UI.RunStatusHUD>();
            ApplyVisual(); if(initialClass==PlayerClass.Shinobi&&!GetComponent<ShinobiGuard>())gameObject.AddComponent<ShinobiGuard>(); state.Died += OnDeath;
            Codice.Presentation.ActorPresentation.Install(gameObject);
            mouseSensitivity = PlayerPrefs.GetFloat("Codice.Sensitivity", mouseSensitivity);
        }
        void Start() { if(!Codice.Multiplayer.LanAvatar.IsRemote(this))CaptureCursor(true); }
        void OnDestroy() { state.Died -= OnDeath; }
        void OnDisable() { if (merchant) merchant.CloseShop(); CaptureCursor(false); }
        void OnApplicationFocus(bool focused) { if (!focused) CaptureCursor(false); }
        public bool SelectClass(PlayerClass selected)
        {
            // Selection is a pre-run operation: never reset health/inventory mid-combat.
            if (initialized && Application.isPlaying) return false;
            initialClass = selected; return true;
        }
        public bool SelectLaboratoryClass(PlayerClass selected)
        {
            bool lab=state.LaboratoryImmortal&&FindAnyObjectByType<Codice.Mechanics.CombatTestRoom>();
            if(!Application.isPlaying||(!lab&&!Codice.Mechanics.JamDebug.Enabled)||!Enum.IsDefined(typeof(PlayerClass),selected))return false;
            bool immortal=state.LaboratoryImmortal;
            GetComponent<PlayerCombat>()?.TryCancelForDodge();
            GetComponent<Codice.Mechanics.EstusFlask>()?.Interrupt();
            GetComponent<Codice.Mechanics.PlayerMechanics>()?.Cancel();
            GetComponent<ShinobiGuard>()?.SetGuard(false);
            initialClass=selected;state.Initialize(selected);state.LaboratoryImmortal=immortal;
            verticalSpeed=dashEnds=nextDash=secondDashReady=slowUntil=staggerUntil=0;
            moveVelocity=Vector3.zero;dashDirection=Vector3.zero;movementMultiplier=1;
            ApplyVisual();
            if(selected==PlayerClass.Shinobi&&!GetComponent<ShinobiGuard>())gameObject.AddComponent<ShinobiGuard>();
            GetComponent<Codice.Presentation.ActorPresentation>()?.RefreshLaboratoryClass();
            Debug.Log("[TestRoom] Class switched: "+selected+"; starter equipment and resources reset; immortality retained");
            return true;
        }
        void ApplyVisual()
        {
            if (classVisuals == null) return;
            if(initialClass==PlayerClass.Mage && classVisuals.Length<4)
            {
                var mage=Resources.Load<GameObject>("MageVisual");
                if(mage){Array.Resize(ref classVisuals,4);classVisuals[3]=Instantiate(mage,transform,false);}
            }
            if(initialClass==PlayerClass.Shinobi&&classVisuals.Length<5){var ninja=Resources.Load<GameObject>("ShinobiVisual");if(ninja){Array.Resize(ref classVisuals,5);classVisuals[4]=Instantiate(ninja,transform,false);}}
            if(initialClass==PlayerClass.Berserker&&classVisuals.Length<6){var source=classVisuals[0];Array.Resize(ref classVisuals,6);classVisuals[5]=Instantiate(source,transform,false);}
            for (int i = 0; i < classVisuals.Length; i++)
                if (classVisuals[i]) classVisuals[i].SetActive(i == (initialClass==PlayerClass.Mage&&classVisuals.Length<4?0:(int)initialClass));
        }
        void Update()
        {
            if(Codice.Multiplayer.LanSession.Client){Codice.Multiplayer.LanSession.Instance.ClientControls(this);return;}
            if(Codice.Multiplayer.LanAvatar.IsRemote(this)){
                var avatar=GetComponent<Codice.Multiplayer.LanAvatar>();
                if(!ControlsBlocked&&state.IsAlive){state.TickPassives(Time.deltaTime);avatar.Motor(this,Time.deltaTime);if(avatar.attack){Codice.Multiplayer.LanSession.ActingFor=avatar.id;try{GetComponent<PlayerCombat>().TryAttack();}finally{Codice.Multiplayer.LanSession.ActingFor=-1;}}}return;
            }
            if(Codice.Multiplayer.LanSession.Active&&Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.Paused)return;
            if (Codice.UI.RunNavigation.Loading) return;
            if (!state.IsAlive || Time.timeScale == 0) return;
            if (!ControlsBlocked && !(Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible))
            { state.TickMana(Time.deltaTime); state.TickAmmunition(Time.deltaTime); state.TickPassives(Time.deltaTime); }
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (keys == null || mouse == null || !view) return;
            if (merchant)
            {
                if (keys.escapeKey.wasPressedThisFrame || Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Interact) || Codice.Mechanics.GameInput.PadPressed(Codice.Mechanics.GameAction.Back)) merchant.CloseShop();
                else if (keys.enterKey.wasPressedThisFrame) merchant.ContinueRound();
                else
                {
                    int offer = keys.digit1Key.wasPressedThisFrame ? 0 : keys.digit2Key.wasPressedThisFrame ? 1 : keys.digit3Key.wasPressedThisFrame ? 2 : keys.digit4Key.wasPressedThisFrame ? 3 : -1;
                    int replace = keys.leftShiftKey.isPressed ? 0 : keys.leftCtrlKey.isPressed ? 1 : keys.leftAltKey.isPressed ? 2 : -1;
                    if (offer >= 0) Debug.Log(merchant.TryPurchase(this, offer, replace) ? "Compra realizada." : "Compra rechazada: revisa oro, salud, stock o ranuras.", this);
                }
                return;
            }
            if (Codice.UI.RunHUD.HandlesEscapeThisFrame) return;
            if (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) return;
            if (narrativeBlocked) return;
            if (keys.escapeKey.wasPressedThisFrame && !Codice.Mechanics.GameInput.UsingGamepad) { CaptureCursor(false); return; }
            if (Cursor.lockState != CursorLockMode.Locked)
            { if (mouse.leftButton.wasPressedThisFrame || Codice.Mechanics.GameInput.UsingGamepad) CaptureCursor(true); return; }
            if (ControlsBlocked) return;
            if (Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Relic1)) GetComponent<Codice.Mechanics.RunRelics>().Activate(0);
            if (Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Relic2)) GetComponent<Codice.Mechanics.RunRelics>().Activate(1);
            var delta = Codice.Mechanics.GameInput.Look(mouseSensitivity);
            var lockOn=GetComponent<PlayerLockOn>();
            if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.LockOn))lockOn.Toggle();
            lockOn.Validate();
            if(lockOn.Target){
                Vector3 to=lockOn.Point-(transform.position+Vector3.up*1.45f);
                var flat=Vector3.ProjectOnPlane(to,Vector3.up);
                if(flat.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),720*Time.deltaTime);
                float targetPitch=-Mathf.Atan2(to.y,flat.magnitude)*Mathf.Rad2Deg;
                pitch=Mathf.LerpAngle(pitch,Mathf.Clamp(targetPitch,-45,60),1-Mathf.Exp(-10*Time.deltaTime));
            }else{transform.Rotate(0,delta.x,0);pitch=Mathf.Clamp(pitch-delta.y,-85,85);}
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 input = Codice.Mechanics.GameInput.Move();
            Vector3 direction = transform.TransformDirection(new Vector3(input.x, 0, input.y));
            if (Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Dodge)) HandleDodgePress(direction);
            if (Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Jump)) jumpQueuedUntil = Time.time + .12f;
            SprintRequested=Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Sprint);
            SimulateMotor(direction, Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Jump), Time.deltaTime);
            if (transform.position.y < -20) state.Damage(state.MaxHealth);
            if (state.ActiveWeapon?.kind == WeaponKind.Staff)
            {
                int spell=keys.digit1Key.wasPressedThisFrame?0:keys.digit2Key.wasPressedThisFrame?1:keys.digit3Key.wasPressedThisFrame?2:keys.digit4Key.wasPressedThisFrame?3:-1;
                if(spell<0&&Codice.Mechanics.GameInput.PadPressed(Codice.Mechanics.GameAction.NextSpell))spell=((int)state.SelectedSpell+1)%4;
                if(spell>=0)state.SelectSpell(spell);
            }
            else if (keys.digit1Key.wasPressedThisFrame) state.SelectWeapon(0);
            else if (keys.digit2Key.wasPressedThisFrame) state.SelectWeapon(1);
            if (Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.SwapWeapon) || Codice.Mechanics.GameInput.WeaponScroll() != 0) state.SelectWeapon(1 - state.ActiveSlot);
            if (Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Interact)) TryInteract();
            if (!ControlsBlocked && Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Special)) SpecialRequested?.Invoke();
        }
        public bool TryDash(Vector3 direction)
        {
            if (!state.IsAlive || ControlsBlocked || IsStaggered || IsDashing || Time.timeScale == 0 || Codice.UI.RunNavigation.Loading) return false;
            bool primaryReady = Time.time >= nextDash;
            if (!primaryReady && (!state.HasPassive(PassiveKind.MercuryBoots) || Time.time < secondDashReady)) return false;
            if(GetComponent<PlayerCombat>() is PlayerCombat combat&&!combat.TryCancelForDodge())return false;
            if(!(GetComponent<PlayerStamina>()?.TrySpend(PlayerStamina.Action.Dodge)??true))return false;
            direction.y = 0; dashDirection = direction.sqrMagnitude > .01f ? direction.normalized : transform.forward;
            ShortDodge=false;dashStarted = Time.time; dashEnds = Time.time + DodgeDuration;
            Codice.Presentation.ActorPresentation.Install(gameObject).Play("Dash", DodgeDuration, true);
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Dash);
            if (primaryReady) nextDash = Time.time + DodgeCooldown; else secondDashReady = Time.time + DodgeCooldown;
            return true;
        }
        public bool TryJump()
        {
            if(GuardMovementLocked||AttackMovementLocked)return false;
            if(GetComponent<PlayerCombat>()?.BowMotionLocked??false)return false;
            if (!state.IsAlive || ControlsBlocked || IsStaggered || IsDashing ||
                (!Grounded && Time.time - lastGrounded > .1f) || verticalSpeed > 0 ||
                Time.timeScale == 0 || Codice.UI.RunNavigation.Loading ||
                (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible)) return false;
            verticalSpeed = Mathf.Sqrt(2 * JumpGravity * jumpHeight);
            lastGrounded = float.NegativeInfinity; jumpQueuedUntil = -1;
            return true;
        }
        public bool TryActivatePassive(int slot)
        {
            if (ControlsBlocked || IsStaggered || Time.timeScale == 0 ||
                (Codice.UI.RunHUD.Instance && (Codice.UI.RunHUD.Instance.ResultsVisible || (!Codice.Multiplayer.LanAvatar.IsRemote(this) && Codice.UI.RunHUD.Instance.Paused)))) return false;
            bool activated=state.ActivatePassive(slot);
            if(activated)Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Power);
            return activated;
        }
        // Esquiva perfecta: an attack that passes through the roll's invulnerable frames empowers the next hit.
        public float PerfectDodgeUntil {get;private set;}
        float nextPerfectDodge;
        public bool ConsumePerfectDodge(){if(Time.time>=PerfectDodgeUntil)return false;PerfectDodgeUntil=0;return true;}
        void PerfectDodge(DamageInfo hit)
        {
            if(Time.time<nextPerfectDodge)return;nextPerfectDodge=Time.time+.6f;PerfectDodgeUntil=Time.time+2.5f;
            var at=transform.position+Vector3.up*1.1f;
            Codice.Presentation.LordsAttackVfx.ImpactTinted(at,-hit.direction,new Color(.55f,.8f,1f,.9f),.6f);
            Codice.Presentation.ImpactVfx.Spawn("VFX_ImpactToon",at,Camera.main?Camera.main.transform.position-at:Vector3.back,.4f,.8f);
            Codice.Presentation.HitStop.Do(.07f);Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Power);
            Codice.UI.RunHUD.Instance?.Announce("ESQUIVA PERFECTA · Tu próximo golpe es crítico");
        }
        internal Codice.Multiplayer.DefenseTiming OwnedDefense()
        {
            if(!state.IsAlive)return Codice.Multiplayer.DefenseTiming.Open;
            if(DodgeInvulnerable)return Codice.Multiplayer.DefenseTiming.Roll;
            if(ControlsBlocked||IsStaggered)return Codice.Multiplayer.DefenseTiming.Open;
            var guard=GetComponent<ShinobiGuard>();
            if(state.Class==PlayerClass.Shinobi&&guard&&!guard.Broken&&(guard.InDeflect||guard.Guarding))
                return guard.Perfect?Codice.Multiplayer.DefenseTiming.Perfect:guard.InDeflect?Codice.Multiplayer.DefenseTiming.Deflect:Codice.Multiplayer.DefenseTiming.Guard;
            if(GetComponent<PlayerCombat>().Blocking)return GetComponent<ClassIdentity>().InOwnedParryWindow?
                Codice.Multiplayer.DefenseTiming.Perfect:Codice.Multiplayer.DefenseTiming.Guard;
            return Codice.Multiplayer.DefenseTiming.Open;
        }
        internal void ResolveOwnedDefense(DamageInfo hit,Codice.Multiplayer.DefenseTiming result)
        {
            if(!state.IsAlive)return;
            if(result==Codice.Multiplayer.DefenseTiming.Roll){PerfectDodge(hit);return;}
            if(result>=Codice.Multiplayer.DefenseTiming.Guard&&state.Class==PlayerClass.Shinobi)
                GetComponent<ShinobiGuard>().SetContactTiming(result);
            if(state.Class!=PlayerClass.Shinobi)GetComponent<ClassIdentity>().SetContactTiming(result);
            ApplyDamage(hit,true,result);
        }
        public void ReceiveDamage(DamageInfo hit)
        {
            if(Codice.Multiplayer.LanSession.Host&&Codice.Multiplayer.LanAvatar.IsRemote(this)&&
               !(hit.source&&hit.source.GetComponentInParent<PlayerCharacter>())&&Codice.Multiplayer.LanSession.Instance.AskDefense(this,hit))return;
            ApplyDamage(hit,false,Codice.Multiplayer.DefenseTiming.Unspecified);
        }
        void ApplyDamage(DamageInfo hit,bool confirmed,Codice.Multiplayer.DefenseTiming result)
        {
            if(Codice.Multiplayer.LanSession.Client)return;
            if(Codice.Multiplayer.LanSession.Active&&hit.source&&hit.source.GetComponentInParent<PlayerCharacter>())return;
            if (!confirmed && DodgeInvulnerable && state.IsAlive && hit.amount>0 && !hit.secondary && hit.source && !hit.source.GetComponentInParent<PlayerCharacter>()) PerfectDodge(hit);
            if ((!confirmed&&DodgeInvulnerable) || !state.IsAlive) return;
            if(hit.source&&!hit.source.GetComponentInParent<PlayerCharacter>())hit.amount*=Codice.Multiplayer.CoopScaling.EnemyDamage;
            var mechanics = GetComponent<Codice.Mechanics.PlayerMechanics>();
            if (mechanics && mechanics.Invulnerable) return;
            float originalDamage=hit.amount;
            int vengeance=state.Boon(AshBoon.VengefulEmber);
            if(vengeance>0&&hit.melee&&!hit.secondary&&hit.source&&UnityEngine.Random.value<.25f*vengeance){var attacker=hit.source.GetComponentInParent<IDamageReceiver>();if(attacker!=null&&!(attacker is PlayerCharacter)){attacker.ReceiveDamage(new DamageInfo{amount=20,source=gameObject,direction=-hit.direction,secondary=true});Codice.Mechanics.RelicPulse.Spawn(hit.source.transform.position+Vector3.up,.6f,new Color(1,.35f,.05f));}}
            hit.amount=GetComponent<ClassIdentity>()?.Incoming(hit,confirmed)??hit.amount;
            var guard=GetComponent<ShinobiGuard>();if(guard&&(!confirmed||result>=Codice.Multiplayer.DefenseTiming.Guard))hit.amount=guard.Resolve(hit,confirmed);
            if (mechanics) hit.amount = mechanics.ModifyIncoming(hit);
            if(hit.amount>0)Codice.Presentation.GameAudio.Play(mechanics&&mechanics.ShieldActive&&hit.amount<originalDamage?Codice.Presentation.SoundCue.Block:Codice.Presentation.SoundCue.Hurt);
            float healthBefore=state.Health+state.Armor;
            bool damaged=state.Damage(hit.amount,hit.poison);
            if(damaged){GetComponent<Codice.Mechanics.EstusFlask>()?.Interrupt();if(!hit.secondary)GetComponent<ElementalStatus>()?.Add(hit.element!=Element.None?hit.element:hit.poison?Element.Poison:Element.None,hit.buildup>0?hit.buildup:25,hit.source);}
            CombatFeedback.Report(this,hit,Mathf.Max(0,healthBefore-state.Health-state.Armor),hit.amount<originalDamage);
            if (damaged && mechanics) mechanics.AfterDamage(hit);
        }
        public void RestoreRunState(string snapshot)
        { state.Restore(snapshot); initialClass = state.Class; ApplyVisual(); }
        public void ReviveFromAlly(float fraction) { state.ReviveWith(fraction); ControlsBlocked = false; if (!Codice.Multiplayer.LanAvatar.IsRemote(this) && !Codice.Multiplayer.LanSession.Client) CaptureCursor(true); }
        public void TryInteract()
        {
            if (ControlsBlocked || !state.IsAlive) return;
            if (Codice.Multiplayer.LanSession.Host) { var ally = Codice.Multiplayer.LanSession.Instance.FallenAllyNear(this); if(ally)return; }   /* coop: revive a fallen teammate */
            (InteractionTarget.Find(this) as IPlayerInteractable)?.Interact(this);
        }
        public void BeginShop(Merchant shop)
        { merchant = shop; ControlsBlocked = true; CaptureCursor(false); ShopOpened?.Invoke(shop); }
        public void EndShop()
        { merchant = null; ControlsBlocked = false; CaptureCursor(state.IsAlive); }
        // A new boss attempt resets the run state and all temporary movement effects.
        public bool RestartAfterDefeat(Vector3 position, Quaternion rotation)
        {
            if (state.IsAlive) return false;
            GetComponent<Codice.Mechanics.EstusFlask>()?.Interrupt();
            if (merchant) merchant.CloseShop();
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            state.Initialize(initialClass);
            verticalSpeed = pitch = dashEnds = nextDash = secondDashReady = slowUntil = staggerUntil = 0;
            movementMultiplier = 1; dashDirection = Vector3.zero; ControlsBlocked = false;
            if (view) view.transform.localRotation = Quaternion.identity;
            CaptureCursor(true);
            return true;
        }
        void OnDeath() { if (merchant) merchant.CloseShop(); ControlsBlocked = true; CaptureCursor(false); }
        void CaptureCursor(bool capture)
        { if(Codice.Multiplayer.LanAvatar.IsRemote(this))return; Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !capture; }
#if UNITY_EDITOR
        [ContextMenu("Prueba / Dar 500 oro (solo Play)")]
        void GrantTestGold() { if (Application.isPlaying) state.AddGold(500); }
#endif
    }
}


