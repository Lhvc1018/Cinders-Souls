using System.Collections.Generic;
using Codice.Characters;
using Codice.Mechanics;
using UnityEngine;
namespace Codice.Expedition
{
    /// Brann, el herrero de la escoria: reforja el arma ACTIVA un nivel (T1→T4) conservando su tipo y sus rasgos
    /// (katana, shuriken, munición). Así un Shinobi puede quedarse con su katana todo el descenso.
    /// Las armas de la escuela de la clase (afinidad Maestra) cuestan un 20 % menos. Una reforja por jugador y yunque.
    public sealed class Blacksmith : MonoBehaviour, IPlayerInteractable
    {
        readonly HashSet<PlayerCharacter> served = new HashSet<PlayerCharacter>();
        public static int Price(CharacterState s, WeaponInstance w)
        {
            if (w == null || w.tier >= 4) return -1;
            int baseCost = w.tier == 1 ? 150 : w.tier == 2 ? 280 : 450;
            return ClassArsenal.Effective(s, w) == WeaponAffinity.Maestra ? Mathf.RoundToInt(baseCost * .8f) : baseCost;
        }
        public string Describe(PlayerCharacter player)
        {
            if (!player) return "";
            var w = player.State.ActiveWeapon; string name = w != null ? Codice.UI.UIWidgets.WeaponName(w) : "—";
            if (served.Contains(player)) return "BRANN · HERRERO DE LA ESCORIA\nEl yunque aún humea con tu arma. Busca otro herrero más abajo.";
            if (w == null) return "BRANN · HERRERO DE LA ESCORIA\nEmpuña un arma para reforjarla.";
            if (w.tier >= 4) return $"BRANN · HERRERO DE LA ESCORIA\n{name} T4 ya es una obra maestra. No queda nada que templar.";
            var next = new WeaponInstance(w.kind, w.tier + 1) { katana = w.katana, shuriken = w.shuriken };
            int price = Price(player.State, w);
            bool school = ClassArsenal.Effective(player.State, w) == WeaponAffinity.Maestra;
            return $"BRANN · HERRERO DE LA ESCORIA\n[E] Reforjar {name} T{w.tier} → T{w.tier + 1} · {price} oro{(school ? " (precio de escuela)" : "")}\n{w.Damage:0} → {next.Damage:0} daño · conserva el arma y su afinidad";
        }
        public void Interact(PlayerCharacter player)
        {
            if (Codice.Multiplayer.LanSession.Client || !player || !player.State.IsAlive || player.ControlsBlocked || Vector3.Distance(player.transform.position, transform.position) > 3.8f) return;
            var hud = Codice.UI.RunHUD.Instance; var w = player.State.ActiveWeapon;
            if (served.Contains(player)) { hud?.Announce("Brann: «Una reforja por visita. El acero necesita enfriarse.»"); return; }
            int price = Price(player.State, w);
            if (price < 0) { hud?.Announce(w == null ? "Brann: «¿Qué quieres que temple, tus nudillos?»" : "Brann: «Esa hoja ya no admite más fuego.»"); return; }
            if (!player.State.SpendGold(price)) { hud?.Announce("Brann: «Son " + price + " de oro. El carbón no se paga con promesas.»"); return; }
            w.tier++; if (w.Capacity > 0) w.ammunition = Mathf.Max(w.ammunition, w.Capacity);
            served.Add(player); player.State.NotifyChanged();
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Pickup);
            ImpactSpark(); hud?.Announce("REFORJA · " + Codice.UI.UIWidgets.WeaponName(w) + " T" + w.tier + " · " + w.Damage.ToString("0") + " daño");
        }
        void ImpactSpark() { RelicPulse.Spawn(transform.position + transform.forward * .9f + Vector3.up * 1.1f, .6f, new Color(1f, .55f, .2f)); }
        public static void NetworkReforge(PlayerCharacter player, int id) { foreach (var b in FindObjectsByType<Blacksmith>(FindObjectsSortMode.None)) if (Vector3.Distance(b.transform.position, player.transform.position) < 4) { b.Interact(player); return; } }
        /// Built deterministically by every machine's floor so guests see the smith and get the prompt; only the host forges.
        public static Blacksmith Create(Vector3 position, float yaw, Transform parent, int floor)
        {
            var go = new GameObject("Brann · Herrero de la escoria"); go.transform.SetParent(parent); go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var smith = go.AddComponent<Blacksmith>();
            var col = go.AddComponent<CapsuleCollider>(); col.isTrigger = true; col.radius = 1.1f; col.height = 2.2f; col.center = Vector3.up * 1.1f;
            var model = Resources.Load<GameObject>("WorldProgression/Nereo");
            if (model)
            {
                var body = Instantiate(model, go.transform, false); body.name = "Brann"; body.transform.localScale = Vector3.one * 1.04f;
                foreach (var r in body.GetComponentsInChildren<Renderer>()) for (int i = 0; i < r.sharedMaterials.Length; i++)
                    { var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block, i); block.SetColor("_BaseColor", i % 2 == 0 ? new Color(.24f, .15f, .09f) : new Color(.16f, .12f, .1f)); r.SetPropertyBlock(block, i); }
                foreach (var c in body.GetComponentsInChildren<Collider>()) Destroy(c);
            }
            var anvil = Resources.Load<GameObject>("WorldFour/Anvil");
            if (anvil) { var a = Instantiate(anvil, go.transform, false); a.transform.localPosition = new Vector3(0, 0, 1.25f); a.transform.localRotation = Quaternion.Euler(0, 90, 0); a.transform.localScale *= .75f; }
            var coals = Resources.Load<GameObject>("WorldThree/Kit/barrel_small_stack");
            if (coals) { var b = Instantiate(coals, go.transform, false); b.transform.localPosition = new Vector3(-1.5f, 0, .6f); b.transform.localScale *= .6f; }
            var light = new GameObject("Brasa de la forja").AddComponent<Light>(); light.transform.SetParent(go.transform, false); light.transform.localPosition = new Vector3(0, 1.4f, 1.2f);
            light.type = LightType.Point; light.color = new Color(1f, .5f, .18f); light.range = 7; light.intensity = 3.2f; light.shadows = LightShadows.None;
            return smith;
        }
    }
}
