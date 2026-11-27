using System.Collections.Generic;
using Codice.Presentation;
using UnityEngine;
namespace Codice.Characters
{
    /// Elemental imbue on the held weapon (fire, frost, lightning, poison): bonus damage, status buildup on every melee
    /// hit and a glowing trail on the blade for a limited time. Data lives in the Imbues table; any source (spell,
    /// item, class skill) calls Apply. Generalises the Shinobi's old one-off blade effect.
    public sealed class WeaponImbue : MonoBehaviour, IBuffSource
    {
        public struct Entry { public string name; public Color color; public float bonus, buildup; }
        public static readonly Dictionary<Element, Entry> Imbues = new Dictionary<Element, Entry>
        {
            { Element.Burn,   new Entry { name = "Hoja ígnea",     color = new Color(1f, .45f, .12f), bonus = .20f, buildup = 26 } },
            { Element.Frost,  new Entry { name = "Hoja escarchada", color = new Color(.55f, .85f, 1f),  bonus = .12f, buildup = 30 } },
            { Element.Shock,  new Entry { name = "Hoja de tormenta", color = new Color(.75f, .8f, 1f),  bonus = .18f, buildup = 24 } },
            { Element.Poison, new Entry { name = "Hoja venenosa",   color = new Color(.55f, .9f, .3f),  bonus = .10f, buildup = 34 } },
        };
        public Element Active { get; private set; }
        float until, duration; Transform visualHost; GameObject visual;
        public bool Imbued => Active != Element.None && Time.time < until;
        public float DamageFactor => Imbued ? 1 + Imbues[Active].bonus : 1;
        public void Apply(Element element, float seconds)
        {
            if (!Imbues.ContainsKey(element)) return;
            Active = element; duration = seconds; until = Time.time + seconds; Rebuild();
            Codice.UI.RunHUD.Instance?.Announce(Imbues[element].name.ToUpper() + " · " + Mathf.RoundToInt(seconds) + " s");
        }
        /// Called by PlayerCombat after a melee hit lands.
        public void OnMeleeHit(Component target)
        {
            if (!Imbued || !target) return;
            var status = target.GetComponent<ElementalStatus>() ?? target.gameObject.AddComponent<ElementalStatus>();
            status.Add(Active, Imbues[Active].buildup, gameObject);
        }
        void Update()
        {
            if (Active != Element.None && Time.time >= until) { Active = Element.None; Clear(); return; }
            if (!Imbued) return;
            // Weapon swaps replace the held model: follow the current one.
            var held = GetComponent<HumanPreviewCamera>()?.HeldWeapon;
            if (held != visualHost) Rebuild();
        }
        void Rebuild()
        {
            Clear(); var held = GetComponent<HumanPreviewCamera>()?.HeldWeapon; visualHost = held; if (!held || !Imbued) return;
            var c = Imbues[Active].color;
            visual = new GameObject("Weapon imbue"); visual.transform.SetParent(held, false); visual.transform.localPosition = Vector3.up * .55f;
            var trail = visual.AddComponent<TrailRenderer>(); trail.time = .18f; trail.startWidth = .12f; trail.endWidth = 0; trail.minVertexDistance = .02f;
            trail.sharedMaterial = Resources.Load<Material>("Presentation/LordsVfx/Slash"); trail.startColor = new Color(c.r, c.g, c.b, .9f); trail.endColor = new Color(c.r, c.g, c.b, 0);
            var light = visual.AddComponent<Light>(); light.color = c; light.range = 3.5f; light.intensity = 1.6f; light.shadows = LightShadows.None;
        }
        void Clear() { if (visual) Destroy(visual); visual = null; visualHost = null; }
        void OnDisable() => Clear();
        public void CollectBuffs(List<BuffInfo> into)
        {
            if (Imbued) into.Add(new BuffInfo("+" + Mathf.RoundToInt(Imbues[Active].bonus * 100) + "%", Codice.UI.HudSymbol.Lance, Imbues[Active].color, until - Time.time, duration));
        }
    }
}
