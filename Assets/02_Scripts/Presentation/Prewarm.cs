using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Codice.Presentation
{
    /// Moves first-use costs (asset loading, shader/PSO compilation, particle materials) to the floor's
    /// loading moment instead of the first combat: every material of the enemies, boss and common VFX of
    /// this floor is drawn once, tiny and in front of the camera, for a couple of frames.
    public sealed class Prewarm : MonoBehaviour
    {
        static readonly HashSet<string> warmedPaths = new HashSet<string>();
        readonly List<(Mesh mesh, Material mat)> draws = new List<(Mesh, Material)>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() => warmedPaths.Clear();
        static readonly string[] CommonFolders = { "CindersVfx", "CindersVfx/Impacts", "SpellEffects" };
        public static void Run(IEnumerable<Object> prefabs)
        {
            var go = new GameObject("Prewarm (first-use costs)"); var p = go.AddComponent<Prewarm>();
            var seen = new HashSet<Material>(); Mesh quad = null;
            void Add(GameObject g)
            {
                if (!g) return;
                foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh = r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>() ? r.GetComponent<MeshFilter>().sharedMesh : null;
                    if (!mesh) { if (!quad) quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx"); mesh = quad; }
                    foreach (var m in r.sharedMaterials) if (m && seen.Add(m)) p.draws.Add((mesh, m));
                    if (r is ParticleSystemRenderer pr && pr.trailMaterial && seen.Add(pr.trailMaterial)) p.draws.Add((quad ? quad : quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx"), pr.trailMaterial));
                }
            }
            foreach (var o in prefabs) Add(o is GameObject g ? g : o is Component c ? c.gameObject : null);
            foreach (var folder in CommonFolders)
                if (warmedPaths.Add(folder)) foreach (var g in Resources.LoadAll<GameObject>(folder)) Add(g);
            Debug.Log($"[Prewarm] {p.draws.Count} materials queued");
        }
        int frames;
        void LateUpdate()
        {
            var cam = Camera.main; if (!cam) { if (++frames > 10) Destroy(gameObject); return; }
            var basePos = cam.transform.position + cam.transform.forward * (cam.nearClipPlane + .5f);
            var m = Matrix4x4.TRS(basePos, cam.transform.rotation, Vector3.one * .0005f);
            foreach (var (mesh, mat) in draws)
                for (int sub = 0; sub < Mathf.Max(1, Mathf.Min(mesh.subMeshCount, 4)); sub++) Graphics.DrawMesh(mesh, m, mat, 0, cam, sub, null, false, false, false);
            if (++frames >= 3) Destroy(gameObject);
        }
    }
}
