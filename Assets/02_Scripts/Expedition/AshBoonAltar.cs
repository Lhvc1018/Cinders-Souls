using System.Collections.Generic;
using System.Linq;
using Codice.Characters;
using UnityEngine;
namespace Codice.Expedition
{
    /// Altar de ascuas: three Ash Boons float over a basalt plinth; each player may take one. Appears after some
    /// cleared rooms and in hidden vaults. A single orb (no altar) can also spill out of weapon chests.
    public sealed class AshBoonAltar : MonoBehaviour
    {
        readonly HashSet<PlayerCharacter> served = new HashSet<PlayerCharacter>();
        public bool Served(PlayerCharacter p) => served.Contains(p);
        public void Take(PlayerCharacter p)
        {
            served.Add(p);
            int team = Codice.Multiplayer.LanSession.Host ? Codice.Multiplayer.LanSession.Instance.Players().Count() : 1;
            if (served.Count >= team) Destroy(gameObject);
        }
        public static AshBoonAltar Create(Vector3 at, int seed, Transform parent, PlayerCharacter reference)
        {
            var go = new GameObject("Altar de ascuas"); go.transform.SetParent(parent); go.transform.position = at;
            var altar = go.AddComponent<AshBoonAltar>();
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder); plinth.name = "Peana de basalto"; plinth.transform.SetParent(go.transform, false);
            plinth.transform.localPosition = Vector3.up * .35f; plinth.transform.localScale = new Vector3(2.4f, .35f, 2.4f);
            var stone = Resources.Load<Material>("WorldFour/Basalto estratificado"); if (stone) plinth.GetComponent<Renderer>().sharedMaterial = stone;
            var rng = new System.Random(seed); var pool = Enumerable.Range(0, BoonCatalog.Count).Select(i => (AshBoon)i).Where(b => !reference || reference.State.CanTakeBoon(b)).OrderBy(_ => rng.Next()).Take(3).ToArray();
            for (int i = 0; i < pool.Length; i++)
            {
                float a = (i - 1) * 50 * Mathf.Deg2Rad;
                AshBoonOrb.Create(pool[i], go.transform.position + new Vector3(Mathf.Sin(a) * .85f, 1.25f, Mathf.Cos(a) * .25f), go.transform, altar);
            }
            Codice.Mechanics.RelicPulse.Spawn(at + Vector3.up, 1.2f, new Color(1f, .6f, .25f));
            return altar;
        }
    }
    public sealed class AshBoonOrb : MonoBehaviour, IPlayerInteractable
    {
        public AshBoon boon; public AshBoonAltar altar; Transform core; float phase;
        public string Description => "BENDICIÓN DE CENIZA · " + BoonCatalog.Names[(int)boon] + "\n" + BoonCatalog.Effects[(int)boon] + "\n[E] Aceptar" + (altar ? " · Elige una de tres" : "") + " · Acumulable ×" + BoonCatalog.MaxStacks;
        public void Interact(PlayerCharacter player)
        {
            if (Codice.Multiplayer.LanSession.Client || !player || !player.State.IsAlive || player.ControlsBlocked || Vector3.Distance(player.transform.position, transform.position) > 3.5f) return;
            var hud = Codice.UI.RunHUD.Instance;
            if (altar && altar.Served(player)) { hud?.Announce("Ya tomaste una bendición de este altar."); return; }
            if (!player.State.GrantBoon(boon)) { hud?.Announce(BoonCatalog.Names[(int)boon] + " ya está al máximo."); return; }
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Orb);
            Codice.Mechanics.RelicPulse.Spawn(transform.position, .9f, BoonCatalog.Colors[(int)boon]);
            hud?.Announce("BENDICIÓN · " + BoonCatalog.Names[(int)boon] + " ×" + player.State.Boon(boon) + " · " + BoonCatalog.Effects[(int)boon]);
            if (altar) altar.Take(player); else Destroy(gameObject);
        }
        public static AshBoonOrb Create(AshBoon boon, Vector3 at, Transform parent, AshBoonAltar altar = null)
        {
            var go = new GameObject("Bendición · " + BoonCatalog.Names[(int)boon]); go.transform.SetParent(parent); go.transform.position = at;
            var orb = go.AddComponent<AshBoonOrb>(); orb.boon = boon; orb.altar = altar; orb.phase = (int)boon;
            var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = .45f;
            orb.core = Visual(go.transform, BoonCatalog.Colors[(int)boon]); return orb;
        }
        public static Transform Visual(Transform parent, Color color)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = "Ascua"; s.transform.SetParent(parent, false); s.transform.localScale = Vector3.one * .32f;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", color * .6f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 3.2f); s.GetComponent<Renderer>().sharedMaterial = m;
            var l = parent.gameObject.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.range = 3; l.intensity = 1.6f; l.shadows = LightShadows.None;
            return s.transform;
        }
        void Update() { if (core) { core.localPosition = Vector3.up * Mathf.Sin(Time.time * 2 + phase) * .08f; core.Rotate(0, 90 * Time.deltaTime, 0); } }
    }
}
