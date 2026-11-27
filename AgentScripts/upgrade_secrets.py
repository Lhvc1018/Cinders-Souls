from pathlib import Path
p=Path('Assets/02_Scripts/Expedition/ExpeditionLayout.cs')
s=p.read_text(encoding='utf-8-sig').replace('public const int Count=9, Boss=8;','public int Count => Rooms.Count;\n        public readonly int Boss, SecretRoom;')
a=s.index('            var random=new System.Random(seed);');b=s.index('            for(int i=0;i<Count;i++)',a)
s=s[:a]+'''            var random=new System.Random(seed);int variant=random.Next(0,3);
            int total=random.Next(6,10);Boss=total-2;SecretRoom=total-1;
            MerchantRoom=random.NextDouble()<.75?random.Next(1,Boss):-1;
            do {StatueRoom=random.Next(1,Boss);}while(StatueRoom==MerchantRoom);
            int spread=random.Next(12,17),step=random.Next(12,17);
            var positions=new List<Vector2Int>{Vector2Int.zero};
            var edges=new List<Vector2Int>();int current=0, row=0;
            while(positions.Count<=Boss)
            {
                int remaining=Boss+1-positions.Count;
                if(remaining>=3&&random.NextDouble()<.5)
                {
                    int left=positions.Count,right=left+1,merge=left+2;
                    positions.Add(new Vector2Int(-spread,++row*step));
                    positions.Add(new Vector2Int(spread,row*step));
                    positions.Add(new Vector2Int(0,++row*step));
                    edges.Add(new Vector2Int(current,left));edges.Add(new Vector2Int(current,right));
                    edges.Add(new Vector2Int(left,merge));edges.Add(new Vector2Int(right,merge));current=merge;
                }
                else
                {
                    int next=positions.Count;int x=next==Boss?0:random.Next(-1,2)*spread;
                    positions.Add(new Vector2Int(x,++row*step));edges.Add(new Vector2Int(current,next));current=next;
                }
            }
            positions.Add(new Vector2Int((random.Next(2)==0?-1:1)*spread*2,0));
            edges.Add(new Vector2Int(0,SecretRoom));
            for(int i=0;i<total;i++)
''' +s[b+len('            for(int i=0;i<Count;i++)\n'):]
s=s.replace('else if(i==StatueRoom)', 'else if(i==SecretRoom){plan.width=2;plan.depth=2;}\n                else if(i==StatueRoom)')
a=s.index('            var edges=new List<Vector2Int>(new[]');b=s.index('            foreach(var e in edges)',a);s=s[:a]+s[b:]
s=s.replace('while(p.x!=end.x){p.x+=Math.Sign(end.x-p.x);path.Add(p);}', 'int middle=(p.y+end.y)/2;\n                while(p.y!=middle){p.y+=Math.Sign(middle-p.y);path.Add(p);}\n                while(p.x!=end.x){p.x+=Math.Sign(end.x-p.x);path.Add(p);}')
p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Expedition/ExpeditionFloor.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public readonly bool[] Visited=new bool[RoomCount],Cleared=new bool[RoomCount];','public bool[] Visited {get;private set;}\n        public bool[] Cleared {get;private set;}').replace('public readonly TreasureChest[] Chests=new TreasureChest[RoomCount];','public TreasureChest[] Chests {get;private set;}').replace('public const int RoomCount=ExpeditionLayout.Count;','public int RoomCount=>Layout?.Count??0;\n        public bool SecretRevealed {get;private set;}').replace('CurrentRoom==ExpeditionLayout.Boss','CurrentRoom==Layout.Boss')
s=s.replace('public string TitleFor(int room)=>room==Layout.MerchantRoom?', 'public string TitleFor(int room)=>room==Layout.Boss?"Santuario del inquisidor":room==Layout.SecretRoom?"Cámara del juramento roto":room==Layout.StatueRoom?"Capilla del pacto":room==Layout.MerchantRoom?')
s=s.replace('Enumerable.Range(0,6)','Enumerable.Range(0,RoomCount)')
s=s.replace('if(!Layout.Connected())', 'Visited=new bool[RoomCount];Cleared=new bool[RoomCount];Chests=new TreasureChest[RoomCount];\n            if(!Layout.Connected())')
s=s.replace('var points=new GameObject("Room spawn markers")','BuildSecretWall(root);\n            var points=new GameObject("Room spawn markers")')
s=s.replace('if(!Layout.Links.Any(e=>e.y==index&&Cleared[e.x]))return false;', 'if(index==Layout.SecretRoom&&!SecretRevealed)return false;\n            if(!Layout.Links.Any(e=>e.y==index&&Cleared[e.x]))return false;')
s=s.replace('index!=Layout.MerchantRoom&&index!=Layout.StatueRoom','index!=Layout.MerchantRoom&&index!=Layout.StatueRoom&&index!=Layout.SecretRoom').replace('index==Layout.MerchantRoom||index==Layout.StatueRoom','index==Layout.MerchantRoom||index==Layout.StatueRoom||index==Layout.SecretRoom')
s=s.replace('Cleared[index]=true;ClearedRooms++;director.SetExpeditionPhase', 'if(index==Layout.SecretRoom){SpawnChest(index,false,false,3);RelicAltar.Create(Center(index)+new Vector3(-3,.1f,0),Seed^971,false);}\n                Cleared[index]=true;ClearedRooms++;director.SetExpeditionPhase')
p.write_text(s,encoding='utf-8')
for f in ['RunHUD.cs','RunHUD.Menus.cs']:
 p=Path('Assets/02_Scripts/UI')/f;s=p.read_text(encoding='utf-8-sig').replace('Codice.Expedition.ExpeditionFloor.RoomCount','director.Expedition.RoomCount');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.World.cs');s=p.read_text(encoding='utf-8-sig');s='\n'.join(l for l in s.split('\n') if 'Waypoint(0,Context.dungeon.merchant' not in l);p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.Dungeon.cs');s=p.read_text(encoding='utf-8-sig').replace('new Vector2[ExpeditionFloor.RoomCount]','new Vector2[9]').replace('ExpeditionFloor.RoomCount','Floor.RoomCount').replace('ExpeditionLayout.Boss','Floor.Layout.Boss')
