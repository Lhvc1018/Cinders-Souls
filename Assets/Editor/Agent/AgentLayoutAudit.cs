using System.Linq;
using System.Text;
using Codice.Expedition;
using UnityEngine;
/// Generates many seeded layouts per floor and checks: connectivity, secret room present when flagged and close to its wall,
/// longest corridor-only run, rooms overlapping (a cell owned by two rooms).
public static class AgentLayoutAudit
{
    public static string Run()
    {
        var sb = new StringBuilder();
        for (int floor = 1; floor <= 15; floor++)
        {
            int bad = 0, maxRun = 0, secrets = 0, overlap = 0, elevFail = 0; long runSum = 0; int runs = 0; string example = "";
            for (int s = 0; s < 300; s++)
            {
                int seed = unchecked(s * 7477 + 13 + (floor - 1) * 104729);
                ExpeditionLayout l;
                try { l = new ExpeditionLayout(seed, floor, s % 4 == 0 ? 3 : 0); } catch (System.Exception e) { bad++; example = e.Message; continue; }
                if (!l.Connected()) { bad++; example = "disconnected seed " + seed; }
                if (l.SecretRoom >= 0) { secrets++; if (System.Math.Abs(l.Rooms[l.SecretRoom].x) != 8) example = "secret far"; }
                for (int i = 0; i < l.Rooms.Count; i++) for (int j = i + 1; j < l.Rooms.Count; j++)
                {
                    var pi = l.Plans[i]; var pj = l.Plans[j]; var d = l.Rooms[j] - l.Rooms[i];
                    if (System.Math.Abs(d.x) <= pi.width + pj.width && System.Math.Abs(d.y) <= pi.depth + pj.depth)
                        for (int x = -pj.width; x <= pj.width; x++) for (int z = -pj.depth; z <= pj.depth; z++) if (pj.Contains(x, z) && pi.Contains(x + d.x, z + d.y)) { overlap++; goto next; }
                    next:;
                }
                foreach (var path in l.Corridors)
                {
                    int run = 0, best = 0;
                    foreach (var c in path) { if (l.RoomAt(new Vector3(c.x * 4, 0, c.y * 4)) < 0) { run++; best = Mathf.Max(best, run); } else run = 0; }
                    maxRun = Mathf.Max(maxRun, best); runSum += best; runs++;
                }
                if (floor >= 7 && !l.Vertical) elevFail++;
            }
            sb.AppendLine($"floor {floor}: bad={bad} overlapPairs={overlap} secrets={secrets}/300 corridor avg={(float)runSum / Mathf.Max(1, runs):0.0} max={maxRun} cells flatW3={elevFail} {example}");
        }
        return sb.ToString();
    }
}
