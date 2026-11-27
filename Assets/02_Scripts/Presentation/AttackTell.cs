using UnityEngine;
namespace Codice.Presentation
{
    /// Readable attack tell: a short white-gold glint at head height when an enemy commits to a strike,
    /// so block/parry can be timed by eye instead of guessed.
    public sealed class AttackTell : MonoBehaviour
    {
        static Material glint; float life, total; Light lamp; Transform core;
        public static void Flash(Component owner, float windup, bool ranged)
        {
            if (!owner) return;
            if (!glint) { glint = new Material(Shader.Find("Universal Render Pipeline/Unlit")); glint.SetColor("_BaseColor", new Color(1f, .95f, .75f)); }
            var col = owner.GetComponent<CapsuleCollider>(); float h = col ? (col.center.y + col.height * .45f) * owner.transform.lossyScale.y : 1.6f;
            var go = new GameObject("Aviso de ataque"); go.transform.SetParent(owner.transform, false); go.transform.position = owner.transform.position + Vector3.up * h + owner.transform.forward * .35f;
            var tell = go.AddComponent<AttackTell>(); tell.total = tell.life = Mathf.Clamp(windup * .45f, .16f, .3f);
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.transform.SetParent(go.transform, false); s.transform.localScale = Vector3.one * .01f; s.GetComponent<Renderer>().sharedMaterial = glint; s.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tell.core = s.transform;
            tell.lamp = go.AddComponent<Light>(); tell.lamp.type = LightType.Point; tell.lamp.color = ranged ? new Color(.6f, .8f, 1f) : new Color(1f, .85f, .5f); tell.lamp.range = 3.5f; tell.lamp.shadows = LightShadows.None;
        }
        void Update()
        {
            life -= Time.deltaTime; if (life <= 0) { Destroy(gameObject); return; }
            float k = 1 - life / total, pulse = Mathf.Sin(k * Mathf.PI);
            core.localScale = Vector3.one * (.06f + .22f * pulse) / Mathf.Max(.01f, transform.lossyScale.x); core.Rotate(0, 0, 600 * Time.deltaTime);
            lamp.intensity = 3.5f * pulse;
        }
    }
}
