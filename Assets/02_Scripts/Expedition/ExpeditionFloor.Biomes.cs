using System.Collections.Generic;
using Codice.Characters;
using Codice.Enemies;
using UnityEngine;
namespace Codice.Expedition
{
    /// One signature mechanic per biome (data in Signature):
    ///  W1 Mazmorra (1-3): spike floor traps · W2 Cloacas (4-6): sewer gas vents (poison) · W3 Cementerio (7-9): terraces,
    ///  stairs and ash pools · W4 Forja (10-12): flame vents · W5 Campamento (13-15): war drums that enrage the horde.
    public sealed partial class ExpeditionFloor
    {
        /// Rooms whose war drum is broken (host -> guests).
        public readonly HashSet<int> BrokenDrums = new HashSet<int>();
        public void ApplyCoopDrum(int room) { if (!BrokenDrums.Add(room)) return; foreach (var d in Object.FindObjectsByType<WarDrum>(FindObjectsSortMode.None)) if (d.Room == room) d.BreakRemote(); }
        public static string Signature(int floor) => floor <= 3 ? "Trampas de pinchos" : floor <= 6 ? "Gas de las cloacas" : floor <= 9 ? "Bancales y ceniza tóxica" : floor <= 12 ? "Respiraderos de la forja" : "Tambores de guerra";
        void BuildBiomeMechanic(Transform root)
        {
            for (int room = 1; room < RoomCount; room++)
            {
                if (room == Layout.Boss || room == Layout.MerchantRoom || room == Layout.StatueRoom || room == Layout.SecretRoom || IsVault(room)) continue;
                var plan = Layout.Plans[room]; var c = Center(room); var rng = new System.Random(Seed ^ room * 6151 ^ 0xB10);
                if (FloorNumber >= 4 && FloorNumber <= 6)
                {
                    // Two gas vents off the door axes.
                    for (int n = 0, tries = 0; n < 2 && tries < 20; tries++)
                    {
                        int x = rng.Next(-(plan.width - 1), plan.width), z = rng.Next(-(plan.depth - 1), plan.depth);
                        if (x == 0 || z == 0 || !plan.Contains(x, z)) continue;
                        SewerGas.Create(this, room, c + new Vector3(x * 4, 0, z * 4), root, (float)rng.NextDouble() * 5); n++;
                    }
                }
                else if (FloorNumber >= 13 && plan.width >= 3 && plan.depth >= 2)
                {
                    int side = rng.Next(2) == 0 ? -1 : 1;
                    WarDrum.Create(this, room, c + new Vector3(side * (plan.width * 4 - 4), 0, (rng.Next(2) == 0 ? -1 : 1) * (plan.depth * 4 - 4)), root);
                }
            }
        }
    }
    /// Sewer gas (World 2): a grate that periodically breathes a green cloud; standing in it builds up poison.
    public sealed class SewerGas : MonoBehaviour
    {
        ExpeditionFloor floor; int room; float clock, tick; ParticleSystem cloud; Light glow; const float Radius = 2.2f;
        public static void Create(ExpeditionFloor floor, int room, Vector3 at, Transform root, float offset)
        {
            var go = new GameObject("Gas de las cloacas"); go.transform.SetParent(root); go.transform.position = at;
            var g = go.AddComponent<SewerGas>(); g.floor = floor; g.room = room; g.clock = offset;
            var grate = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(grate.GetComponent<Collider>()); grate.transform.SetParent(go.transform, false);
            grate.transform.localPosition = Vector3.up * .03f; grate.transform.localScale = new Vector3(1.6f, .03f, 1.6f);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", new Color(.07f, .09f, .05f)); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.08f, .25f, .04f)); grate.GetComponent<Renderer>().sharedMaterial = m;
            var fx = new GameObject("Nube"); fx.transform.SetParent(go.transform, false); fx.transform.localPosition = Vector3.up * .2f;
            g.cloud = fx.AddComponent<ParticleSystem>(); var main = g.cloud.main; main.startLifetime = 2.2f; main.startSpeed = .9f; main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.8f); main.startColor = new Color(.45f, .75f, .25f, .35f); main.maxParticles = 120; main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f); main.playOnAwake = false;
            var em = g.cloud.emission; em.rateOverTime = 26; var sh = g.cloud.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.2f; fx.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            g.cloud.GetComponent<ParticleSystemRenderer>().sharedMaterial = Codice.Presentation.SoftParticle.Make(new Color(.5f, .8f, .3f, .3f), false); g.cloud.Stop();
            g.glow = go.AddComponent<Light>(); g.glow.color = new Color(.4f, 1f, .3f); g.glow.range = 5; g.glow.intensity = 0; g.glow.shadows = LightShadows.None;
        }
        void Update()
        {
            if (!floor || !floor.context || !floor.context.player) return;
            bool here = room < 0 ? (floor.context.player.transform.position - transform.position).sqrMagnitude < 196 : floor.Layout.RoomAt(floor.context.player.transform.position) == room;
            if (!here) { if (cloud.isPlaying) cloud.Stop(); glow.intensity = 0; return; }
            clock += Time.deltaTime; float cycle = clock % 7f;   // 4 s quiet · 0.8 s hiss · 2.2 s cloud
            if (cycle < 4f) { if (cloud.isPlaying) cloud.Stop(); glow.intensity = .25f; return; }
            if (cycle < 4.8f) { glow.intensity = 1.2f; if (!cloud.isPlaying) { cloud.Play(); Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.SpellPoison, transform.position); } return; }
            glow.intensity = 1.8f; tick -= Time.deltaTime; if (tick > 0 || Codice.Multiplayer.LanSession.Client) return; tick = .5f;
            var seen = new HashSet<PlayerCharacter>();
            foreach (var hit in Physics.OverlapSphere(transform.position + Vector3.up, Radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var p = hit.GetComponentInParent<PlayerCharacter>(); if (!p || !seen.Add(p)) continue;
                (p.GetComponent<ElementalStatus>() ?? p.gameObject.AddComponent<ElementalStatus>()).Add(Element.Poison, 14, gameObject);
            }
        }
    }
    /// War drum (World 5): while it stands, the horde in its room moves and strikes 30 % faster. Break it (3-4 hits).
    public sealed class WarDrum : MonoBehaviour, IDamageReceiver
    {
        ExpeditionFloor floor; int room; float pulse, scanAt; float health = 60; GameObject model; Light glow; readonly List<BasicEnemy> hasted = new List<BasicEnemy>();
        public static void Create(ExpeditionFloor floor, int room, Vector3 at, Transform root)
        {
            var asset = Resources.Load<GameObject>("WorldFive/Drum"); if (!asset) return;
            var go = new GameObject("Tambor de guerra"); go.transform.SetParent(root); go.transform.position = at;
            var d = go.AddComponent<WarDrum>(); d.floor = floor; d.room = room;
            d.model = Object.Instantiate(asset, at, Quaternion.identity, go.transform); d.model.transform.localScale *= 1.35f;
            var col = go.AddComponent<CapsuleCollider>(); col.center = Vector3.up * .8f; col.radius = .8f; col.height = 1.8f;
            d.glow = go.AddComponent<Light>(); d.glow.color = new Color(1f, .3f, .1f); d.glow.range = 6; d.glow.intensity = 0; d.glow.shadows = LightShadows.None;
        }
        public void ReceiveDamage(DamageInfo hit)
        {
            if (health <= 0 || hit.amount <= 0 || float.IsNaN(hit.amount) || !hit.source || !hit.source.GetComponentInParent<PlayerCharacter>()) return;
            health -= hit.amount; Codice.Mechanics.RelicPulse.Spawn(transform.position + Vector3.up, .5f, new Color(.6f, .25f, .1f));
            if (health > 0) return;
            floor.BrokenDrums.Add(room); Release(); Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Heavy, transform.position);
            Codice.UI.RunHUD.Instance?.Announce("TAMBOR DE GUERRA ROTO · la horda pierde su frenesí");
            Destroy(gameObject);
        }
        void Update()
        {
            if (!floor || health <= 0) return;
            bool fighting = floor.InEncounter && floor.CurrentRoom == room;
            if (!fighting) { Release(); glow.intensity = 0; return; }
            pulse += Time.deltaTime; float beat = Mathf.Repeat(pulse, .6f);
            glow.intensity = beat < .12f ? 3f : .6f; if (model) model.transform.localScale = Vector3.one * 1.35f * (beat < .12f ? 1.05f : 1);
            if (Mathf.Repeat(pulse, 1.2f) < Time.deltaTime) Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Heavy, transform.position);
            if (Time.time < scanAt) return; scanAt = Time.time + .5f;   // new wave members join the frenzy
            foreach (var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))
                if (e.IsAlive && floor.Layout.RoomAt(e.transform.position) == room && !hasted.Contains(e)) { e.haste = 1.3f; hasted.Add(e); }
        }
        public int Room => room;
        /// Broken on the host: guests drop their copy with the same feedback.
        public void BreakRemote() { if (health <= 0) return; health = 0; Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Heavy, transform.position); Destroy(gameObject); }
        void Release() { foreach (var e in hasted) if (e) e.haste = 1; hasted.Clear(); }
        void OnDestroy() => Release();
    }
}
