using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codice.Prologue
{
    // A local PRNG keeps level generation independent from combat's random stream.
    public sealed class PrologueLayout
    {
        public readonly HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();
        public readonly List<Vector2Int> Rooms = new List<Vector2Int>();
        public Vector2Int Exit => Rooms[Rooms.Count - 1];
        public PrologueLayout(int seed)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < 5; i++)
            {
                var room = new Vector2Int(i == 0 ? 0 : random.Next(-1, 2) * 5, i * 6);
                Rooms.Add(room);
                for (int x = -1; x <= 1; x++)
                    for (int z = -1; z <= 1; z++) Cells.Add(room + new Vector2Int(x, z));
                if (i == 0) continue;
                var cursor = Rooms[i - 1];
                // Horizontal, then vertical corridors guarantee a continuous route.
                while (cursor.x != room.x) { Cells.Add(cursor); cursor.x += Math.Sign(room.x - cursor.x); }
                while (cursor.y != room.y) { Cells.Add(cursor); cursor.y += Math.Sign(room.y - cursor.y); }
                Cells.Add(cursor);
            }
        }
        public bool IsConnected()
        {
            var visited = new HashSet<Vector2Int>();
            var pending = new Queue<Vector2Int>();
            pending.Enqueue(Vector2Int.zero); visited.Add(Vector2Int.zero);
            var sides = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (pending.Count > 0)
            {
                var cell = pending.Dequeue();
                foreach (var side in sides)
                    if (Cells.Contains(cell + side) && visited.Add(cell + side)) pending.Enqueue(cell + side);
            }
            return visited.Count == Cells.Count && visited.Contains(Exit);
        }
    }
}
