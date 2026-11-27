using System.Collections.Generic;
using Codice.Enemies;
using UnityEngine;
namespace Codice.Expedition
{
    public sealed partial class ExpeditionFloor
    {
        /// Everything this floor can spawn, so first-use costs happen at load time instead of mid-fight.
        void StartPrewarm()
        {
            var set = new List<Object>();
            void Add(Object o) { if (o && !set.Contains(o)) set.Add(o); }
            try
            {
                Add(context.dungeon ? context.dungeon.expeditionPrefab : null);
                Add(context.arena ? context.arena.expeditionPrefab : null);
                Add(Resources.Load<BasicEnemy>("ExpeditionEnemies/CreepyDoll")); Add(Resources.Load<BasicEnemy>("ExpeditionEnemies/CryptSkeleton"));
                foreach (var n in new[] { "Lobo de hollin", "Salteador de la cripta", "Golem funerario", "Hondero de la cripta" }) Add(Resources.Load<BasicEnemy>("ExpeditionEnemies/MuckResidents/" + n));
                if (contentCatalog && contentCatalog.additionalEnemies != null) foreach (var e in contentCatalog.additionalEnemies) Add(e);
                for (int room = 1; room < RoomCount; room++)
                {
                    BasicEnemy[] roster = FloorNumber >= 13 ? WorldFiveRoster(room) : FloorNumber >= 10 ? WorldFourRoster(room) : FloorNumber >= 7 ? CemeteryRoster(room) : null;
                    if (roster != null) foreach (var e in roster) Add(e);
                }
                if (Layout.VaultRoom >= 0 && IsVaultBoss(Layout.VaultRoom)) Add(Resources.Load<GameObject>("Secrets/Bosses/" + SecretBossFor(FloorNumber)));
            }
            catch (System.Exception e) { Debug.LogWarning("[Prewarm] roster scan: " + e.Message); }
            // One dormant copy of each enemy (inactive parent: no Awake, no AI) so the first real spawn is cheap.
            var dormant = new GameObject("Prewarm dormant enemies"); dormant.SetActive(false); dormant.transform.SetParent(transform);
            var copies = new List<GameObject>();
            foreach (var o in set) if (o is BasicEnemy be) copies.Add(Instantiate(be, dormant.transform).gameObject);
            // Wake them far below the level with AI and navigation off: Awake (model setup, presentation) runs now, at load,
            // instead of in the first combat room. They never tick (component disabled) and vanish after a second.
            var awake = new GameObject("Prewarm awake enemies"); awake.transform.position = new Vector3(0, -900, 0);
            foreach (var c in copies)
            {
                foreach (var b in c.GetComponents<BasicEnemy>()) b.enabled = false;
                foreach (var a in c.GetComponents<UnityEngine.AI.NavMeshAgent>()) a.enabled = false;
                foreach (var col in c.GetComponentsInChildren<Collider>(true)) col.enabled = false;
                c.transform.SetParent(awake.transform, false);
            }
            Destroy(dormant); Destroy(awake, 1f);
            Codice.Presentation.Prewarm.Run(set);
        }
    }
}
