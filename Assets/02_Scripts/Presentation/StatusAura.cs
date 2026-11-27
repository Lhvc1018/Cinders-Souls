using Codice.Characters;
using TMPro;
using UnityEngine;
namespace Codice.Presentation
{
    /// Makes elemental states readable on enemies: colored flickering light, recurring particles and a small
    /// label over the head (QUEMADO / CONGELADO / ENVENENADO / ELECTROCUTADO) while the state lasts.
    public sealed class StatusAura : MonoBehaviour
    {
        ElementalStatus status; Light glow; TextMeshPro label; float nextPuff; Collider body;
        static readonly Color[] Colors = { Color.clear, new Color(1f, .45f, .1f), new Color(.45f, .8f, 1f), new Color(.45f, .9f, .2f), new Color(.7f, .75f, 1f) };
        static readonly string[] Names = { "", "QUEMADO", "CONGELADO", "ENVENENADO", "ELECTROCUTADO" };
        public static void Ensure(ElementalStatus s) { if (s && !s.GetComponent<PlayerCharacter>() && !s.GetComponent<StatusAura>()) s.gameObject.AddComponent<StatusAura>(); }
        /// Sound + burst the moment a state triggers.
        public static void Proc(Component target, Element e)
        {
            if (!target) return; var p = Center(target);
            GameAudio.PlayAt(e == Element.Burn ? SoundCue.SpellFire : e == Element.Frost ? SoundCue.SpellFrost : e == Element.Poison ? SoundCue.SpellPoison : SoundCue.SpellLightning, p);
            Puff(e, p, .45f);
        }
        static Vector3 Center(Component c) { var col = c.GetComponentInChildren<Collider>(); return col ? col.bounds.center : c.transform.position + Vector3.up; }
        static void Puff(Element e, Vector3 p, float scale)
        {
            switch (e)
            {
                case Element.Burn: ImportedAttackVfx.Burst(p, true, scale * .6f); break;
                case Element.Frost: StaffSpells.Effect("Ice", p - Vector3.up * .8f, scale, 1.2f); break;
                case Element.Poison: ImpactVfx.Spawn("VFX_Poison_01", p, Vector3.up, scale, 1.2f); break;
                case Element.Shock: ImportedAttackVfx.Burst(p, false, scale * .6f); break;
            }
        }
        void Awake()
        {
            status = GetComponent<ElementalStatus>(); body = GetComponentInChildren<Collider>();
            glow = new GameObject("Status glow").AddComponent<Light>(); glow.transform.SetParent(transform, false); glow.transform.localPosition = Vector3.up * 1.2f;
            glow.type = LightType.Point; glow.range = 4; glow.intensity = 0; glow.shadows = LightShadows.None;
            label = new GameObject("Status label").AddComponent<TextMeshPro>(); label.transform.SetParent(transform, false);
            label.fontSize = 1.3f; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(4, .6f); label.text = "";
            label.fontStyle = FontStyles.Bold; label.outlineWidth = .2f; label.outlineColor = Color.black;
        }
        void LateUpdate()
        {
            if (!status) { Destroy(this); return; }
            int active = 0; float best = 0;
            for (int i = 1; i < 5; i++) { float r = status.Until[i] - Time.time; if (r > best) { best = r; active = i; } }
            float top = body ? body.bounds.max.y : transform.position.y + 2;
            label.transform.position = new Vector3(transform.position.x, top + .45f, transform.position.z);
            var cam = Camera.main; if (cam) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
            if (active == 0) { glow.intensity = 0; label.text = ""; return; }
            var c = Colors[active];
            glow.color = c; glow.intensity = (active == (int)Element.Shock ? Random.Range(1f, 4f) : 2.2f + Mathf.Sin(Time.time * 9) * .6f);
            label.text = $"{Names[active]} {Mathf.CeilToInt(best)}s"; label.color = c;
            if (Time.time >= nextPuff) { nextPuff = Time.time + .55f; Puff((Element)active, body ? body.bounds.center : transform.position + Vector3.up, .3f); }
        }
    }
}
