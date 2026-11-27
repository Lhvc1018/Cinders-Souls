using System;
using System.Collections.Generic;
using UnityEngine;
namespace Codice.Expedition
{
    public sealed class ExpeditionLayout
    {
        public readonly List<Vector2Int> Rooms = new List<Vector2Int>();
        public readonly HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();
        public int Radius(int room) => room == 5 ? 3 : 2;
        public ExpeditionLayout(int seed)
        {
            var random=new System.Random(seed);
            for(int i=0;i<6;i++)
            {
                var center=new Vector2Int(i==0?0:random.Next(-2,3)*2,i*11);
                Rooms.Add(center);int radius=Radius(i);
                for(int x=-radius;x<=radius;x++)for(int z=-radius;z<=radius;z++)Cells.Add(center+new Vector2Int(x,z));
                if(i==0)continue;
                var from=Rooms[i-1];int middle=from.y+5;
                for(int z=from.y;z<=middle;z++)Cells.Add(new Vector2Int(from.x,z));
                for(int x=Math.Min(from.x,center.x);x<=Math.Max(from.x,center.x);x++)Cells.Add(new Vector2Int(x,middle));
                for(int z=middle;z<=center.y;z++)Cells.Add(new Vector2Int(center.x,z));
            }
        }
        public bool Connected()
        {
            var seen=new HashSet<Vector2Int>{Vector2Int.zero};var queue=new Queue<Vector2Int>();queue.Enqueue(Vector2Int.zero);
            var sides=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
            while(queue.Count>0){var p=queue.Dequeue();foreach(var s in sides)if(Cells.Contains(p+s)&&seen.Add(p+s))queue.Enqueue(p+s);}
            return seen.Count==Cells.Count;
        }
    }
}
