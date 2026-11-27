using System.Linq;
using UnityEngine;
namespace Codice.Presentation
{
    // El Vigía: an ash-grey penitent with a lantern and a broken halo, built over the gothic humanoid instead of the toy mage.
    // El Vigía: the first sentinel. Ash-grey plate under a travelling cloak, a crown of votive candles,
    // a blindfold, a tall lantern pole and the tags of every name he has counted.
    public static class VigiaLook
    {
        static Material iron, wax, flame, glass, parchment, blind, ember;
        static Transform body; static float k;
        public static GameObject Dress(GameObject npc)
        {
            if (!npc || npc.transform.Find("El Vigía · centinela de las velas")) return null;
            var prefab = Resources.Load<GameObject>("Presentation/Classes/Tank");
            if (!prefab) return null;
            foreach (var r in npc.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var a in npc.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            var go = Object.Instantiate(prefab, npc.transform, false); go.name = "El Vigía · centinela de las velas"; body = go.transform;
            body.localPosition = Vector3.zero; body.localRotation = Quaternion.identity;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            var skin = go.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault();
            if (skin) body.localScale *= 2.02f / Mathf.Max(.1f, skin.bounds.size.y) / Mathf.Max(.01f, npc.transform.lossyScale.y);
            k = body.lossyScale.y;
            // Ash over steel: armour goes cold grey, the cloak keeps dried crimson.
            var block = new MaterialPropertyBlock();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true; bool cloth = r.name.Contains("Capa") || r.sharedMaterials.Any(m => m && (m.name.Contains("Paño") || m.name.Contains("Cloth") || m.name.Contains("Cape")));
                r.GetPropertyBlock(block); block.SetColor("_BaseColor", cloth ? new Color(.30f, .13f, .12f) : new Color(.40f, .40f, .42f)); r.SetPropertyBlock(block);
            }
            if (!iron)
            {
                iron = Lit(new Color(.07f, .065f, .06f), Color.black, .8f); wax = Lit(new Color(.82f, .76f, .62f), new Color(.12f, .08f, .03f), 0);
                flame = Lit(new Color(1, .7f, .3f), new Color(5.5f, 2.6f, .6f), 0); glass = Lit(new Color(.9f, .55f, .2f), new Color(4.5f, 1.9f, .5f), 0);
                parchment = Lit(new Color(.72f, .66f, .5f), Color.black, 0); blind = Lit(new Color(.36f, .04f, .05f), Color.black, 0); ember = Lit(new Color(.3f, .1f, .03f), new Color(2.2f, .8f, .15f), 0);
            }
            // Candle crown: iron band and seven votive candles of uneven height.
            Piece("B-head", PrimitiveType.Cylinder, new Vector3(0, .2f, 0), Vector3.zero, new Vector3(.3f, .012f, .3f), iron, "Corona de hierro");
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51.4f * Mathf.Deg2Rad, h = .07f + (i * 37 % 5) * .022f; var at = new Vector3(Mathf.Sin(a) * .135f, .21f, Mathf.Cos(a) * .135f);
                Piece("B-head", PrimitiveType.Cylinder, at + Vector3.up * h * .5f, Vector3.zero, new Vector3(.03f, h * .5f, .03f), wax, "Vela");
                Piece("B-head", PrimitiveType.Sphere, at + Vector3.up * (h + .022f), Vector3.zero, new Vector3(.02f, .045f, .02f), flame, "Llama");
                Piece("B-head", PrimitiveType.Sphere, at + new Vector3(0, -.01f, 0) + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * .012f, Vector3.zero, new Vector3(.022f, .05f, .022f), wax, "Cera derramada");
            }
            // Blindfold with two tails.
            Piece("B-head", PrimitiveType.Cylinder, new Vector3(0, .1f, .005f), Vector3.zero, new Vector3(.262f, .025f, .262f), blind, "Venda");
            for (int i = 0; i < 2; i++) Piece("B-head", PrimitiveType.Cube, new Vector3(-.05f + i * .06f, -.02f - i * .03f, -.14f), new Vector3(14, 0, i * 12 - 4), new Vector3(.035f, .2f + i * .06f, .006f), blind, "Venda cola");
            // Candles melted on the left pauldron.
            for (int i = 0; i < 4; i++)
            {
                float h = .06f + (i * 3 % 4) * .03f; var at = new Vector3(-.2f - i * .035f, .1f - i * .012f, -.02f + (i % 2) * .05f);
                Piece("B-shoulder.L", PrimitiveType.Cylinder, at + Vector3.up * h * .5f, Vector3.zero, new Vector3(.028f, h * .5f, .028f), wax, "Vela de hombro");
                Piece("B-shoulder.L", PrimitiveType.Sphere, at + Vector3.up * (h + .02f), Vector3.zero, new Vector3(.018f, .04f, .018f), flame, "Llama");
            }
            // Chain of names across the chest, each tag a pilgrim.
            Piece("B-chest", PrimitiveType.Cube, new Vector3(0, .06f, .19f), new Vector3(0, 0, -32), new Vector3(.5f, .018f, .018f), iron, "Cadena de nombres");
            for (int i = 0; i < 7; i++)
            {
                float t = -.2f + i * .066f; var at = new Vector3(t, .06f - t * .62f - .05f - (i % 3) * .012f, .2f);
                Piece("B-chest", PrimitiveType.Cube, at, new Vector3(0, 0, (i % 3 - 1) * 9), new Vector3(.034f, .08f + (i % 2) * .03f, .004f), parchment, "Nombre");
            }
            // Ledger of the counted at the hip, tally marks burnt into the tabard.
            Piece("B-hips", PrimitiveType.Cube, new Vector3(.24f, -.1f, .02f), new Vector3(0, 0, -8), new Vector3(.07f, .24f, .18f), iron, "Libro de los contados");
            Piece("B-hips", PrimitiveType.Cube, new Vector3(.243f, -.1f, .028f), new Vector3(0, 0, -8), new Vector3(.055f, .22f, .18f), parchment, "Hojas");
            Piece("B-hips", PrimitiveType.Cube, new Vector3(0, -.28f, .2f), new Vector3(-4, 0, 0), new Vector3(.2f, .52f, .012f), blind, "Tabardo");
            for (int i = 0; i < 9; i++) Piece("B-hips", PrimitiveType.Cube, new Vector3(-.06f + (i % 5) * .03f, -.16f - (i / 5) * .14f, .208f), new Vector3(0, 0, i % 5 == 4 ? 60 : 0), new Vector3(.006f, i % 5 == 4 ? .14f : .09f, .004f), ember, "Marca de cuenta");
            var animator = go.GetComponentInChildren<Animator>(true);
            if (animator)
            {
                animator.enabled = true; animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Presentation/Human/HumanPlayer");
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (Application.isPlaying && animator.runtimeAnimatorController && animator.HasState(0, Animator.StringToHash("StaffIdle"))) animator.Play("StaffIdle");
            }
            LanternPole(Find("B-hand.R"));
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = npc.layer;
            return go;
        }
        static Transform Find(string name) => body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        static Material Lit(Color color, Color emission, float metal)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", metal > 0 ? .5f : .2f);
            if (emission.maxColorComponent > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission); }
            return m;
        }
        // Offsets, angles and sizes are authored in metres in the character's own frame (rest pose).
        static GameObject Piece(string bone, PrimitiveType type, Vector3 offset, Vector3 euler, Vector3 size, Material material, string name) => Piece(Find(bone), type, offset, euler, size, material, name);
        static GameObject Piece(Transform parent, PrimitiveType type, Vector3 offset, Vector3 euler, Vector3 size, Material material, string name)
        {
            if (!parent) return null;
            var go = GameObject.CreatePrimitive(type); go.name = name; var col = go.GetComponent<Collider>(); if (col) { if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col); }
            go.transform.SetParent(parent, true); go.transform.position = parent.position + body.rotation * (offset * k); go.transform.rotation = body.rotation * Quaternion.Euler(euler);
            go.transform.localScale = size * k / Mathf.Max(.001f, parent.lossyScale.x); go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static void LanternPole(Transform hand)
        {
            if (!hand) return;
            var root = new GameObject("Farol de vigilia").transform; root.SetParent(body, false); root.position = hand.position; root.rotation = body.rotation;
            Piece(root, PrimitiveType.Cylinder, new Vector3(0, .15f, 0), Vector3.zero, new Vector3(.03f, 1.15f, .03f), iron, "Asta");
            Piece(root, PrimitiveType.Cube, new Vector3(0, 1.28f, .12f), Vector3.zero, new Vector3(.025f, .025f, .28f), iron, "Brazo");
            Piece(root, PrimitiveType.Cylinder, new Vector3(0, 1.2f, .24f), Vector3.zero, new Vector3(.01f, .07f, .01f), iron, "Cadena");
            Piece(root, PrimitiveType.Cube, new Vector3(0, 1.12f, .24f), Vector3.zero, new Vector3(.16f, .02f, .16f), iron, "Tapa");
            var core = Piece(root, PrimitiveType.Cube, new Vector3(0, 1.0f, .24f), Vector3.zero, new Vector3(.1f, .2f, .1f), glass, "Llama cautiva");
            Piece(root, PrimitiveType.Cube, new Vector3(0, .89f, .24f), Vector3.zero, new Vector3(.16f, .02f, .16f), iron, "Base");
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 }) Piece(root, PrimitiveType.Cube, new Vector3(x * .065f, 1.0f, .24f + z * .065f), Vector3.zero, new Vector3(.012f, .22f, .012f), iron, "Barrote");
            for (int i = 0; i < 3; i++) Piece(root, PrimitiveType.Cube, new Vector3(.02f, .7f - i * .12f, .02f), new Vector3(0, 0, i * 20 - 20), new Vector3(.03f, .1f, .004f), parchment, "Nombre atado");
            root.gameObject.AddComponent<VigiaLantern>().Follow(hand, body);
            var light = core.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .62f, .3f); light.range = 7.5f; light.intensity = 3f; light.shadows = LightShadows.None;
        }
    }
    // The lantern hangs from the hand in every pose instead of following the wrist rotation.
    public sealed class VigiaLantern : MonoBehaviour
    {
        Transform hand, owner;
        public void Follow(Transform grip, Transform body) { hand = grip; owner = body; }
        void LateUpdate() { if (!hand || !owner) return; transform.SetPositionAndRotation(hand.position, Quaternion.Euler(0, owner.eulerAngles.y, 0)); }
    }
}
