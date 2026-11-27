using UnityEngine;
namespace Codice.Presentation
{
    /// Shared material for code-built particle systems. URP Particles/Unlit without a texture draws hard squares
    /// (the "green box" sewer gas, square fire columns); this gives them a soft radial sprite and proper blending.
    public static class SoftParticle
    {
        static Texture2D sprite;
        static Texture2D Sprite()
        {
            if (sprite) return sprite;
            const int N = 64; sprite = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "Soft particle", wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N]; float c = (N - 1) * .5f;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c); float a = Mathf.SmoothStep(1, 0, d); px[y * N + x] = new Color(1, 1, 1, a * a); }
            sprite.SetPixels(px); sprite.Apply(false, true); return sprite;
        }
        public static Material Make(Color color, bool additive)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = additive ? "Soft particle (add)" : "Soft particle" };
            m.SetTexture("_BaseMap", Sprite()); m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", additive ? 2 : 0); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); if (additive) m.EnableKeyword("_BLENDMODE_ADD");
            return m;
        }
            static Texture2D beam;
        /// Additive material for LineRenderer beams: soft only across the width (V), constant along the length (U),
        /// so a stretched line reads as a straight energy beam instead of a lens / spindle.
        public static Material BeamMaterial(Color color)
        {
            if (!beam)
            {
                beam = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "Beam profile", wrapMode = TextureWrapMode.Clamp }; var px = new Color[4 * 64];
                for (int y = 0; y < 64; y++) { float v = Mathf.Abs(y / 63f * 2 - 1); float a = Mathf.Exp(-v * v * 6f); for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(1, 1, 1, a); }
                beam.SetPixels(px); beam.Apply(false, true);
            }
            var m = Make(color, true); m.SetTexture("_BaseMap", beam); m.name = "Energy beam"; return m;
        }
        static Texture2D rune;
        /// Carved flame rune (ring, three tongues, base bar) for puzzle doors and seals instead of a flat square.
        public static Material RuneMaterial(Color color)
        {
            if (!rune)
            {
                const int N = 128; rune = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "Flame rune", wrapMode = TextureWrapMode.Clamp }; var px = new Color[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    var p = new Vector2(x, y) / (N - 1) * 2 - Vector2.one; float a = 0;
                    float ring = Mathf.Abs(p.magnitude - .82f); a = Mathf.Max(a, Mathf.Clamp01(1 - ring / .06f));
                    for (int k = -1; k <= 1; k++) { float cx = k * .3f, top = k == 0 ? .55f : .3f; if (p.y > -.35f && p.y < top) { float w = .07f * (1 - (p.y + .35f) / (top + .35f)) + .02f; a = Mathf.Max(a, Mathf.Clamp01(1 - Mathf.Abs(p.x - cx - .08f * Mathf.Sin(p.y * 6 + k)) / w)); } }
                    if (Mathf.Abs(p.y + .45f) < .05f && Mathf.Abs(p.x) < .5f) a = Mathf.Max(a, 1);
                    px[y * N + x] = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, a));
                }
                rune.SetPixels(px); rune.Apply(false, true);
            }
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Flame rune" };
            m.SetTexture("_BaseMap", rune); m.SetColor("_BaseColor", color); m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 2); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return m;
        }
    }
}
