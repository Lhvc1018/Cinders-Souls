using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Codice.Expedition
{
    // World 3 (floors 7-9): stepped terrain, stair flights, retaining ledges, optional side crypts and
    // room themes dressed with the darkened KayKit dungeon kit (Resources/WorldThree/Kit).
    public sealed partial class ExpeditionFloor
    {
        readonly Dictionary<string,GameObject> kit=new Dictionary<string,GameObject>();
        Material foundationStone;
        static readonly Vector2Int[] GridSides={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        enum CryptTheme{Ossuary,RuinedChapel,ExecutionHall,DrownedGarden}
        bool IsSideRoom(int room)=>Layout.SideRooms.Contains(room);
        GameObject Kit(string name,Vector3 position,float yaw,Transform parent,float scale=1)
        {
            if(!kit.TryGetValue(name,out var asset)){asset=Resources.Load<GameObject>("WorldThree/Kit/"+name);kit[name]=asset;}
            if(!asset)return null;
            var go=Instantiate(asset,position,Quaternion.Euler(0,yaw,0),parent);go.transform.localScale*=scale;return go;
        }
        /// Places a solid prop only where nothing solid stands yet (graves, crypts, other props).
        GameObject KitIfClear(string name,Vector3 position,float yaw,Transform parent,float scale,Vector3 halfExtents)
        {
            Physics.SyncTransforms();
            var box=position+Vector3.up*(halfExtents.y+.25f);
            foreach(var hit in Physics.OverlapBox(box,halfExtents,Quaternion.Euler(0,yaw,0),~0,QueryTriggerInteraction.Ignore))
                if(hit.bounds.max.y>position.y+.3f)return null;
            return Kit(name,position,yaw,parent,scale);
        }
        /// Cell-level geometry for World 3. Returns false when the cell is ordinary flat ground.
        bool BuildWorldThreeCell(Vector2Int cell,Transform root)
        {
            var level=Layout.Levels.TryGetValue(cell,out var l)?l:null;if(level==null)return false;
            var p=new Vector3(cell.x*4,level.Low,cell.y*4);
            // Foundations hide the void under raised ground wherever a lower neighbour can see the cliff.
            float lowest=level.Low;
            foreach(var s in GridSides)if(Layout.Cells.Contains(cell+s))lowest=Mathf.Min(lowest,Layout.LevelAt(cell+s));
            if(lowest<level.Low-.05f)Foundation(p,lowest,level.Low,root);
            // Ledges: any shared edge whose two walking heights differ (and is not a designed stair joint) gets a parapet.
            foreach(var s in GridSides)
            {
                if(!Layout.Cells.Contains(cell+s))continue;
                float mine=Layout.EdgeHeight(cell,s),theirs=Layout.EdgeHeight(cell+s,-s);
                if(float.IsNaN(mine)){Ledge(p,s,level.High,true,root);continue;}
                if(float.IsNaN(theirs))continue;// the stair cell builds its own side wall
                if(mine>theirs+.3f)Ledge(p,s,mine,false,root);
            }
            if(!level.Stair)return false;
            BuildStair(cell,level,root);return true;
        }
        void Foundation(Vector3 p,float from,float to,Transform root)
        {
            if(!foundationStone)foundationStone=Resources.Load<Material>(FloorNumber>=13?"WorldFive/Barro pisoteado":FloorNumber>=10?"WorldFour/Basalto estratificado":"CindersStone/Wall")??stoneMaterial;
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Cimiento del bancal";go.transform.SetParent(root);
            float bottom=from-.6f,top=to-.36f;go.transform.position=new Vector3(p.x,(bottom+top)/2,p.z);go.transform.localScale=new Vector3(4,top-bottom,4);
            go.GetComponent<Renderer>().sharedMaterial=foundationStone;
        }
        void Ledge(Vector3 p,Vector2Int side,float height,bool tall,Transform root)
        {
            var delta=new Vector3(side.x,0,side.y);float yaw=side.x!=0?90:0;
            var at=new Vector3(p.x,height,p.z)+delta*1.75f;
            if(FloorNumber>=13){Camp("Barricade",at,yaw,root);return;}
            if(FloorNumber>=10){Forge("Rail",at,yaw,root);return;}
            var piece=tall?Kit("wall_broken",new Vector3(p.x,p.y,p.z)+delta*1.75f,yaw,root):Kit("barrier_column",at,yaw,root);
            if(piece)piece.name=tall?"Muro del tramo":"Pretil del bancal";
            if(!tall&&((Mathf.RoundToInt(p.x+p.z)&7)==0))Kit("candle_triple",at-delta*.6f,yaw,root,.6f);
        }
        void BuildStair(Vector2Int cell,ExpeditionLayout.CellLevel level,Transform root)
        {
            var center=new Vector3(cell.x*4,0,cell.y*4);var dir=new Vector3(level.dir.x,0,level.dir.y);float yaw=level.dir.x!=0?90:0;
            float delta=level.b-level.a;int n=Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(delta)/.2f));const float thickness=.35f;
            float bottom=level.Low-.35f;
            for(int i=0;i<n;i++)
            {
                float top=level.a+delta*(i+1)/n;float along=-2+(i+.5f)*4f/n;
                var tread=FloorNumber>=13?Camp("Ground",center+dir*along+Vector3.up*bottom,yaw,root):FloorNumber>=10?Forge("Floor",center+dir*along+Vector3.up*bottom,yaw,root):Module("Paving",center+dir*along+Vector3.up*bottom,yaw,root);tread.name="Peldaño del cementerio";
                tread.transform.localScale=new Vector3(.9f,(top-bottom)/thickness,1f/n);
            }
            // Balustrade candles at the upper landing make the climb readable from below.
            var upper=center+dir*(delta>0?1.6f:-1.6f)+Vector3.up*level.High;var side=new Vector3(dir.z,0,dir.x);
            if(FloorNumber<10)foreach(int s in new[]{-1,1})Kit("candle_lit",upper+side*s*1.75f,yaw,root,.55f);
            Glow(upper+Vector3.up*1.4f,new Color(1f,.62f,.32f),2.2f,8,root);
        }
        // ---------------------------------------------------------------- residents
        /// World 3 rosters mix the base residents with their authored variants; deeper floors lean on the elites.
        Codice.Enemies.BasicEnemy[] CemeteryRoster(int room)
        {
            string[] pool=FloorNumber==7?new[]{"HeadSpider","Huntsman","Executioner","StrawReaper","Watcher","Variants/HeadSpider_Osario","Variants/Huntsman_Ceniza","Variants/StrawReaper_Vela"}
                :FloorNumber==8?new[]{"HeadSpider","Executioner","Watcher","Variants/HeadSpider_Osario","Variants/Huntsman_Carmesi","Variants/Executioner_Campana","Variants/StrawReaper_Podrido","Variants/Watcher_Fatuo"}
                :new[]{"Executioner","Watcher","Variants/HeadSpider_Brasa","Variants/Huntsman_Carmesi","Variants/Executioner_Sangre","Variants/StrawReaper_Podrido","Variants/Watcher_Velado","Variants/Watcher_Fatuo","Variants/Executioner_Campana"};
            var rng=new System.Random(Seed^room*7349);var picked=new List<Codice.Enemies.BasicEnemy>();
            foreach(var name in pool.OrderBy(_=>rng.Next()).Take(IsSideRoom(room)?3:5)){var e=Resources.Load<Codice.Enemies.BasicEnemy>("ExpeditionEnemies/Cemetery/"+name);if(e)picked.Add(e);}
            if(picked.Count==0)picked.Add(Resources.Load<Codice.Enemies.BasicEnemy>("ExpeditionEnemies/Cemetery/Watcher"));
            return picked.ToArray();
        }
        // ---------------------------------------------------------------- room dressing
        void WorldThreeDressing(int room,Transform root)
        {
            var c=Center(room);var plan=Layout.Plans[room];var rng=new System.Random(Seed^room*6151^FloorNumber*97);
            if(IsSideRoom(room)){SideCrypt(room,c,plan,rng,root);return;}
            if(room==0||room==Layout.Boss||room==Layout.SecretRoom||room==Layout.MerchantRoom||room==Layout.StatueRoom)return;
            var theme=(CryptTheme)((rng.Next(4)+FloorNumber+room)%4);
            // Perimeter rows (north/south) away from the corridor axis, the crypt flank (west) and the gallery (east).
            var spots=new List<(Vector3 p,float yaw)>();
            foreach(int row in new[]{-1,1})for(int x=-(plan.width-2);x<=plan.width-2;x++)
            {
                if(Mathf.Abs(x)<=(row<0?2:1)||!plan.Contains(x,row*plan.depth))continue;
                spots.Add((c+new Vector3(x*4,0,row*(plan.depth*4+.6f)),row<0?0:180));
            }
            int light=0;
            foreach(var (p,yaw) in spots)
            {
                GameObject placed=null;int pick=rng.Next(4);
                switch(theme)
                {
                    case CryptTheme.Ossuary:
                        placed=pick<2?KitIfClear("shelves",p,yaw,root,1,new Vector3(1,1.2f,.4f)):pick==2?KitIfClear("rubble_half",p,yaw+rng.Next(-20,20),root,.55f,new Vector3(1.1f,.8f,.8f)):KitIfClear("barrel_small_stack",p,yaw,root,.7f,new Vector3(.7f,.6f,.4f));
                        if(placed&&pick<2)Kit("bottle_C_brown",p+Vector3.up*.05f+Quaternion.Euler(0,yaw,0)*Vector3.forward*.9f,0,root,.5f);
                        break;
                    case CryptTheme.RuinedChapel:
                        placed=pick<2?KitIfClear("pillar_decorated",p,yaw,root,.75f,new Vector3(.8f,1.4f,.6f)):pick==2?KitIfClear("wall_arched",p+Quaternion.Euler(0,yaw,0)*Vector3.back*.4f,yaw,root,.85f,new Vector3(1.7f,1.6f,.4f)):null;
                        Kit(rng.Next(2)==0?"banner_thin_red":"banner_patternA_red",p+Quaternion.Euler(0,yaw,0)*Vector3.back*1.1f+Vector3.up*.2f,yaw,root,.75f);
                        break;
                    case CryptTheme.ExecutionHall:
                        placed=pick==0?KitIfClear("table_long_broken",p,yaw+90,root,.7f,new Vector3(1.6f,.5f,.8f)):pick==1?KitIfClear("keg_decorated",p,yaw,root,.6f,new Vector3(1.1f,.6f,.6f)):pick==2?KitIfClear("trunk_large_A",p,yaw+rng.Next(-15,15),root,.8f,new Vector3(.6f,.4f,.5f)):KitIfClear("crates_stacked",p,yaw,root,.7f,new Vector3(.75f,.75f,.75f));
                        if(placed&&pick!=1)Kit("sword_shield_broken",p+Quaternion.Euler(0,yaw,0)*Vector3.back*.8f+Vector3.up*.6f,yaw,root,.7f);
                        break;
                    case CryptTheme.DrownedGarden:
                        Kit(rng.Next(2)==0?"floor_tile_small_weeds_A":"floor_dirt_small_weeds",p+Quaternion.Euler(0,yaw,0)*Vector3.forward*1.6f,rng.Next(4)*90,root,1);
                        placed=pick<2?KitIfClear("column",p,0,root,1.2f,new Vector3(.4f,.8f,.4f)):KitIfClear("barrier_half",p,yaw,root,.9f,new Vector3(.9f,.5f,.25f));
                        break;
                }
                if(placed&&rng.Next(3)==0)Kit(rng.Next(2)==0?"candle_triple":"candle_melted",p+Quaternion.Euler(0,yaw,0)*Vector3.forward*1.2f+Vector3.right*(rng.Next(2)==0?-.9f:.9f),0,root,.55f);
                if(placed&&light<2&&rng.Next(2)==0){light++;Glow(p+Vector3.up*2.2f+Quaternion.Euler(0,yaw,0)*Vector3.forward*1.5f,theme==CryptTheme.DrownedGarden?new Color(.45f,.75f,.6f):new Color(1f,.58f,.3f),2f,9,root);}
            }
        }
        void SideCrypt(int room,Vector3 c,ChamberPlan plan,System.Random rng,Transform root)
        {
            // Off-route reliquary crypt: coffins along the flanks, an ossuary wall and a warm candle shrine at the far end.
            int away=System.Math.Sign(Layout.Rooms[room].x-Layout.Rooms[Layout.Links.Find(e=>e.y==room).x].x);
            var far=c+Vector3.right*away*(plan.width*4-1.2f);float faceIn=away>0?-90:90;
            Kit("wall_archedwindow_gated",far+Vector3.right*away*.6f,faceIn+180,root);
            Kit("shelf_small_candles",far+Vector3.up*1.2f,faceIn+180,root,.9f);
            foreach(int z in new[]{-1,1})
            {
                Module("BurialCoffin",c+new Vector3(away*4,0,z*5.2f),0,root);
                Kit("candle_triple",c+new Vector3(away*6.5f,0,z*5.4f),0,root,.6f);
                Kit("banner_shield_red",c+new Vector3(away*2,0,z*(plan.depth*4+1.5f)),z<0?0:180,root,.7f);
                Kit("pillar",c+new Vector3(-away*4,0,z*(plan.depth*4-1)),0,root,.6f);
            }
            Kit("coin_stack_small",far+Vector3.right*(-away)*1.4f,rng.Next(360),root,.8f);
            Glow(far+Vector3.up*2.4f,new Color(1f,.55f,.28f),3.2f,12,root);
            Glow(c+Vector3.up*4,new Color(.5f,.42f,.6f),1.6f,14,root);
        }
    }
}
