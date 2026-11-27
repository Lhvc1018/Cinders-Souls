using UnityEngine;
namespace Codice.Presentation
{
    /// Readable stun state: a ring of orbiting sparks and a pulsing light above the head for the stun's duration.
    public sealed class StunHalo : MonoBehaviour
    {
        float until, size = 1; Transform[] sparks; Light glow; float height;
        public static void Show(Component target, float seconds)
        {
            if (!target || seconds <= 0) return;
            var halo = target.GetComponentInChildren<StunHalo>();
            if (!halo)
            {
                var go = new GameObject("Aturdido"); go.transform.SetParent(target.transform, false);
                halo = go.AddComponent<StunHalo>();
                var col = target.GetComponent<CapsuleCollider>(); float h = col ? col.center.y + col.height * .5f : 2f; halo.height = h * target.transform.lossyScale.y + .35f;
                go.transform.localPosition = Vector3.up * (halo.height / Mathf.Max(.01f, target.transform.lossyScale.y));
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", new Color(1f, .86f, .3f));
                halo.sparks = new Transform[5]; float size = Mathf.Max(1f, (col ? col.radius : .5f) * target.transform.lossyScale.x / .45f); halo.size = size;
                for (int i = 0; i < 5; i++)
                {
                    var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = "Chispa"; s.transform.SetParent(go.transform, false);
                    s.transform.localScale = Vector3.one * .16f * size / Mathf.Max(.01f, target.transform.lossyScale.x); s.GetComponent<Renderer>().sharedMaterial = mat; halo.sparks[i] = s.transform;
                }
                halo.glow = go.AddComponent<Light>(); halo.glow.type = LightType.Point; halo.glow.color = new Color(1f, .85f, .35f); halo.glow.range = 4; halo.glow.shadows = LightShadows.None;
            }
            halo.until = Mathf.Max(halo.until, Time.time + seconds); halo.gameObject.SetActive(true);
        }
        void Update()
        {
            if (Time.time > until) { gameObject.SetActive(false); return; }
            float r = .45f / Mathf.Max(.01f, transform.lossyScale.x) * Mathf.Max(1, transform.lossyScale.x);
            for (int i = 0; i < sparks.Length; i++) { float a = Time.time * 5 + i * Mathf.PI * 2 / sparks.Length; sparks[i].localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2) * .08f, Mathf.Sin(a)) * .6f * size / Mathf.Max(.01f, transform.lossyScale.x); }
            transform.rotation = Quaternion.identity; glow.intensity = 1.4f + Mathf.Sin(Time.time * 12) * .5f;
        }
    }
}
