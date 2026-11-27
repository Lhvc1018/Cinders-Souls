using UnityEngine;
namespace Codice.Characters
{
    public sealed partial class PlayerCharacter
    {
        public const float DodgeDuration = .42f;
        public const float DodgeCooldown = .85f;
        public bool ShortDodge {get;private set;}
        public float ActiveDodgeDuration=>DodgeDuration;
        public string DodgeAnimation=>"Dash";
        public bool HandleDodgePress(Vector3 direction)=>TryDash(direction);
        const float JumpGravity = 22f;
        float dashStarted = float.NegativeInfinity;
        float lastGrounded = float.NegativeInfinity, jumpQueuedUntil = -1;
        public float VerticalSpeed => verticalSpeed;
        public bool SprintRequested {get;set;}
        public bool Sprinting => !ControlsBlocked && !IsStaggered && !IsDashing && !(GetComponent<PlayerCombat>()?.IsSwinging??false);
        public Vector3 MotorVelocity { get; private set; }
        Vector3 moveVelocity;
        public bool GuardMovementLocked => (GetComponent<PlayerCombat>()?.Blocking??false)||(GetComponent<ShinobiGuard>()?.Guarding??false);
        public float NetworkActionUntil;   /* coop guest: host-resolved attack in progress */
        public float Pitch => pitch;
        public bool AttackMovementLocked => Time.time<NetworkActionUntil|| (GetComponent<Codice.Mechanics.PlayerMechanics>()?.UltimateActive??false)|| (GetComponent<PlayerCombat>()?.IsSwinging??false)||(GetComponent<StaffSpells>()?.Casting??false)||(GetComponent<RunSpellbook>()?.Casting??false)||(GetComponent<ChargedAttack>()?.IaidoActive??false);
        public Vector3 DodgeDirection => dashDirection;
        public bool DodgeInvulnerable => IsDashing && Time.time - dashStarted >= .04f && Time.time - dashStarted < .28f;
        /// Coop guest: the same camera, lock-on, dodge, jump, sprint and motor as the host's local player (it used to be a simplified motor without lock-on or sprint).
        public void ClientLocomotion(bool allowed)
        {
            if (!state.IsAlive || ControlsBlocked || !view) { if (state.IsAlive) SimulateMotor(Vector3.zero, false, Time.deltaTime); return; }
            var lockOn = GetComponent<PlayerLockOn>(); Vector2 input = Vector2.zero;
            if (allowed)
            {
                var delta = Codice.Mechanics.GameInput.Look(mouseSensitivity);
                if (lockOn && Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.LockOn)) lockOn.Toggle();
                if (lockOn) lockOn.Validate();
                if (lockOn && lockOn.Target)
                {
                    Vector3 to = lockOn.Point - (transform.position + Vector3.up * 1.45f); var flat = Vector3.ProjectOnPlane(to, Vector3.up);
                    if (flat.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 720 * Time.deltaTime);
                    pitch = Mathf.LerpAngle(pitch, Mathf.Clamp(-Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg, -45, 60), 1 - Mathf.Exp(-10 * Time.deltaTime));
                }
                else { transform.Rotate(0, delta.x, 0); pitch = Mathf.Clamp(pitch - delta.y, -85, 85); }
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                input = Codice.Mechanics.GameInput.Move();
            }
            Vector3 direction = transform.TransformDirection(new Vector3(input.x, 0, input.y));
            if (allowed && Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Dodge)) HandleDodgePress(direction);
            if (allowed && Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Jump)) jumpQueuedUntil = Time.time + .12f;
            SprintRequested = allowed && Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Sprint);
            SimulateMotor(direction, allowed && Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Jump), Time.deltaTime);
        }
        int lastMotorFrame = -1;
        void LateUpdate()
        {
            if (lastMotorFrame == Time.frameCount || Time.timeScale == 0 || Codice.UI.RunNavigation.Loading ||
                Codice.Multiplayer.LanSession.Active || !state.IsAlive) return;
            SimulateMotor(Vector3.zero, true, Time.deltaTime);
        }
        readonly RaycastHit[] groundHits = new RaycastHit[16];
        bool ProbeGround()
        {
            if (!controller || !controller.enabled) return false;
            if (controller.isGrounded) return true;
            float radius = controller.radius * Mathf.Abs(transform.lossyScale.x) * .9f;
            Vector3 bottom = transform.TransformPoint(controller.center) - Vector3.up * (controller.height * Mathf.Abs(transform.lossyScale.y) * .5f);
            int count = Physics.SphereCastNonAlloc(bottom + Vector3.up * (radius + .06f), radius, Vector3.down,
                groundHits, .14f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!groundHits[i].transform.IsChildOf(transform) && Vector3.Angle(groundHits[i].normal, Vector3.up) <= controller.slopeLimit)
                    return true;
            return false;
        }
        // The capsule is the only movement authority. Animation root motion remains disabled.
        public void SimulateMotor(Vector3 direction, bool jumpHeld, float deltaTime)
        {
            if (!controller || !controller.enabled || deltaTime <= 0 || !state.IsAlive) return;
            lastMotorFrame = Time.frameCount;
            float dt = Mathf.Min(deltaTime, .05f);
            bool grounded = Grounded;
            if (grounded) { lastGrounded = Time.time; if (verticalSpeed < 0) verticalSpeed = -2; }
            if (jumpQueuedUntil >= Time.time) TryJump();
            float gravity = verticalSpeed < 0 ? JumpGravity * 1.6f : !jumpHeld && verticalSpeed > 0 ? JumpGravity * 2f : JumpGravity;
            verticalSpeed = Mathf.Max(-35, verticalSpeed - gravity * dt);
            direction = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(direction, Vector3.up), 1);
            float specialSpeed = GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShadowStep == true ? 1.75f : 1;
            bool bowLocked=GetComponent<PlayerCombat>()?.BowMotionLocked??false;
            bool stopped=IsStaggered||ControlsBlocked||bowLocked||GuardMovementLocked||AttackMovementLocked;
            Vector3 desired=direction*(state.MoveSpeed/6.5f)*MovementMultiplier*specialSpeed*6f;
            moveVelocity=stopped?Vector3.zero:Vector3.MoveTowards(moveVelocity,desired,(direction.sqrMagnitude>.01f?35f:50f)*dt);
            Vector3 horizontal=moveVelocity;
            if (IsDashing && !IsStaggered && !ControlsBlocked)
            {
                float progress = Mathf.Clamp01((Time.time - dashStarted) / ActiveDodgeDuration);
                horizontal = dashDirection * Mathf.Lerp(13f,4f,progress);
                moveVelocity=Vector3.zero;
            }
            controller.stepOffset = grounded && verticalSpeed <= 0 ? .25f : 0;
            Vector3 before=transform.position;
            var flags = controller.Move((horizontal + Vector3.up * verticalSpeed) * dt);
            MotorVelocity=Vector3.ProjectOnPlane(transform.position-before,Vector3.up)/dt;
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0) verticalSpeed = -2;
        }
    }
}

