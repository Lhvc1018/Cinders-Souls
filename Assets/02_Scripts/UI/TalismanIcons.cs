using System.Collections.Generic;
using Codice.Characters;
using UnityEngine;
namespace Codice.UI
{
    /// Hand-drawn (procedural) icon per talisman: rings are a metal band with a set stone, amulets a chain with a pendant.
    /// The stone's cut follows the main stat (round = vitality, diamond = arcane, square = defence, spike = offence,
    /// teardrop = flask/mobility) and its colour is the talisman tint, so every talisman reads differently at a glance.
    public static class TalismanIcons
    {
        static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
        public static Sprite Get(Talisman t)
        {
            if (cache.TryGetValue((int)t, out var s) && s) return s;
            const int N = 96; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "Talisman " + t, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var e = TalismanCatalog.All[(int)t]; bool ring = e.kind == "Anillo"; var metal = new Color(.82f, .68f, .4f); var gem = e.tint;
            var stat = e.mods[0].stat; int cut = stat == TalismanStat.MaxHealth ? 0 : stat == TalismanStat.MaxMana || stat == TalismanStat.ManaRegen ? 1 : stat == TalismanStat.MaxArmor || stat == TalismanStat.DamageTaken ? 2 : stat == TalismanStat.Damage || stat == TalismanStat.AttackSpeed ? 3 : 4;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                var p = new Vector2(x, y) / (N - 1) * 2 - Vector2.one; Color c = Color.clear;
                if (ring)
                {
                    var q = new Vector2(p.x, (p.y + .15f) * 1.25f); float band = Mathf.Abs(q.magnitude - .55f);
                    if (band < .1f) c = Shade(metal, .75f + .35f * (1 - band / .1f) - .2f * q.y);
                    var g = p - new Vector2(0, .5f); if (Stone(g, cut, .3f)) c = Shade(gem, 1.15f - .5f * (g.x + g.y));
                }
                else
                {
                    float chain = Mathf.Abs(Mathf.Abs(p.x) * 1.4f - (p.y - .05f) * -1.1f - .95f);
                    if (p.y > .05f && chain < .05f && Mathf.Repeat((p.x + 1) * 9, 1) < .7f) c = Shade(metal, .9f);
                    var g = p - new Vector2(0, -.25f); if (g.magnitude < .48f && g.magnitude > .38f) c = Shade(metal, .8f + .4f * -g.y);
                    if (Stone(g, cut, .36f)) c = Shade(gem, 1.15f - .5f * (g.x + g.y));
                }
                px[y * N + x] = c;
            }
            tex.SetPixels(px); tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(.5f, .5f), 100); cache[(int)t] = s; return s;
        }
        static Color Shade(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1);
        static bool Stone(Vector2 g, int cut, float r)
        {
            switch (cut)
            {
                case 0: return g.magnitude < r;
                case 1: return Mathf.Abs(g.x) + Mathf.Abs(g.y) < r * 1.2f;
                case 2: return Mathf.Abs(g.x) < r * .8f && Mathf.Abs(g.y) < r * .8f;
                case 3: { float a = Mathf.Atan2(g.y, g.x); float rr = r * (.6f + .4f * Mathf.Abs(Mathf.Cos(a * 2.5f))); return g.magnitude < rr; }
                default: return g.magnitude < r * (g.y < 0 ? 1 : 1 - g.y / (r * 1.6f)) && g.y < r * 1.4f;
            }
        }
    }
}
