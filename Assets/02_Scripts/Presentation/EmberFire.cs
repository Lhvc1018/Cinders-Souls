using UnityEngine;
namespace Codice.Presentation
{
    /// Torch / brazier fire. The old version was one particle system with a trapezoid shader, offset from the torch:
    /// it read as a flat yellow card floating beside the bracket. Now the flame sits on the torch head (found from the
    /// model bounds, biased toward the room) and is built from four layers: hot additive core, licking outer flame with
    /// turbulence, rising embers and a faint smoke wisp. All particles use the soft radial sprite.
    public static class EmberFire
    {
        static Material material, core, flame, ember, smoke;
        public static Material SharedMaterial {get{if(!material)material=new Material(Resources.Load<Shader>("Presentation/EmberFlame")){name="Living ember flame"};return material;}}
        static void Materials()
        {
            if (core) return;
            core = SoftParticle.Make(new Color(4f, 2.6f, 1.1f, 1f), true);   // HDR: the core must outshine the lit wall and feed bloom
            flame = SoftParticle.Make(new Color(3f, 1.1f, .25f, .9f), true);
            ember = SoftParticle.Make(new Color(5f, 2f, .5f, 1f), true);
            smoke = SoftParticle.Make(new Color(.12f, .1f, .09f, .35f), false);
        }
        /// Flame head of a mounted torch: top of its bounds, pushed toward `hint` (a point in front of the wall).
        public static Vector3 Head(Transform torch, Vector3 hint)
        {
            var rs = torch.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return hint;
            var b = rs[0].bounds; foreach (var r in rs) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
            var out_ = Vector3.ProjectOnPlane(hint - torch.position, Vector3.up); out_ = out_.sqrMagnitude > .0001f ? out_.normalized : torch.forward;
            float reach = Mathf.Max(b.extents.x, b.extents.z) * .55f;
            return new Vector3(b.center.x, b.max.y - .04f, b.center.z) + out_ * reach;
        }
        public static void Attach(Transform parent, Vector3 position)
        {
            Materials();
            var at = Head(parent, position);
            var root = new GameObject("Living torch flame"); root.transform.SetParent(parent, false); root.transform.position = at; root.transform.rotation = Quaternion.identity;
            Layer(root.transform, "Core", core, rate: 40, life: new Vector2(.18f, .3f), size: new Vector2(.16f, .24f), speed: new Vector2(.25f, .45f), radius: .025f, noise: .12f, shrink: true);
            Layer(root.transform, "Flame", flame, rate: 34, life: new Vector2(.25f, .45f), size: new Vector2(.22f, .36f), speed: new Vector2(.35f, .6f), radius: .04f, noise: .22f, shrink: true);
            Layer(root.transform, "Embers", ember, rate: 3.5f, life: new Vector2(.8f, 1.5f), size: new Vector2(.015f, .035f), speed: new Vector2(.6f, 1.3f), radius: .06f, noise: 1.2f, shrink: false);
            var s = Layer(root.transform, "Smoke", smoke, rate: 2.5f, life: new Vector2(1.4f, 2.2f), size: new Vector2(.25f, .4f), speed: new Vector2(.3f, .5f), radius: .05f, noise: .4f, shrink: false);
            s.transform.localPosition = Vector3.up * .25f; var sz = s.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .6f, 1, 1.8f));
        }
        static ParticleSystem Layer(Transform parent, string name, Material mat, float rate, Vector2 life, Vector2 size, Vector2 speed, float radius, float noise, bool shrink)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y); m.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y); m.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f); m.maxParticles = 48; m.simulationSpace = ParticleSystemSimulationSpace.World; m.cullingMode = ParticleSystemCullingMode.PauseAndCatchup; m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var e = ps.emission; e.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6; sh.radius = radius; go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var n = ps.noise; n.enabled = true; n.strength = noise; n.frequency = 1.6f; n.scrollSpeed = 1.2f; n.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, .95f, .75f), 0), new GradientColorKey(new Color(1f, .55f, .18f), .35f), new GradientColorKey(new Color(.7f, .12f, .03f), 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(.6f, .6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            if (shrink) { var so = ps.sizeOverLifetime; so.enabled = true; so.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, .15f)); }
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.sortingFudge = name == "Core" ? -2 : 0;
            ps.Play(); return ps;
        }
    }
}