s=s.replace('int hash=Floor.Seed^Floor.PlayerRoom;','int hash=Floor.Seed^Floor.PlayerRoom^(Floor.SecretRevealed?16384:0);')
s=s.replace('icon.gameObject.SetActive(i==0||', 'icon.gameObject.SetActive((i!=Floor.Layout.SecretRoom||Floor.SecretRevealed)&&(i==0||').replace('||chest);','||chest));')
s=s.replace('var a=centers[link.x];','if(link.y==Floor.Layout.SecretRoom&&!Floor.SecretRevealed)continue;\n                var a=centers[link.x];')
s=s.replace('Box(mesh,new Rect(Mathf.Min(a.x,b.x)-2,a.y-2,Mathf.Abs(a.x-b.x)+4,4),color);\n                Box(mesh,new Rect(b.x-2,Mathf.Min(a.y,b.y)-2,4,Mathf.Abs(b.y-a.y)+4),color);','float mid=(a.y+b.y)/2;\n                Box(mesh,new Rect(a.x-2,Mathf.Min(a.y,mid)-2,4,Mathf.Abs(a.y-mid)+4),color);\n                Box(mesh,new Rect(Mathf.Min(a.x,b.x)-2,mid-2,Mathf.Abs(a.x-b.x)+4,4),color);\n                Box(mesh,new Rect(b.x-2,Mathf.Min(mid,b.y)-2,4,Mathf.Abs(mid-b.y)+4),color);')
s=s.replace('var color=i==Floor.PlayerRoom?', 'if(i==Floor.Layout.SecretRoom&&!Floor.SecretRevealed)continue;\n                var color=i==Floor.PlayerRoom?')
p.write_text(s,encoding='utf-8')
