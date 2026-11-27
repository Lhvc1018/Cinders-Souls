using UnityEngine;
namespace Codice.Presentation
{
    /// Short star-shaped glint at an enemy's weapon hand right before a melee hit: the timing cue for a perfect deflect.
    public sealed class DeflectGlint : MonoBehaviour
    {
        static Material mat; float age; Light l; const float Life = .22f;
        public static void Show(Component enemy)
        {
            var anim = enemy.GetComponentInChildren<Animator>(); Transform hand = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
            var at = hand ? hand.position : enemy.transform.position + Vector3.up * 1.5f + enemy.transform.right * .4f;
            if (Codice.Multiplayer.LanSession.Host) Codice.Multiplayer.LanSession.Instance.EmitEffect(NetKey, at, 1, Life);
            ShowAt(at);
        }
        public const string NetKey = "Glint";
        public static void ShowAt(Vector3 at)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(go.GetComponent<Collider>()); go.name = "Deflect glint"; go.transform.position = at;
            if (!mat) mat = SoftParticle.RuneMaterial(new Color(6f, 5f, 3.5f));
            go.GetComponent<Renderer>().sharedMaterial = mat; go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var g = go.AddComponent<DeflectGlint>(); g.l = go.AddComponent<Light>(); g.l.color = new Color(1f, .9f, .7f); g.l.range = 3; g.l.intensity = 4; g.l.shadows = LightShadows.None;
            GameAudio.PlayAt(SoundCue.Coin, at);   // bright "ting", distinct from the parry clash
        }
        void LateUpdate()
        {
            age += Time.deltaTime; float k = age / Life; if (k >= 1) { Destroy(gameObject); return; }
            var cam = Camera.main; if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            float s = Mathf.Sin(k * Mathf.PI) * .55f; transform.localScale = new Vector3(s, s, 1); transform.Rotate(0, 0, age * 360); if (l) l.intensity = 4 * (1 - k);
        }
    }
}
