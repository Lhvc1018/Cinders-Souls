using System.Collections.Generic;
using System.Linq;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;
namespace Codice.Expedition
{
    // Depth of the descent: hazards in combat rooms, a hidden vault behind a brazier puzzle (treasure or a secret
    // boss), and Ash Boon altars after cleared rooms.
    public sealed partial class ExpeditionFloor
    {
        public bool IsVault(int room) => Layout != null && room >= 0 && room == Layout.VaultRoom;
        public bool IsVaultBoss(int room) => IsVault(room) && ExpeditionLayout.VaultBossFloor(FloorNumber);
        bool IsVaultTreasure(int room) => IsVault(room) && !ExpeditionLayout.VaultBossFloor(FloorNumber);
        public bool VaultOpen => vaultPuzzle && vaultPuzzle.Solved;
        VaultPuzzle vaultPuzzle; Codice.Bosses.BossCharacter floorBossPrefab;
        static string SecretBossFor(int floor) => floor <= 3 ? "Eco del Ángel Caído" : floor <= 6 ? "Inquisidor Olvidado" : floor <= 9 ? "Sepulturero sin Nombre" : floor <= 12 ? "Custodio Hueco" : "Matriarca del Foso";
        string VaultTitle(int room) => IsVaultBoss(room) ? "Cámara oculta · " + SecretBossFor(FloorNumber) : "Cámara oculta · Relicario sellado";
        // ---------------------------------------------------------------- hazards
        void BuildHazards(Transform root)
        {
            if (FloorNumber < 2) return;
            for (int room = 1; room < RoomCount; room++)
            {
                if (room == Layout.Boss || room == Layout.MerchantRoom || room == Layout.StatueRoom || room == Layout.SecretRoom || IsVault(room) || IsMemoryEncounter(room)) continue;
                var plan = Layout.Plans[room]; var c = Center(room); var rng = new System.Random(Seed ^ room * 9173 ^ 0x4A2);
                // Biome identity: flame vents belong to the Forge (W4), ash pools to the Cemetery (W3). See ExpeditionFloor.Biomes.
                int vents = FloorNumber >= 10 && FloorNumber <= 12 ? Mathf.Clamp(1 + FloorNumber / 5, 1, 3) : 0, pools = FloorNumber >= 7 && FloorNumber <= 9 ? 2 : 0, placed = 0;
                if (vents + pools == 0) continue;
                for (int attempt = 0; attempt < 30 && placed < vents + pools; attempt++)
                {
                    int x = rng.Next(-(plan.width - 1), plan.width), z = rng.Next(-(plan.depth - 1), plan.depth);
                    if (x == 0 || z == 0 || !plan.Contains(x, z)) continue;   // door axes stay clear
                    var at = c + new Vector3(x * 4 + (float)rng.NextDouble() - .5f, 0, z * 4 + (float)rng.NextDouble() - .5f);
                    Physics.SyncTransforms();
                    if (Physics.OverlapBox(at + Vector3.up * 1.2f, new Vector3(1.4f, 1f, 1.4f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(h => h.bounds.max.y > at.y + .4f && h.bounds.min.y > at.y - .3f)) continue;
                    if (placed < vents) FlameVent.Create(this, room, at, root, (float)rng.NextDouble() * 3); else AshPool.Create(this, room, at, root);
                    placed++;
                }
            }
        }
        // ---------------------------------------------------------------- vault
        void BuildVault(Transform root)
        {
            if (Layout.VaultRoom < 0) return;
            int k = Layout.Links.FindIndex(e => e.y == Layout.VaultRoom); var path = Layout.Corridors[k]; int host = Layout.VaultHost;
            for (int j = 1; j < path.Count; j++)
            {
                if (Layout.RoomAt(new Vector3(path[j - 1].x * 4, 0, path[j - 1].y * 4)) != host || Layout.RoomAt(new Vector3(path[j].x * 4, 0, path[j].y * 4)) == host) continue;
                var sum = path[j - 1] + path[j]; var delta = path[j] - path[j - 1];
                var doorAt = new Vector3(sum.x * 2, Layout.Height(host), sum.y * 2);
                vaultPuzzle = VaultPuzzle.Create(this, doorAt, delta.x != 0 ? 90 : 0, Center(host), root, Seed ^ 0x5EC2, FloorNumber >= 7 ? 4 : 3);
                break;
            }
            var v = Center(Layout.VaultRoom);
            if (IsVaultTreasure(Layout.VaultRoom)) { Glow(v + Vector3.up * 3, new Color(.95f, .75f, .35f), 3.5f, 14, root); }
            else { Glow(v + Vector3.up * 5, new Color(.55f, .4f, .95f), 3.5f, 22, root); }
        }
        void OpenVaultTreasure(int room)
        {
            var c = Center(room);
            SpawnChest(room, true, false, 3);
            AshBoonAltar.Create(c + new Vector3(-3.5f, 0, 2), Seed ^ room * 31, transform, context.player);
            context.player.State.AddGold(120); director.SetExpeditionPhase(Codice.Mechanics.RunPhase.Cleared, "RELICARIO SELLADO · Una reliquia, una bendición y 120 de oro");
        }
        bool BeginVaultBoss()
        {
            var prefab = Resources.Load<Codice.Bosses.BossCharacter>("Secrets/Bosses/" + SecretBossFor(FloorNumber));
            if (!prefab) { Debug.LogError("[Vault] Missing secret boss " + SecretBossFor(FloorNumber)); return false; }
            floorBossPrefab = context.arena.expeditionPrefab; context.arena.expeditionPrefab = prefab;
            bool ok = context.arena.BeginEncounter(5);
            if (!ok) { context.arena.expeditionPrefab = floorBossPrefab; floorBossPrefab = null; }
            else Codice.UI.RunHUD.Instance?.Announce("JEFE SECRETO · " + SecretBossFor(FloorNumber));
            return ok;
        }
        void FinishVaultBoss()
        {
            if (floorBossPrefab) { context.arena.expeditionPrefab = floorBossPrefab; floorBossPrefab = null; }
            var c = Center(CurrentRoom);
            SpawnChest(CurrentRoom, true, true, 4);
            AshBoonAltar.Create(c + new Vector3(-4, 0, 3), Seed ^ 0xB055, transform, context.player);
            foreach (var p in Team()) p.State.AddGold(300);
            director.SetExpeditionPhase(Codice.Mechanics.RunPhase.Cleared, SecretBossFor(FloorNumber).ToUpperInvariant() + " HA CAÍDO · Reliquia activa, bendición y 300 de oro");
        }
        IEnumerable<PlayerCharacter> Team() => Codice.Multiplayer.LanSession.Host ? Codice.Multiplayer.LanSession.Instance.Players() : new[] { context.player };
        // ---------------------------------------------------------------- boons after rooms
        void TryBoonAltar(int room)
        {
            var rng = new System.Random(Seed ^ room * 7717 ^ 0xA5);
            if (rng.Next(100) >= 45) return;
            var c = Center(room); var at = c + new Vector3(-3, 0, 3);
            if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) at = hit.position;
            AshBoonAltar.Create(at, Seed ^ room * 131, transform, context.player);
            Codice.UI.RunHUD.Instance?.Announce("Un altar de ascuas emerge · Elige una bendición");
        }
        public void ApplyCoopPuzzle(int mask, bool solved) { if (vaultPuzzle) vaultPuzzle.ApplyNetwork(mask, solved); }
        public VaultPuzzle Puzzle => vaultPuzzle;
    }
    /// Brazier ordering puzzle: each brazier shows a number of skull offerings; light them from fewest to most.
    /// A wrong flame snuffs them all with a burst of fire. Solving it lifts the stone door of the hidden vault.
    public sealed class VaultPuzzle : MonoBehaviour
    {
        public bool Solved { get; private set; }
        public int LitMask { get; private set; }
        readonly List<PuzzleBrazier> braziers = new List<PuzzleBrazier>(); int next; GameObject door; ExpeditionFloor floor;
        public static VaultPuzzle Create(ExpeditionFloor floor, Vector3 doorAt, float yaw, Vector3 hostCenter, Transform root, int seed, int count)
        {
            var go = new GameObject("Acertijo de los braseros"); go.transform.SetParent(root); go.transform.position = hostCenter;
            var puzzle = go.AddComponent<VaultPuzzle>(); puzzle.floor = floor;
            // The door: a carved stone slab with a dim glyph, blocking the corridor until solved.
            puzzle.door = GameObject.CreatePrimitive(PrimitiveType.Cube); puzzle.door.name = "Puerta del acertijo"; puzzle.door.transform.SetParent(root);
            puzzle.door.transform.SetPositionAndRotation(doorAt + Vector3.up * 2.4f, Quaternion.Euler(0, yaw, 0)); puzzle.door.transform.localScale = new Vector3(4.1f, 4.8f, .7f);
            var stone = Resources.Load<Material>("CindersStone/Wall"); if (stone) puzzle.door.GetComponent<Renderer>().sharedMaterial = stone;
            var glyph = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(glyph.GetComponent<Collider>()); glyph.name = "Glifo"; glyph.transform.SetParent(puzzle.door.transform, false);
            glyph.transform.localPosition = new Vector3(0, .1f, -.52f); glyph.transform.localScale = new Vector3(.34f, .34f, 1);
            glyph.GetComponent<Renderer>().sharedMaterial = Codice.Presentation.SoftParticle.RuneMaterial(new Color(1.4f, .6f, .2f));   // carved flame rune, not a flat square
            var obstacle = puzzle.door.AddComponent<NavMeshObstacle>(); obstacle.carving = true; obstacle.size = Vector3.one;
            puzzle.door.AddComponent<PuzzleDoorHint>().puzzle = puzzle;
            // Braziers on a ring around the host room centre, offerings shuffled by seed.
            var rng = new System.Random(seed); var order = Enumerable.Range(1, count).OrderBy(_ => rng.Next()).ToArray();
            float radius = 6.5f; int made = 0;
            for (int i = 0; i < 12 && made < count; i++)
            {
                float a = (i * 360f / 12 + 15) * Mathf.Deg2Rad; var at = hostCenter + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                if (!NavMesh.SamplePosition(at, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                Physics.SyncTransforms();
                if (Physics.OverlapBox(hit.position + Vector3.up * 1f, new Vector3(.7f, .8f, .7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(h => h.bounds.max.y > hit.position.y + .4f)) continue;
                puzzle.braziers.Add(PuzzleBrazier.Create(puzzle, made, order[made], hit.position, go.transform)); made++;
                i += 2;
            }
            if (made < 2) { puzzle.Solve(); }
            return puzzle;
        }
        public void Light(PuzzleBrazier b, PlayerCharacter player)
        {
            if (Solved || b.Lit || Codice.Multiplayer.LanSession.Client) return;
            int wanted = braziers.Where(x => !x.Lit).Min(x => x.Offerings);
            if (b.Offerings != wanted)
            {
                foreach (var x in braziers) x.SetLit(false); LitMask = 0;
                Codice.Mechanics.RelicPulse.Spawn(b.transform.position + Vector3.up, 2, new Color(1, .3f, .05f));
                if (player && Vector3.Distance(player.transform.position, b.transform.position) < 2.5f) player.ReceiveDamage(new DamageInfo { amount = 15, source = gameObject, direction = Vector3.up, secondary = true });
                Codice.UI.RunHUD.Instance?.Announce("Las llamas se apagan · Las ofrendas piden otro orden"); return;
            }
            b.SetLit(true); LitMask |= 1 << b.Index; Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Orb);
            if (braziers.All(x => x.Lit)) Solve();
        }
        void Solve()
        {
            if (Solved) return; Solved = true; if (door) door.SetActive(false);
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Portal);
            Codice.UI.RunHUD.Instance?.Announce("LA PUERTA SELLADA SE ABRE · Una cámara oculta aguarda");
        }
        public void ApplyNetwork(int mask, bool solved) { foreach (var b in braziers) b.SetLit((mask & (1 << b.Index)) != 0); LitMask = mask; if (solved && !Solved) Solve(); }
        public string Hint => "PUERTA SELLADA · Un susurro: «De la menor ofrenda a la mayor, enciende las llamas»";
    }
    public sealed class PuzzleDoorHint : MonoBehaviour, IPlayerInteractable { public VaultPuzzle puzzle; public void Interact(PlayerCharacter p) { Codice.UI.RunHUD.Instance?.Announce(puzzle.Hint); } }
    public sealed class PuzzleBrazier : MonoBehaviour, IPlayerInteractable
    {
        public VaultPuzzle puzzle; public int Index, Offerings; public bool Lit { get; private set; } Light flame; GameObject fire;
        public string Description => Lit ? "BRASERO ENCENDIDO" : "BRASERO DE OFRENDAS · " + Offerings + (Offerings == 1 ? " cráneo" : " cráneos") + "\n[E] Encender";
        public void Interact(PlayerCharacter player) { if (player && Vector3.Distance(player.transform.position, transform.position) < 3.5f) puzzle.Light(this, player); }
        public void SetLit(bool on) { Lit = on; if (flame) flame.enabled = on; if (fire) fire.SetActive(on); }
        public static PuzzleBrazier Create(VaultPuzzle puzzle, int index, int offerings, Vector3 at, Transform parent)
        {
            var go = new GameObject("Brasero de ofrendas " + offerings); go.transform.SetParent(parent); go.transform.position = at;
            var b = go.AddComponent<PuzzleBrazier>(); b.puzzle = puzzle; b.Index = index; b.Offerings = offerings;
            var col = go.AddComponent<CapsuleCollider>(); col.isTrigger = true; col.radius = .8f; col.height = 2; col.center = Vector3.up;
            var stand = Resources.Load<GameObject>("WorldFive/Brazier"); GameObject body = stand ? Instantiate(stand, go.transform, false) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(go.transform, false); if (!stand) body.transform.localScale = new Vector3(.8f, .6f, .8f);
            var skull = Resources.Load<GameObject>("WorldThree/Kit/candle_melted");
            var bone = new Material(Shader.Find("Universal Render Pipeline/Lit")); bone.SetColor("_BaseColor", new Color(.82f, .78f, .66f));
            for (int i = 0; i < offerings; i++)
            {
                float a = i * Mathf.PI * 2 / Mathf.Max(1, offerings); var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = "Cráneo de ofrenda";
                s.transform.SetParent(go.transform, false); s.transform.localPosition = new Vector3(Mathf.Cos(a) * .85f, .14f, Mathf.Sin(a) * .85f); s.transform.localScale = new Vector3(.24f, .22f, .26f); s.GetComponent<Renderer>().sharedMaterial = bone;
            }
            b.fire = new GameObject("Llama"); b.fire.transform.SetParent(go.transform, false); b.fire.transform.localPosition = Vector3.up * 1.35f;
            var ps = b.fire.AddComponent<ParticleSystem>(); var main = ps.main; main.startLifetime = .7f; main.startSpeed = 1.1f; main.startSize = .35f; main.startColor = new Color(1f, .5f, .15f); main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 40; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10; sh.radius = .2f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Codice.Presentation.EmberFire.SharedMaterial;
            b.flame = b.fire.AddComponent<Light>(); b.flame.color = new Color(1f, .55f, .2f); b.flame.range = 6; b.flame.intensity = 2.5f; b.flame.shadows = LightShadows.None;
            b.SetLit(false); return b;
        }
    }
    /// Flame vent: an iron grate that telegraphs (filled disc) and then erupts, hurting players and enemies.
    public sealed class FlameVent : MonoBehaviour
    {
        ExpeditionFloor floor; int room; float clock; GameObject zone; ParticleSystem fire; Light glow; float tick; const float Radius = 1.5f;
        public static void Create(ExpeditionFloor floor, int room, Vector3 at, Transform root, float offset)
        {
            var go = new GameObject("Respiradero de llamas"); go.transform.SetParent(root); go.transform.position = at;
            var v = go.AddComponent<FlameVent>(); v.floor = floor; v.room = room; v.clock = offset;
            var grate = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(grate.GetComponent<Collider>()); grate.name = "Rejilla"; grate.transform.SetParent(go.transform, false);
            grate.transform.localPosition = Vector3.up * .03f; grate.transform.localScale = new Vector3(2.2f, .03f, 2.2f);
            var iron = new Material(Shader.Find("Universal Render Pipeline/Lit")); iron.SetColor("_BaseColor", new Color(.08f, .06f, .05f)); iron.EnableKeyword("_EMISSION"); iron.SetColor("_EmissionColor", new Color(.6f, .12f, .02f)); grate.GetComponent<Renderer>().sharedMaterial = iron;
            var fx = new GameObject("Columna de fuego"); fx.transform.SetParent(go.transform, false);
            v.fire = fx.AddComponent<ParticleSystem>(); var main = v.fire.main; main.startLifetime = .8f; main.startSpeed = 6; main.startSize = .9f; main.startColor = new Color(1f, .45f, .1f); main.maxParticles = 200; main.playOnAwake = false;
            var em = v.fire.emission; em.rateOverTime = 120; var sh = v.fire.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12; sh.radius = .6f; fx.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            v.fire.GetComponent<ParticleSystemRenderer>().sharedMaterial = Codice.Presentation.EmberFire.SharedMaterial; v.fire.Stop();
            v.glow = go.AddComponent<Light>(); v.glow.color = new Color(1, .45f, .12f); v.glow.range = 7; v.glow.intensity = 0; v.glow.shadows = LightShadows.None;
        }
        void Update()
        {
            if (!floor || !floor.context || !floor.context.player) return;
            // room < 0: corridor vent, active while the player is close.
            bool here = room < 0 ? (floor.context.player.transform.position - transform.position).sqrMagnitude < 196 : floor.Layout.RoomAt(floor.context.player.transform.position) == room || floor.CurrentRoom == room && floor.InEncounter;
            if (!here) { if (zone) Destroy(zone); if (fire.isPlaying) fire.Stop(); glow.intensity = 0; return; }
            clock += Time.deltaTime; float cycle = clock % 4.6f;   // 2.9 s rest · 0.9 s warning · 0.8 s fire
            if (cycle < 2.9f) { if (zone) Destroy(zone); if (fire.isPlaying) fire.Stop(); glow.intensity = .3f; return; }
            if (cycle < 3.8f) { if (!zone) zone = Codice.Bosses.BerserkerZone.Disc(transform.position, Radius, new Color(1f, .4f, .1f), .9f); glow.intensity = 1.5f; return; }
            if (zone) Destroy(zone); if (!fire.isPlaying) { fire.Play(); Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Explosion, transform.position); }
            glow.intensity = 4; tick -= Time.deltaTime; if (tick > 0 || Codice.Multiplayer.LanSession.Client) return; tick = .4f;
            var burned = new HashSet<Component>();
            foreach (var hit in Physics.OverlapSphere(transform.position + Vector3.up, Radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var p = hit.GetComponentInParent<PlayerCharacter>(); if (p) { if (burned.Add(p)) p.ReceiveDamage(new DamageInfo { amount = 12, source = gameObject, direction = Vector3.up, secondary = true }); continue; }
                var e = hit.GetComponentInParent<Codice.Enemies.BasicEnemy>(); if (e && e.IsAlive && burned.Add(e)) e.ReceiveDamage(new DamageInfo { amount = 10, source = gameObject, direction = Vector3.up, secondary = true });
            }
        }
        void OnDestroy() { if (zone) Destroy(zone); }
    }
    /// Toxic ash pool (World 3+): steady damage while standing in it.
    public sealed class AshPool : MonoBehaviour
    {
        ExpeditionFloor floor; int room; float tick;
        public static void Create(ExpeditionFloor floor, int room, Vector3 at, Transform root)
        {
            var go = new GameObject("Charco de ceniza tóxica"); go.transform.SetParent(root); go.transform.position = at; var p = go.AddComponent<AshPool>(); p.floor = floor; p.room = room;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(disc.GetComponent<Collider>()); disc.transform.SetParent(go.transform, false); disc.transform.localPosition = Vector3.up * .02f; disc.transform.localScale = new Vector3(3.6f, .01f, 3.2f);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", new Color(.05f, .07f, .04f)); m.SetFloat("_Smoothness", .96f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.03f, .1f, .02f));   /* dark reflective puddle with a faint glow, not a lit disc */ disc.GetComponent<Renderer>().sharedMaterial = m;
            var l = go.AddComponent<Light>(); l.color = new Color(.4f, 1f, .3f); l.range = 3.5f; l.intensity = .45f; l.shadows = LightShadows.None;
        }
        void Update()
        {
            if (Codice.Multiplayer.LanSession.Client || !floor || !floor.context) return; tick -= Time.deltaTime; if (tick > 0) return; tick = .5f;
            var hurt = new HashSet<PlayerCharacter>();
            foreach (var hit in Physics.OverlapSphere(transform.position + Vector3.up * .5f, 1.7f, ~0, QueryTriggerInteraction.Ignore))
            { var p = hit.GetComponentInParent<PlayerCharacter>(); if (p && hurt.Add(p)) p.ReceiveDamage(new DamageInfo { amount = 4, source = gameObject, direction = Vector3.up, secondary = true }); }
        }
    }
}
