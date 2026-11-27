using UnityEngine;
using UnityEngine.AI;
using Codice.Characters;
using Codice.Presentation;
namespace Codice.Bosses
{
    // Berserker Orco · jefe final (5-3). Repertorio propio con zonas de daño rellenas que se llenan hasta el impacto:
    // Tajo amplio, Combo de dos tajos, Golpe sísmico con grieta, Salto aplastante, Embestida (se aturde si choca con
    // un muro), Grito de guerra (fase 2+) y Torbellino (fase 3, enfurecido con aura de brasas).
    public sealed partial class BossCharacter
    {
        enum Rage { None, Sweep, Combo, Slam, Leap, Charge, Cry, Spin }
        Rage rage; float rageTimer, rageTotal, spinTick; int comboStep; Vector3 leapFrom, leapTo; GameObject rageWarning; bool rageHit; Light enrageLight; ParticleSystem enrageEmbers;
        static readonly Color Ember = new Color(1f, .38f, .1f), Blood = new Color(.95f, .12f, .08f);
        float RageScale => Codice.Multiplayer.CoopScaling.EnemyDamage;
        void TickBerserker(float dt)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            if (State == BossState.Stunned) { State = BossState.Recovery; attackTimer = .6f; }
            UpdateEnrage();
            var pres = GetComponent<ActorPresentation>();
            if (rage != Rage.None) { TickRageAction(dt, pres); return; }
            if (State == BossState.Recovery) { agent.isStopped = true; attackTimer -= dt; if (attackTimer <= 0) State = BossState.Idle; return; }
            Vector3 delta = Vector3.ProjectOnPlane(Target.transform.position - transform.position, Vector3.up); float d = delta.magnitude;
            if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), (Phase == 3 ? 420 : 300) * dt);
            attackTimer -= dt; repath -= dt;
            agent.speed = (Phase == 3 ? 5.8f : 4.6f) * SpeedMultiplier; agent.acceleration = 22;
            if (attackTimer <= 0 && HasLineOfSight(Target.transform.position + Vector3.up))
            {
                facing = delta.sqrMagnitude > .01f ? delta.normalized : transform.forward; aim = Target.transform.position; sequence++;
                Rage pick;
                if (Phase >= 3 && sequence % 4 == 0) pick = Rage.Spin;
                else if (Phase >= 2 && sequence % 5 == 0) pick = Rage.Cry;
                else if (d > 7 && d < 18) pick = sequence % 2 == 0 ? Rage.Leap : Rage.Charge;
                else if (d <= 4.6f) pick = Phase >= 2 && sequence % 3 == 0 ? Rage.Combo : sequence % 2 == 0 ? Rage.Slam : Rage.Sweep;
                else pick = Rage.None;
                if (pick != Rage.None) { BeginRage(pick, pres); return; }
            }
            agent.isStopped = d < 3.2f; State = agent.isStopped ? BossState.Idle : BossState.Moving;
            if (!agent.isStopped && repath <= 0) { repath = .25f; agent.SetDestination(Target.transform.position); }
        }
        void BeginRage(Rage r, ActorPresentation pres)
        {
            rage = r; rageHit = false; comboStep = 0; agent.isStopped = true; State = BossState.Windup; ClearRageWarning();
            float speed = Phase == 3 ? .8f : 1f;
            switch (r)
            {
                case Rage.Sweep: rageTotal = .75f * speed; rageWarning = BerserkerZone.Sector(transform.position, facing, 4.4f, 150, Ember, rageTotal); pres?.Play("Melee", rageTotal + .55f, true); break;
                case Rage.Combo: rageTotal = .55f * speed; rageWarning = BerserkerZone.Sector(transform.position, Quaternion.Euler(0, -35, 0) * facing, 4.2f, 120, Ember, rageTotal); pres?.Play("Melee", rageTotal + .3f, true); break;
                case Rage.Slam: rageTotal = 1.05f * speed; leapTo = transform.position + facing * 2.6f; rageWarning = BerserkerZone.Disc(leapTo, 3.3f, Blood, rageTotal); pres?.Play("Slam", rageTotal + .9f, true); break;
                case Rage.Leap:
                    rageTotal = 1.0f * speed; leapFrom = transform.position;
                    leapTo = NavMesh.SamplePosition(Target.transform.position, out var hit, 3, agent.areaMask) ? hit.position : Target.transform.position;
                    rageWarning = BerserkerZone.Disc(leapTo, 4.2f, Blood, rageTotal + .7f); pres?.Play("Slam", rageTotal + 1.4f, true); break;
                case Rage.Charge: rageTotal = .8f * speed; rageWarning = BerserkerZone.Lane(transform.position, facing, 16, 2.2f, Ember, rageTotal); pres?.Play("Idle", rageTotal, true); break;
                case Rage.Cry: rageTotal = 1.1f; rageWarning = BerserkerZone.Disc(transform.position, 6.5f, new Color(1f, .7f, .2f), rageTotal); pres?.Play("Summon", rageTotal + .5f, true); break;
                case Rage.Spin: rageTotal = .7f; rageWarning = BerserkerZone.Disc(transform.position, 3.2f, Blood, rageTotal); pres?.Play("Melee", rageTotal, true); break;
            }
            if (rageWarning) Track(rageWarning);
            rageTimer = rageTotal; Debug.Log($"[Berserker] {r} phase={Phase}");
        }
        void TickRageAction(float dt, ActorPresentation pres)
        {
            rageTimer -= dt;
            switch (rage)
            {
                case Rage.Leap:
                    if (rageTimer > 0) return;
                    if (State == BossState.Windup) { State = BossState.Charging; rageTimer = .7f; return; }
                    {   // airborne arc toward the marked circle
                        float t = 1 - Mathf.Clamp01(rageTimer / .7f);
                        var flat = Vector3.Lerp(leapFrom, leapTo, t);
                        agent.Move(flat - transform.position);
                        if (visual) visual.localPosition = visualRestPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * 4.2f;
                        if (rageTimer > 0) return;
                        if (visual) visual.localPosition = visualRestPosition;
                        Shockwave(leapTo, 4.2f, 55, true);
                        if (Phase >= 2) for (int i = 0; i < 5; i++) BerserkerAftershock.Create(this, leapTo + Quaternion.Euler(0, i * 72 + sequence * 17, 0) * Vector3.forward * 5.5f, .45f + i * .12f);
                        EndRage(1.1f); return;
                    }
                case Rage.Charge:
                    if (State == BossState.Windup) { if (rageTimer > 0) return; State = BossState.Charging; rageTimer = 1.0f; ClearRageWarning(); pres?.Play("Run", 1, true); return; }
                    {
                        var step = facing * (15 * dt); var to = transform.position + step;
                        if (NavMesh.Raycast(transform.position, to, out var wall, agent.areaMask))
                        {   // slamming into a palisade leaves him dazed: a punish window
                            agent.Move(wall.position - transform.position); ImpactVfx.Spawn("VFX_ImpactClassic01", transform.position + Vector3.up + facing, -facing, 1.6f);
                            LordsAttackVfx.ImpactTinted(transform.position + Vector3.up * 1.2f + facing, -facing, Ember, 2f); rage = Rage.None; CurrentAttack = BossAttack.None; ApplyStun(1.8f); return;
                        }
                        agent.Move(step);
                        var delta = Vector3.ProjectOnPlane(Target.transform.position - transform.position, Vector3.up);
                        if (!rageHit && delta.magnitude < 2.6f && Vector3.Dot(facing, delta.normalized) > -.2f) { rageHit = true; Hurt(42, facing); LordsAttackVfx.ImpactTinted(Target.transform.position + Vector3.up, facing, Ember, 1.6f); }
                        if (rageTimer <= 0) EndRage(.7f);
                        return;
                    }
                case Rage.Spin:
                    if (State == BossState.Windup) { if (rageTimer > 0) return; State = BossState.Charging; rageTimer = 2.6f; ClearRageWarning(); rageWarning = BerserkerZone.Disc(transform.position, 3.2f, Blood, 0); Track(rageWarning); return; }
                    {
                        var delta = Vector3.ProjectOnPlane(Target.transform.position - transform.position, Vector3.up);
                        agent.Move(delta.normalized * Mathf.Min(delta.magnitude, 2.6f * dt));
                        transform.Rotate(0, 900 * dt, 0); if (rageWarning) rageWarning.transform.position = transform.position + Vector3.up * .06f;
                        spinTick -= dt; if (spinTick <= 0) { spinTick = .4f; pres?.Play("Melee", .4f, true); LordsAttackVfx.SlashTinted(transform.position + Vector3.up * 1.4f, transform.forward, Blood, 2.2f, Random.Range(-40, 40)); if (delta.magnitude < 3.2f) Hurt(14, delta.normalized); }
                        if (rageTimer <= 0) { EndRage(1.4f); if (pres) pres.Play("Hit", .8f, true); }
                        return;
                    }
            }
            if (rageTimer > 0) return;
            // impact frame for the grounded attacks
            switch (rage)
            {
                case Rage.Sweep: Cleave(4.4f, 150, 36, facing); EndRage(.75f); break;
                case Rage.Combo:
                    if (comboStep == 0) { Cleave(4.2f, 120, 30, Quaternion.Euler(0, -35, 0) * facing); comboStep = 1; facing = Quaternion.Euler(0, 35, 0) * facing; rageTimer = .42f; ClearRageWarning(); rageWarning = BerserkerZone.Sector(transform.position, Quaternion.Euler(0, 35, 0) * facing, 4.2f, 120, Blood, .42f); Track(rageWarning); pres?.Play("Melee", .8f, true); return; }
                    Cleave(4.2f, 120, 34, facing); EndRage(.9f); break;
                case Rage.Slam:
                    Shockwave(leapTo, 3.3f, 48, false);
                    for (int i = 1; i <= 4; i++) BerserkerAftershock.Create(this, leapTo + facing * (i * 2.4f), .18f * i, 1.3f);
                    EndRage(1.1f); break;
                case Rage.Cry:
                    Shockwave(transform.position, 6.5f, 18, false); enragedUntil = Time.time + 9; Codice.UI.RunHUD.Instance?.Announce("¡EL BERSERKER RUGE! · Se mueve y golpea más rápido");
                    EndRage(.5f); break;
            }
        }
        float enragedUntil;
        void UpdateEnrage()
        {
            bool on = Phase == 3 || Time.time < enragedUntil;
            if (on && !enrageLight)
            {
                enrageLight = new GameObject("Berserker · ira").AddComponent<Light>(); enrageLight.transform.SetParent(transform, false); enrageLight.transform.localPosition = Vector3.up * 2;
                enrageLight.type = LightType.Point; enrageLight.color = Blood; enrageLight.range = 7; enrageLight.intensity = 2.5f; enrageLight.shadows = LightShadows.None;
                enrageEmbers = BerserkerZone.Embers(transform, Ember);
            }
            if (enrageLight) { enrageLight.enabled = on; enrageLight.intensity = 2 + Mathf.Sin(Time.time * 9) * .8f; }
            if (enrageEmbers) { var e = enrageEmbers.emission; e.enabled = on; }
            if (on) agent.speed *= 1.15f;
        }
        void EndRage(float recovery) { rage = Rage.None; ClearRageWarning(); CurrentAttack = BossAttack.None; State = BossState.Recovery; attackTimer = recovery * (Phase == 3 || Time.time < enragedUntil ? .7f : 1); if (visual) visual.localPosition = visualRestPosition; }
        void ClearRageWarning() { if (rageWarning) Destroy(rageWarning); rageWarning = null; }
        void Hurt(float amount, Vector3 direction) { if (Target && Target.State.IsAlive && Mathf.Abs(Target.transform.position.y-transform.position.y)<1.5f && HasLineOfSight(Target.transform.position+Vector3.up)) Target.ReceiveDamage(new DamageInfo { amount = amount * RageScale * (Time.time < enragedUntil ? 1.15f : 1), source = gameObject, direction = direction, melee = true }); }
        void Cleave(float radius, float degrees, float damage, Vector3 dir)
        {
            var delta = Vector3.ProjectOnPlane(Target.transform.position - transform.position, Vector3.up);
            if (delta.magnitude <= radius && Vector3.Angle(dir, delta) <= degrees * .5f && HasLineOfSight(Target.transform.position + Vector3.up)) Hurt(damage, dir);
            LordsAttackVfx.SlashTinted(transform.position + Vector3.up * 1.4f + dir * 1.2f, dir, Ember, 2.6f, -20, .35f); HitStop.Do(.04f);
        }
        void Shockwave(Vector3 center, float radius, float damage, bool heavy)
        {
            if (Vector3.ProjectOnPlane(Target.transform.position - center, Vector3.up).magnitude <= radius) Hurt(damage, (Target.transform.position - center).normalized);
            ImpactVfx.Spawn("VFX_Critical_01", center + Vector3.up * .2f, Vector3.up, radius * .45f, 1.5f);
            var fx = ImportedAttackVfx.Burst(center + Vector3.up * .2f, true, radius * .3f); if (fx) Track(fx);
            Codice.Mechanics.RelicPulse.Spawn(center + Vector3.up * .1f, radius, heavy ? Blood : Ember);
            if (heavy) HitStop.Do(.06f);
        }
    }
    /// Filled, translucent damage zones that fill toward the edge as the impact approaches.
    public sealed class BerserkerZone : MonoBehaviour
    {
        MeshRenderer fill; float total, age; Material mat; Color tint;
        static Material Make(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; m.SetColor("_BaseColor", c); return m;
        }
        static Mesh Fan(float radius, float degrees, int seg)
        {
            var v = new Vector3[seg + 2]; var t = new int[seg * 3]; v[0] = Vector3.zero;
            for (int i = 0; i <= seg; i++) { float a = (-degrees / 2 + degrees * i / seg) * Mathf.Deg2Rad; v[i + 1] = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius; }
            for (int i = 0; i < seg; i++) { t[i * 3] = 0; t[i * 3 + 1] = i + 1; t[i * 3 + 2] = i + 2; }
            var m = new Mesh(); m.vertices = v; m.triangles = t; m.RecalculateNormals(); return m;
        }
        static GameObject Build(string name, Vector3 at, Quaternion rot, Mesh shape, Color c, float total, GameObject outline)
        {
            var go = new GameObject("Berserker · zona " + name); go.transform.SetPositionAndRotation(at + Vector3.up * .06f, rot);
            var z = go.AddComponent<BerserkerZone>(); z.total = total; z.tint = c;
            var under = new GameObject("Zona"); under.transform.SetParent(go.transform, false); under.AddComponent<MeshFilter>().sharedMesh = shape; var r = under.AddComponent<MeshRenderer>(); r.sharedMaterial = Make(new Color(c.r, c.g, c.b, .16f)); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var grow = new GameObject("Llenado"); grow.transform.SetParent(go.transform, false); grow.transform.localPosition = Vector3.up * .01f; grow.AddComponent<MeshFilter>().sharedMesh = shape; z.fill = grow.AddComponent<MeshRenderer>(); z.mat = Make(new Color(c.r, c.g, c.b, .32f)); z.fill.sharedMaterial = z.mat; z.fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            grow.transform.localScale = total > 0 ? Vector3.zero : Vector3.one;
            if (outline) outline.transform.SetParent(go.transform, true);
            return go;
        }
        public static GameObject Disc(Vector3 c, float r, Color tint, float total) => Build("círculo", c, Quaternion.identity, Fan(r, 360, 48), tint, total, BloodVisual.Ring(c + Vector3.up * .08f, r, tint, .07f));
        public static GameObject Sector(Vector3 c, Vector3 facing, float r, float deg, Color tint, float total) => Build("tajo", c, Quaternion.LookRotation(facing), Fan(r, deg, 32), tint, total, BloodVisual.Arc(c + Vector3.up * .08f, r, facing, deg, tint));
        public static GameObject Lane(Vector3 c, Vector3 facing, float length, float width, Color tint, float total)
        {
            var m = new Mesh(); float w = width * .5f; m.vertices = new[] { new Vector3(-w, 0, 0), new Vector3(w, 0, 0), new Vector3(-w, 0, length), new Vector3(w, 0, length) }; m.triangles = new[] { 0, 2, 1, 1, 2, 3 }; m.RecalculateNormals();
            return Build("embestida", c, Quaternion.LookRotation(facing), m, tint, total, BloodVisual.Beam(c + Vector3.up * .1f, c + facing * length + Vector3.up * .1f, tint, .06f));
        }
        void Update()
        {
            if (total <= 0) { if (mat) mat.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, .22f + Mathf.Sin(Time.time * 14) * .08f)); return; }
            age += Time.deltaTime; float k = Mathf.Clamp01(age / total); fill.transform.localScale = new Vector3(k, 1, k);
            if (mat) mat.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, .25f + .35f * k * k));
        }
        void OnDestroy() { foreach (var r in GetComponentsInChildren<Renderer>()) if (r.sharedMaterial && r.sharedMaterial.name.Length == 0) Destroy(r.sharedMaterial); }
        public static ParticleSystem Embers(Transform parent, Color color)
        {
            var go = new GameObject("Brasas de ira"); go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.up * 1.2f;
            var ps = go.AddComponent<ParticleSystem>(); var main = ps.main; main.startLifetime = 1.1f; main.startSpeed = 1.6f; main.startSize = .12f; main.startColor = color; main.maxParticles = 80; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 45; var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1.1f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f); vel.y = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Make(color); return ps;
        }
    }
    /// Delayed fissure eruption marked by a small filled circle (Slam crack line / Leap aftershocks).
    public sealed class BerserkerAftershock : MonoBehaviour
    {
        BossCharacter owner; float remaining, radius;
        public static void Create(BossCharacter boss, Vector3 point, float delay, float radius = 1.6f)
        {
            if (NavMesh.SamplePosition(point, out var hit, 2, NavMesh.AllAreas)) point = hit.position;
            var go = BerserkerZone.Disc(point, radius, new Color(1f, .3f, .08f), delay); var a = go.AddComponent<BerserkerAftershock>(); a.owner = boss; a.remaining = delay; a.radius = radius; boss.Track(go);
        }
        void Update()
        {
            if (!owner || !owner.IsAlive) { Destroy(gameObject); return; }
            if (owner.Paused) return; remaining -= Time.deltaTime; if (remaining > 0) return;
            var t = owner.Target; var p = transform.position;
            if (t && t.State.IsAlive && Vector3.ProjectOnPlane(t.transform.position - p, Vector3.up).magnitude <= radius) t.ReceiveDamage(new DamageInfo { amount = 22 * Codice.Multiplayer.CoopScaling.EnemyDamage, source = owner.gameObject, direction = Vector3.up });
            var fx = ImportedAttackVfx.Burst(p + Vector3.up * .2f, true, .7f); if (fx) owner.Track(fx); Codice.Mechanics.RelicPulse.Spawn(p + Vector3.up * .1f, radius, new Color(1f, .45f, .1f)); Destroy(gameObject);
        }
    }
}
