using System.Collections.Generic;
using UnityEngine;

namespace Codice.Prologue
{
    // Short, hand-made antechamber: the Vigía's cell, a crossroads, then two roads —
    // straight ahead to EL UMBRAL (descend now) or west to LA PRUEBA (tutorial room with its own threshold).
    public sealed class PrologueLayout
    {
        public readonly HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();
        public readonly List<Vector2Int> Rooms = new List<Vector2Int>();
        public static readonly Vector2Int Vigil = new Vector2Int(0, 0), Crossroads = new Vector2Int(0, 4), Trial = new Vector2Int(-6, 4);
        public static readonly Vector2Int DirectExitCell = new Vector2Int(0, 7), TrialExitCell = new Vector2Int(-6, 8);
        public Vector2Int Exit => DirectExitCell;
        public PrologueLayout(int seed)
        {
            Room(Vigil, 1); Room(Crossroads, 1); Room(Trial, 2);
            Cells.Add(new Vector2Int(0, 2));                                         // cell → crossroads
            Cells.Add(new Vector2Int(0, 6)); Cells.Add(DirectExitCell);                 // crossroads → umbral
            Cells.Add(new Vector2Int(-2, 4)); Cells.Add(new Vector2Int(-3, 4));         // crossroads → prueba
            Cells.Add(new Vector2Int(-6, 7)); Cells.Add(TrialExitCell);                 // prueba → its umbral
        }
        void Room(Vector2Int c, int r)
        {
            Rooms.Add(c);
            for (int x = -r; x <= r; x++) for (int z = -r; z <= r; z++) Cells.Add(c + new Vector2Int(x, z));
        }
        public bool IsConnected()
        {
            var visited = new HashSet<Vector2Int>(); var pending = new Queue<Vector2Int>();
            pending.Enqueue(Vector2Int.zero); visited.Add(Vector2Int.zero);
            var sides = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (pending.Count > 0) { var cell = pending.Dequeue(); foreach (var side in sides) if (Cells.Contains(cell + side) && visited.Add(cell + side)) pending.Enqueue(cell + side); }
            return visited.Count == Cells.Count && visited.Contains(DirectExitCell) && visited.Contains(TrialExitCell);
        }
    }
}
