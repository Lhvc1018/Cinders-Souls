using UnityEngine;
namespace Codice.Expedition
{
    // Combat seals: every corridor entrance of a room gets an iron portcullis with a burning rune line. It drops when
    // the encounter starts (players cannot leave, enemies cannot be kited out) and rises only when the room is cleared.
    public sealed partial class ExpeditionFloor
    {
        static Material sealIron, sealRune;
        void DressSeal(GameObject gate)
        {
            if (!sealIron) { sealIron = new Material(Shader.Find("Universal Render Pipeline/Lit")); sealIron.SetColor("_BaseColor", new Color(.12f, .1f, .09f)); sealIron.SetFloat("_Metallic", .85f); sealIron.SetFloat("_Smoothness", .35f); }
            if (!sealRune) { sealRune = new Material(Shader.Find("Universal Render Pipeline/Lit")); sealRune.SetColor("_BaseColor", new Color(.5f, .05f, .03f)); sealRune.EnableKeyword("_EMISSION"); sealRune.SetColor("_EmissionColor", new Color(4f, .5f, .15f)); }
            var bars = new GameObject("Rastrillo de ceniza"); bars.transform.SetParent(gate.transform, false);
            for (int i = 0; i < 9; i++)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(bar.GetComponent<Collider>()); bar.name = "Barrote";
                bar.transform.SetParent(bars.transform, false); bar.transform.localPosition = new Vector3(-1.8f + i * .45f, 2.25f, 0); bar.transform.localScale = new Vector3(.11f, 2.25f, .11f);
                bar.GetComponent<Renderer>().sharedMaterial = sealIron;
            }
            foreach (float y in new[] { .9f, 2.6f, 4.2f })
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(band.GetComponent<Collider>()); band.name = "Travesaño";
                band.transform.SetParent(bars.transform, false); band.transform.localPosition = new Vector3(0, y, 0); band.transform.localScale = new Vector3(4f, .14f, .16f); band.GetComponent<Renderer>().sharedMaterial = sealIron;
            }
            var rune = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(rune.GetComponent<Collider>()); rune.name = "Runa del sello";
            rune.transform.SetParent(bars.transform, false); rune.transform.localPosition = new Vector3(0, .06f, 0); rune.transform.localScale = new Vector3(4f, .05f, .5f); rune.GetComponent<Renderer>().sharedMaterial = sealRune;
            var light = new GameObject("Brillo del sello").AddComponent<Light>(); light.transform.SetParent(bars.transform, false); light.transform.localPosition = Vector3.up * 1.2f;
            light.type = LightType.Point; light.color = new Color(1f, .3f, .1f); light.range = 5; light.intensity = 1.6f; light.shadows = LightShadows.None;
            var wall = gate.AddComponent<BoxCollider>(); wall.size = new Vector3(4f, 4.5f, .45f); wall.center = new Vector3(0, 2.25f, 0);
            gate.AddComponent<SealDrop>().bars = bars.transform;
        }
    }
    /// Drops the portcullis when the seal activates and plays the seal sound.
    public sealed class SealDrop : MonoBehaviour
    {
        public Transform bars; float t;
        void OnEnable() { t = 0; if (bars) bars.localPosition = Vector3.up * 4.2f; if (Time.timeSinceLevelLoad > 2) Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Seal); }
        void OnDisable() { if (Time.timeSinceLevelLoad > 2 && gameObject.scene.isLoaded) Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Seal); }
        void Update() { if (!bars || t >= 1) return; t = Mathf.Min(1, t + Time.deltaTime / .35f); bars.localPosition = Vector3.up * 4.2f * (1 - t * t); }
    }
}
