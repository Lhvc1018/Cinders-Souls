using System.Collections.Generic;
using UnityEngine;
using Codice.Bosses;
using Codice.Enemies;
using Codice.Presentation;
namespace Codice.Mechanics
{
    // Sekiro-style enemy posture, filled by the Shinobi's deflects and katana hits.
    // Full posture staggers the target and opens a deathblow window. Bar is drawn over the target.
    public sealed class EnemyPosture : MonoBehaviour
    {
        public float Value { get; private set; }
        public bool Broken => Time.time < brokenUntil;
        float brokenUntil, lastHit;
        Component owner; LineRenderer back, fill;
        const float BreakSeconds = 2.6f;
        public static EnemyPosture Of(Component target, bool create)
        {
            if (!target) return null;
            var p = target.GetComponent<EnemyPosture>();
            if (!p && create && CombatTargets.IsEnemy(target)) { p = target.gameObject.AddComponent<EnemyPosture>(); p.owner = target; }
            return p;
        }
        /// Adds posture; returns true when this call broke it.
        public static bool Add(Component target, float amount)
        {
            var p = Of(target, true); if (!p || p.Broken) return false;
            bool boss = target is BossCharacter;
            p.Value = Mathf.Min(100, p.Value + amount * (boss ? .55f : 1)); p.lastHit = Time.time;
            if (p.Value < 100) return false;
            p.brokenUntil = Time.time + BreakSeconds; CombatTargets.Stun(target, BreakSeconds);
            var at = p.Top();
            ImpactVfx.Spawn("VFX_ImpactCross", at, Camera.main ? Camera.main.transform.position - at : Vector3.back, .6f);
            LordsAttackVfx.ImpactTinted(at, Vector3.up, new Color(1, .25f, .1f, .9f), .7f);
            Codice.UI.RunHUD.Instance?.Announce("POSTURA ROTA · ¡Golpe mortal!");
            return true;
        }
        /// Consumes a broken posture as a deathblow. Returns the damage multiplier (1 if not broken).
        public static float Deathblow(Component target)
        {
            var p = Of(target, false); if (!p || !p.Broken) return 1;
            p.brokenUntil = 0; p.Value = 0;
            var at = p.Top() - Vector3.up * .5f;
            ImpactVfx.Spawn("VFX_Classic_03", at, Vector3.up, .9f); ImpactVfx.Spawn("VFX_Blood_02", at, Vector3.up, .6f);
            LordsAttackVfx.SlashTinted(at, Camera.main ? Camera.main.transform.forward : Vector3.forward, new Color(1, .15f, .08f, .9f), 2.2f, 35, .35f);
            HitStop.Do(.14f);
            return target is BossCharacter ? 2.2f : 3.5f;
        }
        Vector3 Top()
        {
            var c = GetComponent<Collider>();
            return c ? new Vector3(c.bounds.center.x, c.bounds.max.y + .45f, c.bounds.center.z) : transform.position + Vector3.up * 2.4f;
        }
        void Awake()
        {
            back = Line("Posture back", new Color(0, 0, 0, .7f), .11f);
            fill = Line("Posture fill", new Color(.95f, .72f, .25f, 1), .07f);
        }
        LineRenderer Line(string n, Color c, float w)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var l = BloodVisual.Line(go, c, w); l.positionCount = 2; l.sortingOrder = 50; l.enabled = false; return l;
        }
        void LateUpdate()
        {
            if (!Broken && Time.time - lastHit > 1.8f) Value = Mathf.Max(0, Value - 14 * Time.deltaTime);
            bool show = Value > .5f || Broken; back.enabled = fill.enabled = show; if (!show) return;
            var cam = Camera.main; if (!cam) return;
            var top = Top(); var right = cam.transform.right * .55f;
            back.SetPosition(0, top - right); back.SetPosition(1, top + right);
            float k = Broken ? 1 : Value / 100;
            // Sekiro bar fills from the centre outward.
            fill.SetPosition(0, top - right * k); fill.SetPosition(1, top + right * k);
            var color = Broken ? new Color(1, .12f, .06f, 1) : Color.Lerp(new Color(.95f, .72f, .25f), new Color(1, .35f, .1f), k);
            fill.startColor = fill.endColor = color;
        }
    }
}
