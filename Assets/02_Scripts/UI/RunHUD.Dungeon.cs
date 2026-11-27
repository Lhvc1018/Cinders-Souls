using Codice.Characters;
using Codice.Expedition;
using Codice.Mechanics;
using TMPro;
using UnityEngine;
namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        RectTransform dungeonMapPanel,flaskPanel;
        TextMeshProUGUI mapHeading,mapLegend,flaskText;
        UnityEngine.UI.Image flaskProgress;
        DungeonMapGraphic dungeonMap;
        EstusFlask flask;
        void BuildDungeonHud()
        {
            dungeonMapPanel=UIWidgets.Rect(transform,"Dungeon minimap",-246,36,210,245);Anchor(dungeonMapPanel,1,1);
            var graphic=UIWidgets.Rect(dungeonMapPanel,"Room graph",0,0,210,208);dungeonMap=graphic.gameObject.AddComponent<DungeonMapGraphic>();dungeonMap.raycastTarget=false;
            mapHeading=UIWidgets.Label(dungeonMapPanel,"Floor and room",0,212,210,32,22,new Color(.9f,.88f,.8f));mapHeading.alignment=TextAlignmentOptions.Center;
            flaskPanel=UIWidgets.Surface(transform,"Ash flask",48,185,290,92);
            flaskText=UIWidgets.Label(flaskPanel,"Charges",15,12,260,59,20,new Color(.94f,.69f,.32f));
            flaskProgress=UIWidgets.Filled(flaskPanel,"Drinking",15,76,260,4,new Color(1,.6f,.18f));
            dungeonMapPanel.gameObject.SetActive(false);flaskPanel.gameObject.SetActive(false);
        }
        void RefreshDungeonHud()
        {
            var floor=director?director.Expedition:null;bool visible=floor&&floor.Layout!=null;
            dungeonMapPanel.gameObject.SetActive(visible);flaskPanel.gameObject.SetActive(false);
            if(!visible)return;
            if(!flask)flask=Player.GetComponent<EstusFlask>();
            dungeonMap.Floor=floor;dungeonMap.Refresh();
            mapHeading.text=$"{(floor.FloorNumber-1)/3+1}–{(floor.FloorNumber-1)%3+1}";
            flaskText.text=$"[H]  FRASCO DE CENIZA   {Player.State.EstusCharges}/3\n<size=17>{(flask&&flask.Drinking?"Bebiendo · El daño interrumpe":"Cura 40% · Reponer en la fuente")}</size>";
            flaskProgress.fillAmount=flask?flask.Progress:0;
        }
    }
    /// Minimap drawn from the instantiated layout itself (room cells, real room sizes, real corridor cells),
    /// uniformly scaled so shapes and distances match the level. Rebuilt whenever exploration state changes;
    /// the player marker (white arrow) moves every frame.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DungeonMapGraphic:UnityEngine.UI.MaskableGraphic
    {
        public ExpeditionFloor Floor;
        int stateHash;
        Vector2[] centers=new Vector2[0];
        HudGlyph[] glyphs;
        UnityEngine.UI.Image marker;
        float scale=1;Vector2 origin;
        ExpeditionLayout cachedLayout;
        bool Hidden(int room)=>room==Floor.Layout.SecretRoom&&!Floor.SecretRevealed||room==Floor.Layout.VaultRoom&&!Floor.VaultOpen;
        public void Refresh()
        {
            if(!Floor||Floor.Layout==null)return;
            var layout=Floor.Layout;
            if(layout!=cachedLayout){cachedLayout=layout;stateHash=0;if(glyphs!=null)foreach(var g in glyphs)if(g)Destroy(g.gameObject);glyphs=null;}
            UpdateMarker();
            int hash=Floor.Seed^(Floor.PlayerRoom*7919)^(Floor.SecretRevealed?16384:0)^(Floor.VaultOpen?32768:0)^(Floor.CurrentRoom*131)^(Floor.InEncounter?65536:0);
            for(int i=0;i<Floor.RoomCount;i++)hash=unchecked(hash*31+(Floor.Visited[i]?1:0)+(Floor.Cleared[i]?2:0)+(Floor.Chests[i]&&Floor.Chests[i].Opened?4:0));
            if(hash==stateHash&&glyphs!=null)return;stateHash=hash;
            int n=Floor.RoomCount;if(centers.Length!=n)centers=new Vector2[n];
            if(glyphs==null){glyphs=new HudGlyph[n];for(int i=0;i<n;i++)glyphs[i]=HudGlyph.Create(transform,HudSymbol.Entrance,0,0,15);}
            // Bounds over every cell (rooms + corridors) in grid units (1 cell = 4 m).
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
            foreach(var c in layout.Cells){minX=Mathf.Min(minX,c.x-.5f);maxX=Mathf.Max(maxX,c.x+.5f);minY=Mathf.Min(minY,c.y-.5f);maxY=Mathf.Max(maxY,c.y+.5f);}
            var rect=rectTransform.rect;scale=Mathf.Min((rect.width-12)/Mathf.Max(1,maxX-minX),(rect.height-12)/Mathf.Max(1,maxY-minY));
            origin=new Vector2(rect.center.x-(minX+maxX)/2*scale,rect.center.y-(minY+maxY)/2*scale);
            for(int i=0;i<n;i++)
            {
                var c=layout.Rooms[i];centers[i]=origin+new Vector2(c.x,c.y)*scale;
                var icon=glyphs[i];icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=Vector2.zero;icon.rectTransform.pivot=new Vector2(.5f,.5f);icon.rectTransform.anchoredPosition=centers[i]-rect.min;
                bool chest=Floor.Chests[i]&&!Floor.Chests[i].Opened;
                icon.gameObject.SetActive(!Hidden(i)&&(i==0||i==layout.MerchantRoom||i==layout.StatueRoom||i==layout.Boss||chest));
                icon.Set(i==0?HudSymbol.Entrance:i==layout.Boss?HudSymbol.Boss:i==layout.MerchantRoom?HudSymbol.Merchant:i==layout.StatueRoom?HudSymbol.Special:HudSymbol.Chest);
            }
            SetVerticesDirty();
        }
        Vector2 Map(Vector3 world)=>origin+new Vector2(world.x/4,world.z/4)*scale;
        void UpdateMarker()
        {
            if(!marker){marker=new GameObject("You are here",typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();marker.transform.SetParent(transform,false);marker.raycastTarget=false;marker.sprite=ProceduralUISprites.Shape("diamond");marker.color=Color.white;marker.rectTransform.sizeDelta=new Vector2(9,13);marker.rectTransform.anchorMin=marker.rectTransform.anchorMax=Vector2.zero;}
            var player=Floor.context?Floor.context.player:null;if(!player||stateHash==0){marker.enabled=false;return;}
            marker.enabled=true;var rect=rectTransform.rect;
            marker.rectTransform.anchoredPosition=Map(player.transform.position)-rect.min;
            marker.rectTransform.localRotation=Quaternion.Euler(0,0,-player.transform.eulerAngles.y);
            marker.transform.SetAsLastSibling();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();if(!Floor||Floor.Layout==null||centers.Length!=Floor.RoomCount)return;
            var layout=Floor.Layout;
            // Corridors: the actual corridor cells of each link, skipped while one end is still hidden.
            for(int k=0;k<layout.Corridors.Count;k++)
            {
                var link=k<layout.Links.Count?layout.Links[k]:new Vector2Int(-1,-1);
                if(link.x>=0&&(Hidden(link.x)||Hidden(link.y)))continue;
                bool seen=link.x<0||Floor.Visited[link.x]||Floor.Visited[link.y];
                var color=seen?new Color(.55f,.56f,.55f,.9f):new Color(.22f,.24f,.25f,.85f);
                foreach(var c in layout.Corridors[k])
                {
                    if(layout.RoomAt(new Vector3(c.x*4,0,c.y*4))>=0)continue;
                    var p=origin+new Vector2(c.x,c.y)*scale;float w=Mathf.Max(2,scale*.55f);Box(mesh,new Rect(p.x-w/2,p.y-w/2,w,w),color);
                }
            }
            // Rooms at their true footprint.
            for(int i=0;i<Floor.RoomCount;i++)
            {
                if(Hidden(i))continue;
                var plan=layout.Plans[i];var c=centers[i];
                float hw=Mathf.Max(3,(plan.width+.5f)*scale),hd=Mathf.Max(3,(plan.depth+.5f)*scale);
                bool here=i==Floor.PlayerRoom;bool fight=Floor.InEncounter&&i==Floor.CurrentRoom;
                var color=fight?new Color(.78f,.22f,.18f):here?new Color(.86f,.86f,.82f):Floor.Visited[i]?new Color(.55f,.56f,.54f):new Color(.24f,.26f,.27f);
                Box(mesh,new Rect(c.x-hw,c.y-hd,hw*2,hd*2),color);
                if(Floor.Cleared[i]&&!here)Box(mesh,new Rect(c.x+hw-5,c.y-hd+1,4,3),new Color(.85f,.65f,.2f));
            }
        }
        static void Box(UnityEngine.UI.VertexHelper mesh,Rect rect,Color color)
        {
            int start=mesh.currentVertCount;var v=UnityEngine.UIVertex.simpleVert;v.color=color;
            v.position=new Vector3(rect.xMin,rect.yMin);mesh.AddVert(v);v.position=new Vector3(rect.xMax,rect.yMin);mesh.AddVert(v);
            v.position=new Vector3(rect.xMax,rect.yMax);mesh.AddVert(v);v.position=new Vector3(rect.xMin,rect.yMax);mesh.AddVert(v);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
