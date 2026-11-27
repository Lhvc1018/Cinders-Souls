using UnityEngine;
namespace Codice.Expedition
{
    /// Corridor dressing: long connecting passages get rhythm (props against the walls, small lights,
    /// and on long runs a timed flame vent) so they never read as empty tubes. The walking line in the
    /// middle of the corridor is always kept clear; everything is deterministic from the seed (coop-safe).
    public sealed partial class ExpeditionFloor
    {
        static readonly string[] CorridorProps = { "barrel_small_stack", "crates_stacked", "candle_triple", "rubble_half", "trunk_medium_B", "sword_shield_broken", "shelf_small_candles", "box_stacked" };
        void DecorateCorridors(Transform root)
        {
            for (int k = 0; k < Layout.Corridors.Count; k++)
            {
                var link = Layout.Links[k];
                if (link.y == Layout.VaultRoom || link.y == Layout.SecretRoom) continue;   // hidden passages stay bare
                var path = Layout.Corridors[k];
                var run = new System.Collections.Generic.List<int>();
                for (int j = 1; j < path.Count - 1; j++) if (Layout.RoomAt(new Vector3(path[j].x * 4, 0, path[j].y * 4)) < 0) run.Add(j);
                if (run.Count < 3) continue;
                var rng = new System.Random(Seed ^ k * 7919 ^ 0xC0DE);
                for (int r = 0; r < run.Count; r++)
                {
                    int j = run[r]; var cell = path[j]; var dir = path[j + 1] - path[j - 1];
                    if (dir.x != 0 && dir.y != 0) continue;   // corners stay clear for turning
                    var along = new Vector3(Mathf.Sign(dir.x), 0, Mathf.Sign(dir.y)); var across = new Vector3(along.z, 0, -along.x);
                    var p = new Vector3(cell.x * 4, Layout.LevelAt(cell), cell.y * 4);
                    if (Layout.Levels.TryGetValue(cell, out var lvl) && lvl.Stair) continue;   // never dress stairs
                    float side = rng.Next(2) == 0 ? -1 : 1;
                    if (r % 3 == 1)
                    {
                        var name = CorridorProps[rng.Next(CorridorProps.Length)];
                        KitIfClear(name, p + across * side * 1.45f, Quaternion.LookRotation(-across * side).eulerAngles.y, root, .8f, new Vector3(.45f, .5f, .45f));
                    }
                    else if (r % 3 == 2 && FloorNumber >= 4)
                    {
                        // A visible source: a candle cluster at the foot of the wall, lit low and warm (no sourceless "flashlight" spots).
                        var spot = p + across * -side * 1.55f;
                        if (KitIfClear("candle_triple", spot, rng.Next(360), root, 1f, new Vector3(.3f, .3f, .3f)))
                            Glow(spot + Vector3.up * .7f, FloorNumber >= 7 && FloorNumber <= 9 ? new Color(.75f, .8f, .7f) : new Color(1f, .58f, .3f), 1.1f, 5, root);
                    }
                }
                // Long passages get one light trap in the middle, in the biome's own language (never on floor 1, never on stairs).
                if (FloorNumber >= 2 && run.Count >= 6)
                {
                    var mid = path[run[run.Count / 2]];
                    if (!(Layout.Levels.TryGetValue(mid, out var l) && l.Stair))
                    {
                        var at = new Vector3(mid.x * 4, Layout.LevelAt(mid), mid.y * 4);
                        var trapPrefab = FloorNumber <= 3 ? Resources.Load<GameObject>("CindersDungeon/SpikeTrap") : null;
                        if (FloorNumber <= 3 && trapPrefab) { var trap = Instantiate(trapPrefab, at + Vector3.up * .03f, Quaternion.identity, root).GetComponent<DungeonSpikeTrap>(); if (trap) { trap.player = context.player; trap.room = -1; trap.phaseOffset = (float)rng.NextDouble() * 4; } }
                        else if (FloorNumber <= 6) SewerGas.Create(this, -1, at, root, (float)rng.NextDouble() * 5);
                        else if (FloorNumber <= 9) AshPool.Create(this, -1, at, root);
                        else if (FloorNumber <= 12) FlameVent.Create(this, -1, at, root, (float)rng.NextDouble() * 3);
                    }
                }
            }
        }
    }
}
