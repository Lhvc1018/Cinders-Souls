using System.Collections.Generic;
using UnityEngine;
namespace Codice.Characters
{
    /// One active effect shown under the vitals (Elden Ring style): icon, tint, short label and timer.
    public struct BuffInfo
    {
        public string label; public Codice.UI.HudSymbol icon; public Color tint; public float remaining, duration;
        public BuffInfo(string label, Codice.UI.HudSymbol icon, Color tint, float remaining, float duration) { this.label = label; this.icon = icon; this.tint = tint; this.remaining = remaining; this.duration = duration; }
    }
    /// Any player component that grants timed or standing effects implements this; the HUD gathers them all,
    /// so new buffs (spells, imbues, items) appear without touching the HUD.
    public interface IBuffSource { void CollectBuffs(List<BuffInfo> into); }
}
