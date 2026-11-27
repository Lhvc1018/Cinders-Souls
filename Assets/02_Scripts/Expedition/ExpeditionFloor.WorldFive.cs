using System.Linq;
using UnityEngine;
using Codice.Enemies;
namespace Codice.Expedition
{
    // Mundo 5 · La Horda de los Colmillos (pisos 13-15): empalizadas, tiendas de piel, tótems y braseros de guerra.
    public sealed partial class ExpeditionFloor
    {
        bool CampIfClear(string name, Vector3 position, float yaw, Transform parent, float half)
        {
            Physics.SyncTransforms();
            foreach (var hit in Physics.OverlapBox(position + Vector3.up * (half + .3f), new Vector3(half, half, half), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (hit.bounds.max.y > position.y + .35f) return false;
            Camp(name, position, yaw, parent); return true;
        }
        string WorldFiveName => FloorNumber == 13 ? "Foso de los Colmillos" : FloorNumber == 14 ? "Corte del Rey Trasgo" : "Altar del Berserker";
        GameObject Camp(string name, Vector3 position, float yaw, Transform parent)
        {
            var asset = Resources.Load<GameObject>("WorldFive/" + name);
            if (!asset) throw new System.InvalidOperationException("Missing WorldFive asset: " + name);
            return Instantiate(asset, position, Quaternion.Euler(0, yaw, 0), parent);
        }
        void WorldFiveBoundary(Vector3 p, int side, Transform root)
        {
            Camp("Palisade", p, side * 90, root);
            if (((Mathf.RoundToInt(p.x + p.z) + Seed) & 15) == 0)
            {
                var inner = Quaternion.Euler(0, side * 90, 0) * Vector3.back * 1.1f;
                Camp("Brazier", p + inner, 0, root); Glow(p + inner + Vector3.up * 1.9f, new Color(1f, .45f, .14f), 4.6f, 13, root);
            }
        }
        void WorldFiveRoom(int room, Transform root)
        {
            var c = Center(room); var plan = Layout.Plans[room]; var rng = new System.Random(Seed ^ room * 4111); bool boss = room == Layout.Boss;
            int clutter = 0; const int MaxClutter = 2;   // drums / barrels / crates per room: dense stacks blocked the walking lines
            if (room == Layout.MerchantRoom || room == Layout.StatueRoom) { Camp("Tent", c + new Vector3(-(plan.width * 4 - 2.5f), 0, plan.depth * 4 - 3), 135, root); return; }
            if (boss)
            {
                // War arena: ring of tusk totems and braziers around a clear fighting pit; drums behind the throne side.
                int n = FloorNumber == 15 ? 10 : 8; float radius = Mathf.Min(plan.width, plan.depth) * 4 - 3;
                for (int i = 0; i < n; i++)
                {
                    float a = (i + .5f) * Mathf.PI * 2 / n; var at = c + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
                    if (i % 2 == 0) Camp("Totem", at, -a * Mathf.Rad2Deg + 90, root); else { Camp("Brazier", at, 0, root); Glow(at + Vector3.up * 1.9f, new Color(1f, .42f, .12f), 5f, 14, root); }
                }
                foreach (int s in new[] { -1, 1 }) Camp("Drum", c + new Vector3(s * 4, 0, plan.depth * 4 - 2), 0, root);
                if (FloorNumber == 14) for (int i = -1; i <= 1; i++) Camp("Cage", c + new Vector3(i * 5, 0, -(plan.depth * 4 - 2.5f)), 0, root);
                if (FloorNumber == 15) Glow(c + Vector3.up * 8, new Color(.9f, .2f, .1f), 4, 30, root);
                return;
            }
            // Camp rooms: tents and cages along the north/south edges, totems at corners, barricades as cover off the axes.
            foreach (int row in new[] { -1, 1 }) for (int x = -(plan.width - 1); x <= plan.width - 1; x += 2)
            {
                if (Mathf.Abs(x) <= 1 || !plan.Contains(x, row * plan.depth)) continue;
                var at = c + new Vector3(x * 4, 0, row * (plan.depth * 4 - .5f));
                int pick = rng.Next(5);
                if (pick < 2) Camp("Tent", at, row < 0 ? 0 : 180, root);
                else if (pick == 2) Camp("Cage", at, 0, root);
                else if (pick == 3) Camp("Bones", at, rng.Next(360), root);
                else if (clutter < MaxClutter && CampIfClear("Drum", at, 0, root, .8f)) clutter++;
            }
            if (plan.width >= 3 && plan.depth >= 3)
            {
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) if (plan.Contains(sx * (plan.width - 1), sz * (plan.depth - 1)))
                    Camp("Totem", c + new Vector3(sx * (plan.width * 4 - 3), 0, sz * (plan.depth * 4 - 3)), rng.Next(360), root);
                for (int k = 0; k < 2; k++)
                {
                    int sx = k == 0 ? -1 : 1; var at = c + new Vector3(sx * 7, 0, (rng.Next(2) == 0 ? -1 : 1) * 6);
                    Camp("Barricade", at, rng.Next(2) * 90 + 15, root);
                }
                foreach (int s in new[] { -1, 1 }) { var fire = c + new Vector3(s * 5, 0, s * 2); Camp("Brazier", fire, 0, root); Glow(fire + Vector3.up * 1.9f, new Color(1f, .45f, .14f), 4.5f, 14, root); }
            }
            // Flanks: tents, cages, supply stacks and weapon racks along east/west palisades, off every door axis.
            foreach (int side in new[] { -1, 1 }) for (int z = -(plan.depth - 1); z <= plan.depth - 1; z++)
            {
                if (Mathf.Abs(z) <= 1 || !plan.Contains(side * plan.width, z)) continue;
                var at = c + new Vector3(side * (plan.width * 4 - .4f), 0, z * 4);
                int pick = rng.Next(6); float face = side > 0 ? -90 : 90;
                if (pick == 0) CampIfClear("Tent", at, face, root, 1.6f);
                else if (pick == 1) CampIfClear("Cage", at, 0, root, 1.1f);
                else if (pick == 2) { if (clutter < MaxClutter && KitIfClear("crates_stacked", at, face, root, .8f, new Vector3(.8f, .8f, .8f))) clutter++; }
                else if (pick == 3) { if (clutter < MaxClutter && KitIfClear("barrel_small_stack", at, face, root, .8f, new Vector3(.7f, .6f, .4f))) clutter++; }
                else if (pick == 4) { KitIfClear("sword_shield_broken", at + Vector3.up * .9f, face + 180, root, .9f, new Vector3(.5f, .5f, .2f)); Camp("Bones", at + Vector3.forward * 1.2f, rng.Next(360), root); }
                else if (clutter < MaxClutter && CampIfClear("Drum", at, 0, root, .8f)) clutter++;
            }
            // Camp heart: a cookfire circle with logs, kegs and a war banner, lit warm.
            if (plan.width >= 3 && plan.depth >= 3)
            {
                var heart = c + new Vector3((rng.Next(2) == 0 ? -1 : 1) * 8, 0, (rng.Next(2) == 0 ? -1 : 1) * 7);
                if (CampIfClear("Brazier", heart, 0, root, .7f))
                {
                    for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2 + .4f; KitIfClear(i % 2 == 0 ? "keg_decorated" : "trunk_large_A", heart + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.1f, a * Mathf.Rad2Deg, root, .5f, new Vector3(.5f, .4f, .4f)); }
                    Kit("banner_triple_red", heart + Vector3.back * 2.8f, 0, root, .7f);
                    Glow(heart + Vector3.up * 2.2f, new Color(1f, .5f, .2f), 5f, 15, root);
                }
            }
            if (IsSideRoom(room) || room == Layout.SecretRoom) { Camp("Cage", c + new Vector3(0, 0, plan.depth * 4 - 2.5f), 0, root); Camp("Bones", c + new Vector3(-4, 0, 0), 0, root); }
        }
        void WorldFiveAtmosphere(Transform root)
        {
            CemeteryAtmosphere(root);
            // Warm the ambient probe so the camp reads under the smoky sky (the cemetery base is cold and dark).
            var sh = RenderSettings.ambientProbe; sh *= 1.6f; sh.AddAmbientLight(new Color(.16f, .1f, .065f)); RenderSettings.ambientProbe = sh;
            RenderSettings.fogColor = FloorNumber == 14 ? new Color(.2f, .14f, .09f) : new Color(.22f, .11f, .07f); RenderSettings.fogDensity = .016f;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (light.type == LightType.Directional) { light.color = new Color(.98f, .7f, .52f); light.intensity = .95f; }
            if (profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var grade)) { grade.saturation.value = -6; grade.postExposure.value = 1.15f; grade.colorFilter.value = new Color(1f, .9f, .82f); }
        }
        /// The horde: raiders and wolves early, shamans and standard-bearers deeper; bruisers grow in number toward 5-3.
        BasicEnemy[] WorldFiveRoster(int room)
        {
            string[] pool = FloorNumber == 13 ? new[] { "Saqueador", "Saqueador", "Huargo", "Bruto", "Chaman" }
                : FloorNumber == 14 ? new[] { "Saqueador", "Chaman", "Portaestandarte", "Huargo", "Bruto" }
                : new[] { "Bruto", "Bruto", "Portaestandarte", "Chaman", "Huargo" };
            var rng = new System.Random(Seed ^ room * 2203);
            return pool.OrderBy(_ => rng.Next()).Take(IsSideRoom(room) ? 3 : 5).Select(n => Resources.Load<BasicEnemy>("WorldFive/Enemies/" + n)).Where(e => e).ToArray();
        }
    }
}
