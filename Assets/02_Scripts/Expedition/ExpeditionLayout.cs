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
        public int Count => Rooms.Count;
        public readonly int Boss, SecretRoom;
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
        /// Base seed whose floor-4 layout hosts the giant boss (debug scenarios).
        public static int GiantSeed(){int seed=1000;while(!GiantBoss(unchecked(seed+3*104729),4))seed++;return seed;}
        public static bool GiantBoss(int seed,int floor)=>floor==4&&((unchecked((uint)seed)*2246822519u)%4u)==0;
        public ExpeditionLayout(int seed,int floor=1,int memory=0)
        {
            var random=new System.Random(seed);
            int variant=random.Next(0,3);
            int beforeBoss=random.Next(6,10);
            bool secret=floor>=7 || beforeBoss>6 && random.Next(3)!=0;
            Boss=beforeBoss-(secret?1:0);
            SecretRoom=secret?beforeBoss:-1;
            int total=beforeBoss+1;
            MerchantRoom=random.NextDouble()<.75?random.Next(1,Boss):-1;
            do {StatueRoom=random.Next(1,Boss);}while(StatueRoom==MerchantRoom);
            // Spacing caps keep connecting corridors short (rooms are at most 4 cells deep before floor 7). RNG calls are kept so seeds stay stable.
            // Lateral spread was 11-16 cells: zig-zag corridors ran up to 34 cells (136 m). 8 still leaves a 6-cell gap between two
            // widest rooms on one row; step 12 leaves >=2 cells between rows. RNG calls are kept so seeds stay stable.
            int spread=floor>=7?8:Math.Min(8,random.Next(12,17)),step=floor>=7?12:Math.Min(12,random.Next(12,17));
            var positions=new List<Vector2Int>{Vector2Int.zero};
            var edges=new List<Vector2Int>();int current=0, row=0;
            while(positions.Count<=Boss)
            {
                int remaining=Boss+1-positions.Count;
                if(remaining>=3&&random.NextDouble()<.5)
                {
                    int left=positions.Count,right=left+1,merge=left+2;
                    // Branches open around the current column, so no corridor crosses more than one spread sideways.
                    int cx=positions[current].x;
                    positions.Add(new Vector2Int(cx-spread,++row*step));
                    positions.Add(new Vector2Int(cx+spread,row*step));
                    positions.Add(new Vector2Int(cx,++row*step));
                    edges.Add(new Vector2Int(current,left));edges.Add(new Vector2Int(current,right));
                    edges.Add(new Vector2Int(left,merge));edges.Add(new Vector2Int(right,merge));current=merge;
                }
                else
                {
                    int next=positions.Count;int x=Mathf.Clamp(positions[current].x+random.Next(-1,2)*spread,-spread,spread);if(next==Boss)x=positions[current].x;
                    positions.Add(new Vector2Int(x,++row*step));edges.Add(new Vector2Int(current,next));current=next;
                }
            }
            // Secret chamber sits just behind the cracked wall of the start room (room 0 half-width 2 + 3-cell passage + its own half-width 2 + 1).
            // Rows start at z>=step-5>=7, so the chamber (z -2..2) never overlaps another room.
            if(secret)positions.Add(new Vector2Int((random.Next(2)==0?-1:1)*8,0));
            if(secret)edges.Add(new Vector2Int(0,SecretRoom));
            for(int i=0;i<total;i++)
            {
                var plan=new ChamberPlan();
                if(i==0){plan.width=2;plan.depth=2;}
                else if(i==SecretRoom){plan.width=2;plan.depth=2;}
                else if(i==StatueRoom){plan.width=1;plan.depth=2;plan.shape=ChamberShape.Rectangle;}
                else if(i==MerchantRoom){plan.width=2;plan.depth=3;plan.shape=ChamberShape.CutCorners;}
                else if(i==Boss){plan.width=GiantBoss(seed,floor)?10:floor==15?8:floor>=13?6:floor==12?7:floor>=10?5:floor==9?8:floor==3?6:4;plan.depth=GiantBoss(seed,floor)?10:floor==15?8:floor>=13?6:floor==12?7:floor>=10?5:floor==9?8:floor==3?6:4;plan.shape=ChamberShape.CutCorners;}
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
                if(floor>=7&&i>0&&i!=Boss&&i!=SecretRoom&&i!=StatueRoom&&i!=MerchantRoom){plan.width=4+(i%2);plan.depth=3+(i%3);plan.shape=i%2==0?ChamberShape.Rectangle:ChamberShape.CutCorners;plan.pillars.Clear();plan.traps.Clear();}
                Plans.Add(plan);
                if(i==Boss&&GiantBoss(seed,floor))positions[i]+=Vector2Int.up*14;
                else if(i==Boss&&(floor==9||floor==12||floor==15))positions[i]+=Vector2Int.up*3;
                var center=positions[i];
                Rooms.Add(center);
                for(int x=-plan.width;x<=plan.width;x++)for(int z=-plan.depth;z<=plan.depth;z++)if(plan.Contains(x,z))Cells.Add(center+new Vector2Int(x,z));
            }
            foreach(var e in edges)
            {
                Links.Add(e);var path=new List<Vector2Int>();var p=Rooms[e.x];var end=Rooms[e.y];path.Add(p);
                int middle=(p.y+end.y)/2;
                while(p.y!=middle){p.y+=Math.Sign(middle-p.y);path.Add(p);}
                while(p.x!=end.x){p.x+=Math.Sign(end.x-p.x);path.Add(p);}
                while(p.y!=end.y){p.y+=Math.Sign(end.y-p.y);path.Add(p);}
                Corridors.Add(path);foreach(var c in path)Cells.Add(c);
            }
            if(floor>=7||memory!=0)AddSideRooms(seed ^ (memory*3571));
            if(floor>=2)AddVault(seed,floor);
            if(floor>=7)Elevate(seed);
        }
        // ---------------- Hidden vaults: a sealed chamber behind a puzzle door (secret boss on x-2 floors) ----------------
        public int VaultRoom{get;private set;}=-1;
        public int VaultHost{get;private set;}=-1;
        public static bool VaultBossFloor(int floor)=>floor%3==2;
        void AddVault(int seed,int floor)
        {
            var rng=new System.Random(seed^0x7A017);if(rng.Next(100)>=75)return;
            var candidates=new List<int>();for(int i=1;i<Boss;i++)if(i!=SecretRoom&&i!=StatueRoom&&i!=MerchantRoom&&!SideRooms.Contains(i))candidates.Add(i);
            for(int i=candidates.Count-1;i>0;i--){int j=rng.Next(i+1);var t=candidates[i];candidates[i]=candidates[j];candidates[j]=t;}
            bool boss=VaultBossFloor(floor);
            foreach(int host in candidates)foreach(int sign in rng.Next(2)==0?new[]{-1,1}:new[]{1,-1})
            {
                var plan=boss?new ChamberPlan{width=3,depth=3,shape=ChamberShape.CutCorners}:new ChamberPlan{width=2,depth=2,shape=ChamberShape.CutCorners};
                const int gap=3;var center=Rooms[host]+new Vector2Int(sign*(Plans[host].width+gap+plan.width+1),0);
                var fresh=new HashSet<Vector2Int>();
                for(int x=-plan.width;x<=plan.width;x++)for(int z=-plan.depth;z<=plan.depth;z++)if(plan.Contains(x,z))fresh.Add(center+new Vector2Int(x,z));
                var path=new List<Vector2Int>();var p=Rooms[host];path.Add(p);while(p.x!=center.x){p.x+=sign;path.Add(p);}
                foreach(var c in path)if(RoomOf(c)!=host)fresh.Add(c);
                bool ok=true;
                foreach(var n in fresh)
                {
                    if(Cells.Contains(n)){ok=false;break;}
                    foreach(var s in Sides){var q=n+s;if(fresh.Contains(q)||!Cells.Contains(q))continue;if(path.Contains(n)&&RoomOf(q)==host&&s.y==0)continue;ok=false;break;}
                    if(!ok)break;
                }
                if(!ok)continue;
                int index=Rooms.Count;Rooms.Add(center);Plans.Add(plan);foreach(var n in fresh)Cells.Add(n);
                Links.Add(new Vector2Int(host,index));Corridors.Add(path);VaultRoom=index;VaultHost=host;return;
            }
        }
        // ---------------- World 3: verticality and optional side crypts ----------------
        public sealed class CellLevel{public float a,b;public Vector2Int dir;public bool Stair=>Math.Abs(b-a)>.01f;public float Low=>Math.Min(a,b);public float High=>Math.Max(a,b);}
        /// Per-cell walking level (metres). Flat cells have a==b; stair cells rise from a (entry edge) to b along dir.
        public readonly Dictionary<Vector2Int,CellLevel> Levels=new Dictionary<Vector2Int,CellLevel>();
        public readonly List<int> SideRooms=new List<int>();
        float[] roomHeights;
        public bool Vertical=>roomHeights!=null&&Array.Exists(roomHeights,h=>Math.Abs(h)>.01f);
        public float Height(int room)=>roomHeights==null||room<0||room>=roomHeights.Length?0:roomHeights[room];
        public float LevelAt(Vector2Int cell)=>Levels.TryGetValue(cell,out var l)?l.Low:0;
        /// Walking height of a cell at the edge facing `side`; NaN for the lateral sides of a stair.
        public float EdgeHeight(Vector2Int cell,Vector2Int side){if(!Levels.TryGetValue(cell,out var l))return 0;if(!l.Stair)return l.a;if(side==l.dir)return l.b;if(side==-l.dir)return l.a;return float.NaN;}
        int RoomOf(Vector2Int cell){for(int i=0;i<Rooms.Count;i++){var d=cell-Rooms[i];if(Plans[i].Contains(d.x,d.y))return i;}return -1;}
        static readonly Vector2Int[] Sides={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        void AddSideRooms(int seed)
        {
            var rng=new System.Random(seed^0x51DE);
            var candidates=new List<int>();for(int i=1;i<Boss;i++)if(i!=SecretRoom&&i!=StatueRoom&&i!=MerchantRoom)candidates.Add(i);
            for(int i=candidates.Count-1;i>0;i--){int j=rng.Next(i+1);var t=candidates[i];candidates[i]=candidates[j];candidates[j]=t;}
            int wanted=1+rng.Next(2);
            foreach(int host in candidates)
            {
                if(SideRooms.Count>=wanted)break;
                foreach(int sign in rng.Next(2)==0?new[]{-1,1}:new[]{1,-1})
                {
                    var plan=new ChamberPlan{width=3,depth=2,shape=ChamberShape.CutCorners};
                    const int gap=3;var center=Rooms[host]+new Vector2Int(sign*(Plans[host].width+gap+plan.width+1),0);
                    var fresh=new HashSet<Vector2Int>();
                    for(int x=-plan.width;x<=plan.width;x++)for(int z=-plan.depth;z<=plan.depth;z++)if(plan.Contains(x,z))fresh.Add(center+new Vector2Int(x,z));
                    var path=new List<Vector2Int>();var p=Rooms[host];path.Add(p);while(p.x!=center.x){p.x+=sign;path.Add(p);}
                    foreach(var c in path)if(RoomOf(c)!=host)fresh.Add(c);
                    bool ok=true;
                    foreach(var n in fresh)
                    {
                        if(Cells.Contains(n)){ok=false;break;}
                        foreach(var s in Sides){var q=n+s;if(fresh.Contains(q)||!Cells.Contains(q))continue;if(path.Contains(n)&&RoomOf(q)==host&&s.y==0)continue;ok=false;break;}
                        if(!ok)break;
                    }
                    if(!ok)continue;
                    int index=Rooms.Count;Rooms.Add(center);Plans.Add(plan);foreach(var n in fresh)Cells.Add(n);
                    Links.Add(new Vector2Int(host,index));Corridors.Add(path);SideRooms.Add(index);break;
                }
            }
        }
        void Elevate(int seed)
        {
            var rng=new System.Random(seed^0x7E11);
            var usage=new Dictionary<Vector2Int,int>();foreach(var path in Corridors)foreach(var c in new HashSet<Vector2Int>(path))usage[c]=usage.TryGetValue(c,out var u)?u+1:1;
            var owner=new Dictionary<Vector2Int,int>();foreach(var c in Cells)owner[c]=RoomOf(c);
            for(int attempt=0;attempt<40;attempt++)
            {
                bool gentle=attempt>=20;var h=new float[Rooms.Count];var set=new bool[Rooms.Count];set[0]=true;
                foreach(var e in Links)
                {
                    if(set[e.y])continue;set[e.y]=true;
                    if(e.y==SecretRoom){h[e.y]=0;continue;}
                    float[] options=SideRooms.Contains(e.y)||e.y==VaultRoom?new[]{-1.2f,1.2f}:gentle?new[]{-1.2f,0,1.2f}:new[]{-2.4f,-1.2f,1.2f,2.4f,1.2f,-1.2f,0};
                    h[e.y]=Mathf.Clamp(h[e.x]+options[rng.Next(options.Length)],-2.4f,4.8f);
                }
                if(TryLevels(h,usage,owner)){roomHeights=h;return;}
            }
            TryLevels(new float[Rooms.Count],usage,owner);roomHeights=new float[Rooms.Count];
        }
        bool TryLevels(float[] h,Dictionary<Vector2Int,int> usage,Dictionary<Vector2Int,int> owner)
        {
            Levels.Clear();
            bool Set(Vector2Int c,float a,float b,Vector2Int dir)
            {
                if(Levels.TryGetValue(c,out var o))return Math.Abs(o.a-a)<.01f&&Math.Abs(o.b-b)<.01f;
                Levels[c]=new CellLevel{a=a,b=b,dir=dir};return true;
            }
            foreach(var c in Cells){int r=owner[c];if(r>=0&&!Set(c,h[r],h[r],Vector2Int.zero))return false;}
            for(int k=0;k<Links.Count;k++)
            {
                var path=Corridors[k];float ha=h[Links[k].x],hb=h[Links[k].y],delta=hb-ha;
                var free=new List<int>();for(int j=0;j<path.Count;j++)if(owner[path[j]]<0)free.Add(j);
                int f0=int.MaxValue,n=0;
                if(Math.Abs(delta)>.01f)
                {
                    n=Mathf.CeilToInt(Math.Abs(delta)/1.2f-.001f);
                    int bestStart=-1,bestLen=0,runStart=-1,runLen=0;
                    for(int j=1;j<path.Count-1;j++)
                    {
                        bool straight=owner[path[j]]<0&&usage[path[j]]==1&&path[j]-path[j-1]==path[j+1]-path[j]&&owner[path[j-1]]<0&&owner[path[j+1]]<0;
                        if(straight){if(runLen==0)runStart=j;runLen++;if(runLen>bestLen){bestLen=runLen;bestStart=runStart;}}else runLen=0;
                    }
                    if(bestLen<n)return false;
                    f0=bestStart+(bestLen-n)/2;
                }
                foreach(int j in free)
                {
                    bool ok=j<f0?Set(path[j],ha,ha,Vector2Int.zero):j<f0+n?Set(path[j],ha+delta*(j-f0)/n,ha+delta*(j-f0+1)/n,path[j+1]-path[j]):Set(path[j],hb,hb,Vector2Int.zero);
                    if(!ok)return false;
                }
            }
            foreach(var c in Cells)if(!Levels.ContainsKey(c))return false;
            // every corridor step must meet at the same height on both sides of the shared edge
            foreach(var path in Corridors)for(int j=1;j<path.Count;j++)
            {
                var s=path[j]-path[j-1];float x=EdgeHeight(path[j-1],s),y=EdgeHeight(path[j],-s);
                if(float.IsNaN(x)||float.IsNaN(y)||Math.Abs(x-y)>.01f)return false;
            }
            return true;
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

