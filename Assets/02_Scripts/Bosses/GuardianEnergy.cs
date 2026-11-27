using System.Collections.Generic;
using Codice.Characters;
using Codice.Presentation;
using UnityEngine;
namespace Codice.Bosses
{
    /// Gurdi del Abismo (Muck Guardian) energy attacks, rebuilt from the original behaviour (LaserTest.cs + GuardianLaser.anim):
    /// the native rig kept its "Laser" / "ChargeLaser" anchors but the import stripped their particle systems and line
    /// renderer, so the beam never showed. Timing follows the Muck clip: charge in the chest, then a continuous beam whose
    /// end chases the target with lag (dodge by moving), ticking damage where it lands. The ground slam releases an
    /// expanding energy ring (jump it or stay out of it).
    public static class GuardianEnergy
    {
        static Material Core(Color c) => SoftParticle.Make(new Color(c.r * 4, c.g * 4, c.b * 4, 1), true);
        public static Transform Anchor(BossCharacter boss) => AnchorIn(boss.transform);
        /// Chest anchor inside any copy of the rig (the host's boss or a guest's ghost of it).
        public static Transform AnchorIn(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == "Laser") return t;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == "Torso") return t;
            return root;
        }
        public const string NetCharge = "GurdiCharge", NetBeam = "GurdiBeam", NetWave = "GurdiWave";
        static int OwnerId(PlayerCharacter p) { var a = p ? p.GetComponent<Codice.Multiplayer.LanAvatar>() : null; return a && a.remote ? a.id : 0; }
        /// Chest charge: inward-flowing motes, a growing core and a rising light. Returns the root (destroy to cancel).
        public static GameObject Charge(BossCharacter boss, float seconds, Color tint)
        {
            if (Codice.Multiplayer.LanSession.Host) Codice.Multiplayer.LanSession.Instance.EmitEffect(NetCharge, boss.transform.position, 1, seconds);
            return ChargeAt(boss.transform, seconds, tint);
        }
        public static GameObject ChargeAt(Transform bossRoot, float seconds, Color tint)
        {
            var anchor = AnchorIn(bossRoot); var root = new GameObject("Gurdi · carga del rayo");
            // "Laser" is switched off in the native rig until it fires: hang the charge on its (always active) parent, same spot.
            root.transform.SetParent(anchor.parent ? anchor.parent : anchor, false); root.transform.position = anchor.position;
            var ps = root.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.startLifetime = .55f; m.startSpeed = -4f; m.startSize = new ParticleSystem.MinMaxCurve(.12f, .28f); m.maxParticles = 160; m.simulationSpace = ParticleSystemSimulationSpace.Local; m.duration = seconds; m.loop = false;
            var e = ps.emission; e.rateOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 30, 1, 140)); var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.4f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(.2f, 1) }); col.color = g;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Core(tint); ps.Play();
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(orb.GetComponent<Collider>()); orb.transform.SetParent(root.transform, false); orb.transform.localScale = Vector3.zero;
            orb.GetComponent<Renderer>().sharedMaterial = Core(tint); orb.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var light = root.AddComponent<Light>(); light.color = tint; light.range = 7; light.intensity = 0; light.shadows = LightShadows.None;
            root.AddComponent<GuardianCharge>().Init(orb.transform, light, seconds);
            GameAudio.PlayAt(SoundCue.BossWindup, anchor.position); return root;
        }
        public static GuardianBeam Fire(BossCharacter boss, PlayerCharacter target, float seconds, Color tint)
        {
            if (Codice.Multiplayer.LanSession.Host) Codice.Multiplayer.LanSession.Instance.EmitEffect(NetBeam, boss.transform.position, OwnerId(target), seconds);
            var go = new GameObject("Gurdi · rayo del pecho"); var b = go.AddComponent<GuardianBeam>(); b.Init(boss, Anchor(boss), target, target ? target.transform : null, seconds, tint, Core(tint), SoftParticle.BeamMaterial(new Color(tint.r * 5, tint.g * 5, tint.b * 5, 1))); return b;
        }
        /// Guest copy: same beam drawn from the ghost's chest toward the target's transform, no damage.
        public static void BeamVisual(Transform bossRoot, Transform target, float seconds, Color tint)
        {
            var go = new GameObject("Gurdi · rayo del pecho (red)"); go.AddComponent<GuardianBeam>().Init(null, AnchorIn(bossRoot), null, target, seconds, tint, Core(tint), SoftParticle.BeamMaterial(new Color(tint.r * 5, tint.g * 5, tint.b * 5, 1)));
        }
        public static void Shockwave(BossCharacter boss, Vector3 center, PlayerCharacter target, Color tint)
        {
            if (Codice.Multiplayer.LanSession.Host) Codice.Multiplayer.LanSession.Instance.EmitEffect(NetWave, center, 1, 1);
            ShockwaveVisual(boss, center, target, tint);
        }
        public static void ShockwaveVisual(BossCharacter boss, Vector3 center, PlayerCharacter target, Color tint)
        {
            var go = new GameObject("Gurdi · onda de energía"); go.transform.position = center + Vector3.up * .15f; go.AddComponent<GuardianShockwave>().Init(boss, target, tint, Core(tint));
        }
    }
    public sealed class GuardianCharge : MonoBehaviour
    {
        Transform orb; Light l; float seconds, age;
        public void Init(Transform o, Light light, float s) { orb = o; l = light; seconds = Mathf.Max(.1f, s); }
        void Update()
        {
            age += Time.deltaTime; float k = Mathf.Clamp01(age / seconds); float pulse = 1 + .15f * Mathf.Sin(age * (8 + 20 * k));
            if (orb) orb.localScale = Vector3.one * Mathf.Lerp(.15f, .9f, k) * pulse; if (l) l.intensity = Mathf.Lerp(.5f, 6f, k) * pulse;
        }
    }
    /// Continuous beam: core + halo lines, end chasing the target (lag), wall-clipped, damage ticks at the impact point.
    public sealed class GuardianBeam : MonoBehaviour
    {
        BossCharacter boss; Transform anchor, aimAt; PlayerCharacter target; float seconds, age, tick; Vector3 end; LineRenderer core, halo; ParticleSystem sparks; Light endLight; Color tint;
        const float Chase = 4.5f, TickEvery = .2f, TickDamage = 5, HitRadius = 1.05f;   // ~25 dps if you stand in it
        public void Init(BossCharacter b, Transform a, PlayerCharacter t, Transform aimTransform, float s, Color c, Material mat, Material beamMat)
        {
            boss = b; anchor = a; target = t; aimAt = aimTransform; seconds = s; tint = c; end = a.position + a.root.forward * 4;
            core = Line("Núcleo", beamMat, .2f); halo = Line("Halo", beamMat, .75f); halo.startColor = halo.endColor = new Color(1, 1, 1, .35f);
            var sp = new GameObject("Impacto"); sp.transform.SetParent(transform, false); sparks = sp.AddComponent<ParticleSystem>(); sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = sparks.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.2f, .45f); m.startSpeed = new ParticleSystem.MinMaxCurve(3, 7); m.startSize = new ParticleSystem.MinMaxCurve(.05f, .14f); m.maxParticles = 200; m.simulationSpace = ParticleSystemSimulationSpace.World; m.gravityModifier = .6f;
            var e = sparks.emission; e.rateOverTime = 90; var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = .2f;
            sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat; sparks.Play();
            endLight = sp.AddComponent<Light>(); endLight.color = c; endLight.range = 6; endLight.intensity = 5; endLight.shadows = LightShadows.None;
            if (boss) boss.Track(gameObject); GameAudio.PlayAt(SoundCue.SpellArcane, a.position);
        }
        LineRenderer Line(string n, Material mat, float width)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false); var l = go.AddComponent<LineRenderer>(); l.positionCount = 2; l.useWorldSpace = true; l.sharedMaterial = mat;
            l.widthMultiplier = width; l.numCapVertices = 6; l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.textureMode = LineTextureMode.Stretch; return l;
        }
        void Update()
        {
            age += Time.deltaTime;
            if ((boss ? !boss.IsAlive : false) || !anchor || age >= seconds) { Destroy(gameObject); return; }
            var origin = anchor.position; var aim = aimAt && (!target || target.State.IsAlive) ? aimAt.position + Vector3.up * 1.1f : end;
            end = Vector3.Lerp(end, aim, 1 - Mathf.Exp(-Chase * Time.deltaTime));   // lagging chase: running sideways escapes it
            var dir = end - origin; float dist = dir.magnitude; var hitPoint = end;
            if (dist > .01f && Physics.Raycast(origin, dir / dist, out var hit, dist + 30, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(anchor.root)) hitPoint = hit.point;
            else hitPoint = origin + (dist > .01f ? dir / dist : anchor.root.forward) * Mathf.Max(dist, 1);
            float flicker = 1 + .18f * Mathf.Sin(age * 47) + .1f * Mathf.Sin(age * 83); float fade = Mathf.Clamp01(age / .12f) * Mathf.Clamp01((seconds - age) / .2f);
            foreach (var l in new[] { core, halo }) { l.SetPosition(0, origin); l.SetPosition(1, hitPoint); }
            core.widthMultiplier = .2f * flicker * fade; halo.widthMultiplier = .75f * flicker * fade;
            sparks.transform.position = hitPoint; endLight.intensity = 5 * flicker * fade;
            tick -= Time.deltaTime;
            if (tick <= 0 && boss && target && target.State.IsAlive && !Codice.Multiplayer.LanSession.Client)
            {
                tick = TickEvery;
                // Muck damages where the beam lands; here also anyone standing across the beam's last 1.5 m.
                var p = target.transform.position + Vector3.up * 1f; var seg = hitPoint - origin; float t = Mathf.Clamp01(Vector3.Dot(p - origin, seg) / Mathf.Max(.01f, seg.sqrMagnitude));
                float alongEnd = (1 - t) * seg.magnitude; float off = Vector3.Distance(p, origin + seg * t);
                if (off < HitRadius && alongEnd < 1.6f) target.ReceiveDamage(new DamageInfo { amount = TickDamage, source = boss.gameObject, direction = seg.normalized, secondary = true });
            }
        }
    }
    /// Ground slam: an energy ring that expands along the floor; hurts once if it passes under a grounded target.
    public sealed class GuardianShockwave : MonoBehaviour
    {
        BossCharacter boss; PlayerCharacter target; float age; bool hitDone; LineRenderer ring, glow; Light l;
        const float Duration = .95f, MaxRadius = 9f, Damage = 20;
        public void Init(BossCharacter b, PlayerCharacter t, Color c, Material mat)
        {
            boss = b; target = t; ring = Ring(mat, .35f); glow = Ring(mat, 1.1f); glow.startColor = glow.endColor = new Color(1, 1, 1, .3f);
            l = gameObject.AddComponent<Light>(); l.color = c; l.range = 10; l.intensity = 6; l.shadows = LightShadows.None;
            var burst = new GameObject("Chispas"); burst.transform.SetParent(transform, false); var ps = burst.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.startLifetime = .7f; m.startSpeed = new ParticleSystem.MinMaxCurve(6, 11); m.startSize = new ParticleSystem.MinMaxCurve(.08f, .2f); m.maxParticles = 220; m.loop = false; m.duration = .2f; m.gravityModifier = .3f;
            var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0, 160) }); var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = .6f; burst.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat; ps.Play();
            if (b) b.Track(gameObject); Destroy(gameObject, Duration + .8f);
        }
        LineRenderer Ring(Material mat, float width)
        {
            var go = new GameObject("Anillo"); go.transform.SetParent(transform, false); var r = go.AddComponent<LineRenderer>(); r.loop = true; r.positionCount = 64; r.useWorldSpace = false; r.sharedMaterial = mat; r.widthMultiplier = width;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.alignment = LineAlignment.TransformZ; go.transform.localRotation = Quaternion.Euler(90, 0, 0); return r;
        }
        void Update()
        {
            age += Time.deltaTime; float k = Mathf.Clamp01(age / Duration); float radius = Mathf.Lerp(.5f, MaxRadius, 1 - (1 - k) * (1 - k)); float fade = 1 - k;
            for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; var p = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0); ring.SetPosition(i, p); glow.SetPosition(i, p); }
            ring.widthMultiplier = .35f * fade + .05f; glow.widthMultiplier = 1.1f * fade; l.intensity = 6 * fade;
            if (hitDone || k >= 1 || !target || !target.State.IsAlive || Codice.Multiplayer.LanSession.Client) return;
            var d = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up).magnitude;
            if (Mathf.Abs(d - radius) < .7f && target.Grounded && Mathf.Abs(target.transform.position.y - transform.position.y) < 1.2f)
            { hitDone = true; target.ReceiveDamage(new DamageInfo { amount = Damage, source = boss ? boss.gameObject : gameObject, direction = (target.transform.position - transform.position).normalized, secondary = true }); }
        }
    }
}
