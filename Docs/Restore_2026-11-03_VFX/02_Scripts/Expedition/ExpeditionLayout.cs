using System;
using System.Collections.Generic;
using UnityEngine;
namespace Codice.Expedition
{
    public enum ChamberShape { Rectangle, CutCorners, Cross }
    public sealed class ChamberPlan
    {
        public int width,depth;
        public ChamberShape shape;
        public readonly List<Vector2Int> traps=new List<Vector2Int>(),pillars=new List<Vector2Int>();
        public bool Contains(int x,int z)
        {
            if(Math.Abs(x)>width||Math.Abs(z)>depth)return false;
            if(shape==ChamberShape.Cross&&Math.Abs(x)>1&&Math.Abs(z)>1)return false;
            if(shape==ChamberShape.CutCorners&&Math.Abs(x)==width&&Math.Abs(z)==depth)return false;
            return true;
        }
    }
    public sealed class ExpeditionLayout
    {
        public const int Count=9, Boss=8;
        public readonly List<Vector2Int> Links=new List<Vector2Int>();
        public readonly List<List<Vector2Int>> Corridors=new List<List<Vector2Int>>();
        public readonly int MerchantRoom,StatueRoom;
        public bool Adjacent(int a,int b)=>Links.Exists(e=>e.x==a&&e.y==b||e.x==b&&e.y==a);
        public bool Forward(int a,int b)=>Links.Exists(e=>e.x==a&&e.y==b);
        public IEnumerable<int> Neighbors(int room){foreach(var e in Links){if(e.x==room)yield return e.y;else if(e.y==room)yield return e.x;}}
        public readonly List<Vector2Int> Rooms = new List<Vector2Int>();
        public readonly HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();
        public readonly List<ChamberPlan> Plans=new List<ChamberPlan>();
        public int Radius(int room)=>Plans[room].depth;
        public int Width(int room)=>Plans[room].width;
        public ExpeditionLayout(int seed)
        {
            var random=new System.Random(seed);int variant=random.Next(0,3);
            MerchantRoom=random.NextDouble()<.75?new[]{2,3,5,6}[random.Next(4)]:-1;
            StatueRoom=new[]{2,3,5,6}[random.Next(4)];
            int spread=random.Next(13,16),step=random.Next(12,15);
            var positions=new[]{Vector2Int.zero,new Vector2Int(0,step),new Vector2Int(-spread,step*2),new Vector2Int(spread,step*2),new Vector2Int(0,step*3),new Vector2Int(-spread,step*4),new Vector2Int(spread,step*4),new Vector2Int(0,step*5),new Vector2Int(0,step*6)};
            for(int i=0;i<Count;i++)
            {
                var plan=new ChamberPlan();
                if(i==0){plan.width=2;plan.depth=2;}
                else if(i==MerchantRoom){plan.width=2;plan.depth=3;plan.shape=ChamberShape.CutCorners;}
                else if(i==Boss){plan.width=4;plan.depth=4;plan.shape=ChamberShape.CutCorners;}
                else
                {
                    int style=(variant+(i==1?0:i==3?1:2))%3;
                    plan.width=style==0?4:style==1?2:3+random.Next(0,2);
                    plan.depth=style==0?2+random.Next(0,2):style==1?4:3;
                    plan.shape=style==2?ChamberShape.Cross:style==1?ChamberShape.CutCorners:ChamberShape.Rectangle;
                    int side=random.Next(0,2)==0?-1:1;
                    for(int n=0;n<(i==1?2:i==3?3:4);n++)
                    {
                        var cell=plan.shape==ChamberShape.Cross?new Vector2Int((n%2==0?1:-1)*(2+n/2),0):new Vector2Int((n%2==0?side:-side)*2,n<2?-1:1);
                        if(cell.x!=0&&cell.y!=0&&plan.Contains(cell.x,cell.y)&&!plan.traps.Contains(cell))plan.traps.Add(cell);
                    }
                    if(plan.shape!=ChamberShape.Cross)
                        foreach(int x in new[]{-2,2})foreach(int z in new[]{-2,2})
                            if(plan.Contains(x,z)&&!plan.traps.Contains(new Vector2Int(x,z)))plan.pillars.Add(new Vector2Int(x,z));
                }
                Plans.Add(plan);
                var center=positions[i];
                Rooms.Add(center);
                for(int x=-plan.width;x<=plan.width;x++)for(int z=-plan.depth;z<=plan.depth;z++)if(plan.Contains(x,z))Cells.Add(center+new Vector2Int(x,z));
            }
            foreach(var e in new[]{new Vector2Int(0,1),new Vector2Int(1,2),new Vector2Int(1,3),new Vector2Int(2,4),new Vector2Int(3,4),new Vector2Int(4,5),new Vector2Int(4,6),new Vector2Int(5,7),new Vector2Int(6,7),new Vector2Int(7,8)})
            {
                Links.Add(e);var path=new List<Vector2Int>();var p=Rooms[e.x];var end=Rooms[e.y];path.Add(p);
                while(p.x!=end.x){p.x+=Math.Sign(end.x-p.x);path.Add(p);}
                while(p.y!=end.y){p.y+=Math.Sign(end.y-p.y);path.Add(p);}
                Corridors.Add(path);foreach(var c in path)Cells.Add(c);
            }
        }
        public int RoomAt(Vector3 position)
        {
            var cell=new Vector2Int(Mathf.FloorToInt((position.x+2)/4),Mathf.FloorToInt((position.z+2)/4));
            for(int i=0;i<Rooms.Count;i++){var p=cell-Rooms[i];if(Plans[i].Contains(p.x,p.y))return i;}
            return -1;
        }
        public bool Inside(int room,Vector3 position,float inset=2)
        {
            var c=Rooms[room];var p=Plans[room];float x=position.x-c.x*4,z=position.z-c.y*4;
            return Mathf.Abs(x)<p.width*4+2-inset&&Mathf.Abs(z)<p.depth*4+2-inset&&RoomAt(position)==room;
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
