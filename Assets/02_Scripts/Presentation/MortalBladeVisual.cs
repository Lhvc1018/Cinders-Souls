using UnityEngine;
namespace Codice.Presentation
{
    // Lengthens the held katana into a crimson "mortal blade" for the duration of the special.
    public sealed class MortalBladeVisual : MonoBehaviour
    {
        Vector3 scale; Renderer[] renderers; Material[][] originals; TrailRenderer trail; GameObject aura;
        static Material crimson;
        public static MortalBladeVisual Begin(Transform blade)
        {
            if (!blade) return null;
            var v = blade.GetComponent<MortalBladeVisual>(); if (v) return v;
            v = blade.gameObject.AddComponent<MortalBladeVisual>(); v.Setup(); return v;
        }
        void Setup()
        {
            scale = transform.localScale;
            transform.localScale = new Vector3(scale.x * 1.15f, scale.y * 1.65f, scale.z * 1.15f);
            if (!crimson) { crimson = new Material(Shader.Find("Universal Render Pipeline/Lit")); crimson.SetColor("_BaseColor", new Color(.12f, .02f, .02f)); crimson.EnableKeyword("_EMISSION"); crimson.SetColor("_EmissionColor", new Color(2.6f, .18f, .08f)); crimson.SetFloat("_Smoothness", .8f); crimson.SetFloat("_Metallic", .9f); }
            renderers = GetComponentsInChildren<Renderer>(); originals = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++) { originals[i] = renderers[i].sharedMaterials; var m = new Material[originals[i].Length]; for (int k = 0; k < m.Length; k++) m[k] = crimson; renderers[i].sharedMaterials = m; }
            var tip = new GameObject("Mortal blade tip"); tip.transform.SetParent(transform, false); tip.transform.localPosition = Vector3.up * .9f;
            trail = tip.AddComponent<TrailRenderer>(); trail.time = .22f; trail.startWidth = .16f; trail.endWidth = 0; trail.minVertexDistance = .02f;
            trail.sharedMaterial = Resources.Load<Material>("Presentation/LordsVfx/Slash") ?? crimson;
            trail.startColor = new Color(1, .2f, .1f, .9f); trail.endColor = new Color(.4f, 0, 0, 0);
            aura = ImportedAttackVfx.Burst(transform.position, true, .2f); if (aura) aura.transform.SetParent(transform, true);
        }
        bool ended;
        public void End()
        {
            if (ended) return; ended = true;
            transform.localScale = scale;
            for (int i = 0; i < renderers.Length; i++) if (renderers[i]) renderers[i].sharedMaterials = originals[i];
            if (trail) Destroy(trail.gameObject); if (aura) Destroy(aura);
            Destroy(this);
        }
        void OnDisable() { if (renderers != null) End(); }
    }
}
