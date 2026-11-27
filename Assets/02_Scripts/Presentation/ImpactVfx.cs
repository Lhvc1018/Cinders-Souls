using UnityEngine;
using Codice.Characters;
namespace Codice.Presentation
{
    // WALLCOEUR impact pack (URP copies in Resources/CindersVfx/Impacts). Visual only.
    public static class ImpactVfx
    {
        static float nextSmall;
        public static GameObject Spawn(string name, Vector3 point, Vector3 facing, float scale, float life = 1.2f)
        {
            if (Codice.Multiplayer.LanSession.Host) Codice.Multiplayer.LanSession.Instance.EmitFx("IV:" + name, point, facing, scale, life, Color.white);
            var prefab = Resources.Load<GameObject>("CindersVfx/Impacts/" + name);
            if (!prefab) return null;
            var rot = facing.sqrMagnitude > .001f ? Quaternion.LookRotation(facing) : Quaternion.identity;
            var go = Object.Instantiate(prefab, point, rot);
            go.transform.localScale *= scale; go.name = "Impact • " + name;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) { var m = ps.main; m.loop = false; m.scalingMode = ParticleSystemScalingMode.Hierarchy; ps.Play(); }
            Object.Destroy(go, life);
            return go;
        }
        // Chooses the impact from what the hit actually did, so the reader learns the result at a glance.
        public static void Damage(CombatFeedback.Result r)
        {
            var cam = Camera.main;
            var toCamera = cam ? cam.transform.position - r.position : -r.hit.direction;
            bool playerHurt = r.target is PlayerCharacter;
            if (r.blocked) { Spawn("VFX_ImpactCross", r.position, toCamera, .32f); return; }
            if (playerHurt) { Spawn("VFX_Blood_02", r.position, toCamera, .22f); return; }
            if (r.hit.critical) { Spawn("VFX_Classic_03", r.position, toCamera, .55f); Spawn("VFX_Blood_02", r.position, r.hit.direction, .3f); return; }
            if (r.hit.element == Element.Poison) { Spawn("VFX_Poison_01", r.position, toCamera, .45f); return; }
            if (r.hit.breaksGuard) { Spawn("VFX_ImpactCross", r.position, toCamera, .42f); Spawn("VFX_Blood_01", r.position, r.hit.direction, .4f); return; }
            if (r.hit.melee) { Spawn("VFX_ImpactClassic01", r.position, toCamera, .36f); Spawn("VFX_Blood_01", r.position, r.hit.direction, .32f); return; }
            // Projectiles and secondary ticks get a small star, rate-limited for damage-over-time.
            if (r.hit.secondary && Time.time < nextSmall) return;
            nextSmall = Time.time + .15f;
            Spawn("VFX_ImpactClassic01", r.position, toCamera, .24f, .8f);
        }
        public static void Parry(Vector3 point, Vector3 toward)
        {
            Spawn("VFX_ImpactClassic01", point, toward, .5f);
            Spawn("VFX_Classic_01", point, toward, .35f);
        }
        public static void Slam(Vector3 point) => Spawn("VFX_Classic_01", point + Vector3.up * .15f, Vector3.up, .6f, 1.4f);
    }
}
