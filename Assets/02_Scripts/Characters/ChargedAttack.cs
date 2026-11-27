using System.Collections.Generic;
using Codice.Mechanics;
using Codice.Presentation;
using UnityEngine;
namespace Codice.Characters
{
    /// Hold the heavy button (R2 / RT, keyboard Z) to charge, release to unleash a class-specific heavy strike.
    /// Charge scales damage between 35 % (tap) and 100 % (full, ~0.9 s). Dodging cancels it.
    [DisallowMultipleComponent]
    public sealed class ChargedAttack : MonoBehaviour
    {
        public const float FullCharge = .9f, MaxHold = 2.2f;
        PlayerCharacter player; PlayerCombat combat;
        float chargeStart = -1, readyAt; bool fullNotified, heldBefore;
        GameObject glow; Light glowLight;
        public bool Charging => chargeStart >= 0;
        public float Charge => Charging ? Mathf.Clamp01((Time.time - chargeStart) / FullCharge) : 0;
        public int Executed { get; private set; }
        public static string MoveName(PlayerClass c) => c switch
        {
            PlayerClass.Warrior => "Tajo del Juramento", PlayerClass.Tank => "Sismo de la Muralla", PlayerClass.Berserker => "Torbellino Carmesí",
            PlayerClass.Shinobi => "Iaido de Ceniza", PlayerClass.Archer => "Flecha Perforante", PlayerClass.Mage => "Orbe del Eclipse", _ => "Golpe cargado"
        };
        public static string MoveDescription(PlayerClass c) => c switch
        {
            PlayerClass.Warrior => "tajo frontal que rompe guardias y aturde",
            PlayerClass.Tank => "golpe al suelo que aturde a todo lo que te rodea",
            PlayerClass.Berserker => "giro de dos espadas que golpea dos veces alrededor",
            PlayerClass.Shinobi => "embestida veloz que corta a todos en la línea y rompe postura",
            PlayerClass.Archer => "flecha que atraviesa a todos los enemigos en línea",
            PlayerClass.Mage => "orbe que estalla donde apuntas",
            _ => "golpe pesado"
        };
        void Awake() { player = GetComponent<PlayerCharacter>(); combat = GetComponent<PlayerCombat>(); }
        bool Allowed()
        {
            bool remote = Multiplayer.LanAvatar.IsRemote(player);   /* coop guest on the host: driven by its network 'heavy' button */
            if (!player || !combat || !combat.enabled || Multiplayer.LanSession.Client) return false;
            if (!player.State.IsAlive || player.ControlsBlocked || player.IsStaggered || player.IsDashing || Time.timeScale == 0 || UI.RunNavigation.Loading) return false;
            if (!remote && (Cursor.lockState != CursorLockMode.Locked || UI.RunHUD.HandlesEscapeThisFrame || (UI.RunHUD.Instance && (UI.RunHUD.Instance.Paused || UI.RunHUD.Instance.ResultsVisible)))) return false;
            if (combat.IsSwinging || combat.IsReloading || combat.Blocking || Time.time < readyAt) return false;
            var m = GetComponent<PlayerMechanics>(); if (m && (m.UltimateActive || m.UltimateQueued)) return false;
            return true;
        }
        void Update()
        {
            if (!player) return;
            var av = GetComponent<Multiplayer.LanAvatar>(); bool remoteInput = av && av.remote;
            if (Multiplayer.LanSession.Client) return;
            bool held = remoteInput ? av.heavy : GameInput.Held(GameAction.HeavyAttack); bool pressed = remoteInput ? held && !heldBefore : GameInput.Pressed(GameAction.HeavyAttack); heldBefore = held;
            if (!Charging)
            {
                if (pressed && Allowed() && (GetComponent<PlayerStamina>()?.TrySpend(PlayerStamina.Action.Charged) ?? true)) Begin();
                return;
            }
            if (!player.State.IsAlive || player.IsStaggered || player.IsDashing || player.ControlsBlocked || Time.timeScale == 0) { Cancel(); return; }
            float c = Charge;
            if (glow) { glow.transform.position = Origin() + player.transform.forward * .45f - Vector3.up * .35f; glow.transform.localScale = Vector3.one * Mathf.Lerp(.08f, .32f, c) * (1 + .08f * Mathf.Sin(Time.time * 30)); }
            if (glowLight) glowLight.intensity = Mathf.Lerp(.5f, 4.5f, c);
            if (c >= 1 && !fullNotified) { fullNotified = true; GameAudio.PlayAt(SoundCue.Power, transform.position); GameInput.Rumble(.25f, .45f, .12f); }
            if (!held || Time.time - chargeStart >= MaxHold) Release();
        }
        void Begin()
        {
            chargeStart = Time.time; fullNotified = false;
            glow = GameObject.CreatePrimitive(PrimitiveType.Sphere); glow.name = "Charged attack glow"; Destroy(glow.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); var col = ClassColor(); mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            glow.GetComponent<Renderer>().sharedMaterial = mat; glow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowLight = glow.AddComponent<Light>(); glowLight.type = LightType.Point; glowLight.color = col; glowLight.range = 5;
            GameAudio.PlayAt(SoundCue.BossWindup, transform.position);
        }
        public void Cancel() { chargeStart = -1; if (glow) Destroy(glow); glow = null; }
        Color ClassColor() => player.State.Class switch
        {
            PlayerClass.Mage => new Color(.55f, .45f, 1f), PlayerClass.Archer => new Color(.6f, 1f, .7f), PlayerClass.Shinobi => new Color(1f, .3f, .3f),
            PlayerClass.Tank => new Color(1f, .7f, .3f), PlayerClass.Berserker => new Color(.95f, .15f, .2f), _ => new Color(1f, .82f, .5f)
        };
        Vector3 Origin() => HumanPreviewCamera.Origin(player);
        Vector3 Aim() => HumanPreviewCamera.Aim(player);
        float BaseDamage()
        {
            var w = player.State.ActiveWeapon; if (w == null) return 22;
            var m = GetComponent<PlayerMechanics>(); float d = m ? m.RollBasicDamage(w) : w.Damage;
            return Mathf.Max(d, w.Damage) * player.State.BoonDamage;
        }
        void Release()
        {
            float c = Mathf.Lerp(.35f, 1f, Charge); bool full = Charge >= 1;
            Cancel(); readyAt = Time.time + .55f; Executed++;
            float dmg = BaseDamage() * c;
            // The Iaido drives its own draw-cut animation (synced to the dash); the rest use the class attack.
            if (player.State.Class != PlayerClass.Shinobi) GetComponent<ActorPresentation>()?.Attack(player.State.Class == PlayerClass.Archer || player.State.Class == PlayerClass.Mage, .55f);
            GameInput.Rumble(.5f * c, .8f * c, .18f);
            Vector3 o = Origin(), f = Aim(), flat = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
            switch (player.State.Class)
            {
                case PlayerClass.Tank:
                    Area(transform.position + Vector3.up, Mathf.Lerp(3.2f, 5f, c), 360, dmg * 2.4f, full ? 1.2f : .6f, 5, true);
                    ImpactVfx.Slam(transform.position + flat * .6f); GameAudio.PlayAt(SoundCue.BossImpact, transform.position); break;
                case PlayerClass.Berserker:
                    Area(transform.position + Vector3.up, 3.2f, 360, dmg * 1.5f, .3f, 2, false);
                    Invoke(nameof(SecondSpin), .22f); pendingSpin = dmg * 1.5f;
                    LordsAttackVfx.SlashTinted(o + flat, flat, new Color(.95f, .2f, .25f, .85f), 2f, 0); GameAudio.PlayAt(SoundCue.Heavy, transform.position); break;
                case PlayerClass.Shinobi:
                    StartCoroutine(Iaido(flat, Mathf.Lerp(3.5f, 6.5f, c), dmg * 3f, full)); break;
                case PlayerClass.Archer:
                    Pierce(o, f, dmg * 3f, full); break;
                case PlayerClass.Mage:
                    Orb(o, f, dmg * 2.6f, Mathf.Lerp(2.4f, 3.6f, c)); break;
                default:
                    Area(o, Mathf.Lerp(2.6f, 3.4f, c), 75, dmg * 2.8f, full ? .9f : .45f, 4, true, f);
                    LordsAttackVfx.SlashTinted(o + flat * 1.4f, flat, new Color(1, .8f, .45f, .9f), full ? 2.2f : 1.6f, -70); GameAudio.PlayAt(SoundCue.Heavy, transform.position); break;
            }
            Debug.Log($"[Charged] {player.State.Class} {MoveName(player.State.Class)} charge={c:0.00} dmg={dmg:0}");
        }
        float pendingSpin;
        void SecondSpin() { Area(transform.position + Vector3.up, 3.2f, 360, pendingSpin, .3f, 3, false); LordsAttackVfx.SlashTinted(Origin() + transform.forward, transform.forward, new Color(.95f, .2f, .25f, .85f), 2f, 180); }
        /// Damages every enemy (and training dummy) inside a radius / cone.
        int Area(Vector3 center, float radius, float cone, float damage, float stun, float push, bool guardBreak, Vector3? forward = null)
        {
            var dir = Vector3.ProjectOnPlane(forward ?? transform.forward, Vector3.up).normalized; int hits = 0;
            foreach (var t in Targets(center, radius))
            {
                var to = Vector3.ProjectOnPlane(t.transform.position - transform.position, Vector3.up);
                if (cone < 360 && to.sqrMagnitude > .25f && Vector3.Angle(dir, to) > cone * .5f) continue;
                Strike(t, damage, to.sqrMagnitude > .01f ? to.normalized : dir, stun, push, guardBreak); hits++;
            }
            return hits;
        }
        List<Component> Targets(Vector3 center, float radius)
        {
            var list = new List<Component>();
            foreach (var col in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var r = col.GetComponentInParent<IDamageReceiver>() as Component;
                if (!r || list.Contains(r)) continue;
                if (CombatTargets.IsEnemy(r) || r.GetComponent<Prologue.TrainingDummy>()) list.Add(r);
            }
            return list;
        }
        void Strike(Component t, float damage, Vector3 dir, float stun, float push, bool guardBreak)
        {
            var point = t.GetComponent<Collider>() ? t.GetComponent<Collider>().bounds.center : t.transform.position + Vector3.up;
            (t as IDamageReceiver)?.ReceiveDamage(new DamageInfo { amount = damage, critical = true, hasImpactPoint = true, impactPoint = point, direction = dir, source = gameObject, melee = player.State.Class != PlayerClass.Archer && player.State.Class != PlayerClass.Mage, breaksGuard = guardBreak, knockback = push });
            if (stun > 0) CombatTargets.Stun(t, stun);
            if (player.State.Class == PlayerClass.Shinobi) EnemyPosture.Add(t, 40);
            if (player.State.BoonLifesteal > 0) player.State.Heal(damage * player.State.BoonLifesteal);
            HitStop.Do(.06f);
        }
        System.Collections.IEnumerator Iaido(Vector3 dir, float distance, float damage, bool full)
        {
            // Root causes of the old Iaido: the body kept the camera/input facing while the dash went along the aim, input
            // movement was still added on top, and a random combo slash (0.55 s) played over a 0.16 s dash.
            // Now: face the dash, lock locomotion, play one draw-cut sized to draw + dash + recovery, VFX on contact.
            if (dir.sqrMagnitude < .01f) dir = transform.forward; dir.Normalize();
            var cc = GetComponent<CharacterController>(); var ap = GetComponent<ActorPresentation>(); var start = transform.position; float t = 0; var struck = new HashSet<Component>();
            IaidoActive = true; IaidoDirection = dir; transform.rotation = Quaternion.LookRotation(dir);
            const float Draw = .07f, Dash = .16f, Recover = .14f;
            if (ap) { string cut = ap.Body && ap.Body.HasState(0, Animator.StringToHash("KatanaFinisher")) ? "KatanaFinisher" : "KatanaSlashA"; ap.Play(cut, Draw + Dash + Recover, true); }
            yield return new WaitForSeconds(Draw);
            GameAudio.PlayAt(SoundCue.Dash, transform.position);
            while (t < Dash && player.State.IsAlive && !player.IsStaggered)
            {
                transform.rotation = Quaternion.LookRotation(dir);
                float step = distance / Dash * Time.deltaTime; if (cc && cc.enabled) cc.Move(dir * step + Vector3.down * 4f * Time.deltaTime); t += Time.deltaTime;
                foreach (var target in Targets(transform.position + Vector3.up, 1.6f)) if (struck.Add(target)) Strike(target, damage, dir, full ? .8f : .4f, 1, true);
                yield return null;
            }
            LordsAttackVfx.SlashTinted(Vector3.Lerp(start, transform.position, .5f) + Vector3.up, dir, new Color(1, .3f, .3f, .9f), 2.6f, 0);
            GameAudio.PlayAt(SoundCue.Heavy, transform.position);
            float until = Time.time + Recover; while (Time.time < until) { transform.rotation = Quaternion.LookRotation(dir); yield return null; }
            IaidoActive = false;
        }
        void Pierce(Vector3 o, Vector3 f, float damage, bool full)
        {
            var hits = Physics.SphereCastAll(o, .45f, f, 45, ~0, QueryTriggerInteraction.Ignore); var struck = new HashSet<Component>(); float end = 45;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(transform)) continue;
                var r = h.collider.GetComponentInParent<IDamageReceiver>() as Component;
                if (r && (CombatTargets.IsEnemy(r) || r.GetComponent<Prologue.TrainingDummy>())) { if (struck.Add(r)) Strike(r, damage, f, full ? .5f : 0, 2, false); continue; }
                if (!r && !h.collider.isTrigger) { end = h.distance; PresentationVfx.Impact(h.point, h.normal); break; }
            }
            Beam(o + f * .6f, o + f * end, new Color(.6f, 1f, .7f), .09f);
            PresentationVfx.Muzzle(o + f * .6f, f); GameAudio.PlayAt(SoundCue.Bow, o);
        }
        void Orb(Vector3 o, Vector3 f, float damage, float radius)
        {
            Vector3 point = Physics.Raycast(o, f, out var hit, 32, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(transform) ? hit.point : o + f * 32;
            PresentationVfx.MagicCast(o + f * .6f, f); GameAudio.PlayAt(SoundCue.SpellArcane, o); Beam(o + f * .6f, point, new Color(.6f, .5f, 1f), .14f);
            PresentationVfx.Explosion(point);
            foreach (var t in Targets(point, radius)) Strike(t, damage, (t.transform.position - point).normalized, .5f, 3, false);
        }
        static void Beam(Vector3 a, Vector3 b, Color c, float width)
        {
            var go = new GameObject("Charged beam"); var lr = go.AddComponent<LineRenderer>(); lr.positionCount = 2; lr.SetPosition(0, a); lr.SetPosition(1, b);
            lr.startWidth = width; lr.endWidth = width * .4f; var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.color = c; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); lr.sharedMaterial = mat;
            Destroy(go, .18f);
        }
        void OnDisable() { Cancel(); IaidoActive = false; }
        /// True during the Iaido draw, dash and recovery: locomotion and turning are owned by the cut.
        public bool IaidoActive { get; private set; }
        public Vector3 IaidoDirection { get; private set; } = Vector3.forward;
    }
}
