using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Codice.Expedition
{
    /// Visual pass per biome, layered on top of each world's atmosphere: split-toned grade (cold shadows / warm fire),
    /// stronger contrast, coloured vignette, bloom tuned for flames, flickering fire lights and real shadows from the
    /// few lights nearest the player (LightShadowDirector). One row per world in Looks.
    public sealed partial class ExpeditionFloor
    {
        struct Look
        {
            public float contrast, saturation, temperature, tint, exposure, vignette, bloom, bloomThreshold;
            public Color shadows, highlights, vignetteColor, lift, gain;
        }
        static Look LookFor(int floor)
        {
            if (floor <= 3) return new Look { contrast = 18, saturation = -8, temperature = 3, tint = 0, exposure = .1f, vignette = .34f, bloom = .32f, bloomThreshold = 1f,
                shadows = new Color(.36f, .46f, .58f), highlights = new Color(1f, .74f, .46f), vignetteColor = new Color(.05f, .03f, .02f), lift = new Color(.98f, .99f, 1.03f, -.02f), gain = new Color(1.04f, 1f, .94f, .02f) };
            if (floor <= 6) return new Look { contrast = 16, saturation = -10, temperature = -6, tint = -4, exposure = .05f, vignette = .36f, bloom = .3f, bloomThreshold = 1f,
                shadows = new Color(.3f, .52f, .5f), highlights = new Color(.98f, .86f, .62f), vignetteColor = new Color(.01f, .04f, .035f), lift = new Color(.97f, 1.02f, 1.01f, -.02f), gain = new Color(1.02f, 1f, .95f, .02f) };
            if (floor <= 9) return new Look { contrast = 20, saturation = -14, temperature = -12, tint = 2, exposure = 0, vignette = .32f, bloom = .26f, bloomThreshold = 1.05f,
                shadows = new Color(.32f, .4f, .62f), highlights = new Color(.9f, .92f, 1f), vignetteColor = new Color(.01f, .02f, .05f), lift = new Color(.96f, .99f, 1.05f, -.03f), gain = new Color(1f, 1f, 1.03f, .03f) };
            if (floor <= 12) return new Look { contrast = 22, saturation = -2, temperature = 10, tint = 3, exposure = .05f, vignette = .38f, bloom = .45f, bloomThreshold = .95f,
                shadows = new Color(.42f, .3f, .4f), highlights = new Color(1f, .62f, .32f), vignetteColor = new Color(.08f, .02f, .01f), lift = new Color(1.02f, .98f, .98f, -.03f), gain = new Color(1.06f, .98f, .9f, .02f) };
            return new Look { contrast = 15, saturation = 2, temperature = 8, tint = 4, exposure = 0, vignette = .3f, bloom = .38f, bloomThreshold = 1f,
                shadows = new Color(.44f, .34f, .52f), highlights = new Color(1f, .76f, .5f), vignetteColor = new Color(.06f, .02f, .02f), lift = new Color(1.01f, .98f, 1.01f, -.02f), gain = new Color(1.04f, 1f, .94f, .02f) };
        }
        void ApplyLook(Transform root)
        {
            var look = LookFor(FloorNumber);
            var readableProbe=RenderSettings.ambientProbe;readableProbe.AddAmbientLight(new Color(.08f,.09f,.105f));RenderSettings.ambientProbe=readableProbe;
            ColorAdjustments grade = null; if (profile.TryGet<ColorAdjustments>(out grade)) { grade.contrast.value = look.contrast * .4f; grade.saturation.value += look.saturation + 6; grade.postExposure.value += look.exposure + .65f; /* SSAO and stronger contrast take light away */ }
            if (profile.TryGet<WhiteBalance>(out var wb)) { wb.temperature.value = look.temperature; wb.tint.value = look.tint; }
            if (profile.TryGet<Vignette>(out var vignette)) { vignette.intensity.value = Mathf.Max(.16f,look.vignette - .12f); vignette.color.value = look.vignetteColor; vignette.smoothness.value = .55f; vignette.rounded.value = true; }
            if (profile.TryGet<Bloom>(out var bloom)) { bloom.intensity.value = look.bloom * .7f; bloom.threshold.value = Mathf.Max(1.2f,look.bloomThreshold); bloom.scatter.value = .45f; bloom.tint.value = new Color(1f, .9f, .78f); }
            // Gentle biome tint on the grade itself (split toning / lift-gain washed the frame in colour).
            if (grade) grade.colorFilter.value = grade.colorFilter.value * Color.Lerp(Color.white, Color.Lerp(look.shadows, look.highlights, .5f), .12f);
            var chroma = profile.Add<ChromaticAberration>(true); chroma.intensity.value = 0;
            // Fire lights breathe; the few nearest the player cast soft shadows.
            foreach (var l in root.GetComponentsInChildren<Light>(true))
            {
                if (l.type != LightType.Point) continue;
                bool fire = l.color.r > l.color.b * 1.4f && l.color.r > .6f;
                if (fire && !l.GetComponent<FireFlicker>()) l.gameObject.AddComponent<FireFlicker>();
            }
            AmbientMotes(root);
            if (!root.GetComponent<LightShadowDirector>()) root.gameObject.AddComponent<LightShadowDirector>().player = context.player.transform;
        }
    }
    public sealed partial class ExpeditionFloor
    {
        /// Air detail per biome, one slow particle volume per room: dust in the crypt, damp mist in the sewers, low
        /// fog in the cemetery, rising embers in the forge, drifting ash over the war camp.
        void AmbientMotes(Transform root)
        {
            int world = Mathf.Clamp((FloorNumber - 1) / 3, 0, 4);
            Color[] tint = { new Color(1f, .9f, .75f, .35f), new Color(.7f, .9f, .85f, .22f), new Color(.75f, .8f, .9f, .08f), new Color(3f, 1.2f, .3f, 1f), new Color(.6f, .55f, .5f, .4f) };
            bool additive = world == 3; var mat = Codice.Presentation.SoftParticle.Make(tint[world], additive);
            for (int room = 0; room < RoomCount; room++)
            {
                var plan = Layout.Plans[room]; var c = Center(room);
                var go = new GameObject("Ambient motes"); go.transform.SetParent(root); go.transform.position = c + Vector3.up * (world == 2 ? .6f : 2.2f);
                var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = new ParticleSystem.MinMaxCurve(6, 10); m.startSpeed = 0; m.maxParticles = world == 2 ? 40 : 70;
                m.startSize = world == 2 ? new ParticleSystem.MinMaxCurve(3.5f, 6.5f) : world == 3 ? new ParticleSystem.MinMaxCurve(.03f, .07f) : new ParticleSystem.MinMaxCurve(.04f, .1f);
                m.simulationSpace = ParticleSystemSimulationSpace.World; m.cullingMode = ParticleSystemCullingMode.Pause;
                var e = ps.emission; e.rateOverTime = world == 2 ? 5 : 9;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(plan.width * 8, world == 2 ? .6f : 3.5f, plan.depth * 8);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
                v.x = new ParticleSystem.MinMaxCurve(-.08f, .08f); v.z = new ParticleSystem.MinMaxCurve(-.08f, .08f); v.y = world == 3 ? new ParticleSystem.MinMaxCurve(.25f, .6f) : world == 4 ? new ParticleSystem.MinMaxCurve(-.15f, -.05f) : new ParticleSystem.MinMaxCurve(-.03f, .05f);
                var n = ps.noise; n.enabled = true; n.strength = .25f; n.frequency = .3f; n.scrollSpeed = .2f; n.quality = ParticleSystemNoiseQuality.Low;
                var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .25f), new GradientAlphaKey(1, .7f), new GradientAlphaKey(0, 1) }); col.color = g;
                var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                ps.Play();
            }
        }
    }
    /// Fire-light breathing: smooth noise on intensity (±18 %) and a slight range sway. No allocations.
    public sealed class FireFlicker : MonoBehaviour
    {
        Light l; float baseIntensity, baseRange, seed;
        void Start() { l = GetComponent<Light>(); baseIntensity = l.intensity; baseRange = l.range; seed = Random.value * 100; }
        void Update()
        {
            if (!l) return; float t = Time.time * 7.5f + seed;
            float n = Mathf.PerlinNoise(t, seed) * .7f + Mathf.PerlinNoise(t * 2.7f, seed + 3) * .3f;
            l.intensity = baseIntensity * (.82f + .36f * n); l.range = baseRange * (.96f + .08f * n);
        }
        public void Rebase() { if (l) { baseIntensity = l.intensity; baseRange = l.range; } }
    }
    /// Gives real soft shadows to the 3 point lights closest to the player (re-picked every 0.4 s); the rest stay
    /// shadowless so the additional-light shadow atlas holds only a few cube maps.
    public sealed class LightShadowDirector : MonoBehaviour
    {
        public Transform player; const int Count = 1; const float MaxDistance = 14;
        /// Point-light shadows render 6 faces each: one light only, and none at all if the game runs under 50 fps.
        float fpsAvg = 60; bool disabled;
        readonly List<Light> lights = new List<Light>(); readonly Light[] chosen = new Light[Count]; float next;
        void Start() { foreach (var l in GetComponentsInChildren<Light>(true)) if (l.type == LightType.Point && l.range >= 6) lights.Add(l); }
        void Update()
        {
            if (Time.unscaledDeltaTime > 0) fpsAvg = Mathf.Lerp(fpsAvg, 1f / Time.unscaledDeltaTime, .02f);
            if (!disabled && fpsAvg < 50 && Time.realtimeSinceStartup > 8) { disabled = true; foreach (var l in lights) if (l) l.shadows = LightShadows.None; }
            if (disabled || !player || Time.time < next) return; next = Time.time + .4f;
            for (int i = 0; i < Count; i++) chosen[i] = null;
            var p = player.position; var best = new float[Count]; for (int i = 0; i < Count; i++) best[i] = MaxDistance * MaxDistance;
            foreach (var l in lights)
            {
                if (!l || !l.isActiveAndEnabled) continue; float d = (l.transform.position - p).sqrMagnitude;
                for (int i = 0; i < Count; i++) if (d < best[i]) { for (int k = Count - 1; k > i; k--) { best[k] = best[k - 1]; chosen[k] = chosen[k - 1]; } best[i] = d; chosen[i] = l; break; }
            }
            foreach (var l in lights)
            {
                if (!l) continue; bool on = System.Array.IndexOf(chosen, l) >= 0;
                var want = on ? LightShadows.Soft : LightShadows.None;
                if (l.shadows != want) { l.shadows = want; if (on) { l.shadowStrength = .85f; l.shadowBias = .04f; l.shadowNormalBias = .3f; l.shadowNearPlane = .2f; } }
            }
        }
    }
}
