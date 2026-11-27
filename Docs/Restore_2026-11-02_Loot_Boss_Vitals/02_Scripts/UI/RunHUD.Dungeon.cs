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
            mapHeading.text=$"1–{floor.PlayerRoom+1}";
            flaskText.text=$"[H]  FRASCO DE CENIZA   {Player.State.EstusCharges}/3\n<size=17>{(flask&&flask.Drinking?"Bebiendo · El daño interrumpe":"Cura 40% · Reponer en la fuente")}</size>";
            flaskProgress.fillAmount=flask?flask.Progress:0;
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DungeonMapGraphic:UnityEngine.UI.MaskableGraphic
    {
        public ExpeditionFloor Floor;
        int stateHash;
        readonly Vector2[] centers=new Vector2[6];
        readonly Vector2[] sizes=new Vector2[6];
        HudGlyph[] glyphs;
        public void Refresh()
        {
            if(!Floor||Floor.Layout==null)return;
            int hash=Floor.Seed^Floor.PlayerRoom;
            for(int i=0;i<6;i++)hash=unchecked(hash*31+(Floor.Visited[i]?1:0)+(Floor.Cleared[i]?2:0)+(Floor.Chests[i]&&Floor.Chests[i].Opened?4:0));
            if(hash==stateHash&&glyphs!=null)return;stateHash=hash;
            if(glyphs==null){glyphs=new HudGlyph[6];for(int i=0;i<6;i++)glyphs[i]=HudGlyph.Create(transform,HudSymbol.Entrance,0,0,17);}
            float minX=float.MaxValue,maxX=float.MinValue;foreach(var c in Floor.Layout.Rooms){minX=Mathf.Min(minX,c.x);maxX=Mathf.Max(maxX,c.x);}
            var rect=rectTransform.rect;float xScale=Mathf.Min(10,(rect.width-38)/Mathf.Max(1,maxX-minX));
            for(int i=0;i<6;i++)
            {
                var c=Floor.Layout.Rooms[i];centers[i]=new Vector2(rect.xMin+rect.width/2+(c.x-(minX+maxX)/2)*xScale,rect.yMin+15+i*35);
                sizes[i]=new Vector2(24,24);
                var icon=glyphs[i];icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=Vector2.zero;icon.rectTransform.pivot=new Vector2(.5f,.5f);icon.rectTransform.anchoredPosition=centers[i]-rect.min;
                bool chest=Floor.Chests[i]&&!Floor.Chests[i].Opened;
                icon.gameObject.SetActive(i==0||i==2||i==5||chest);
                icon.Set(i==0?HudSymbol.Entrance:i==5?HudSymbol.Boss:i==2?HudSymbol.Special:HudSymbol.Chest);
            }
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();if(!Floor||Floor.Layout==null)return;
            for(int i=1;i<6;i++)
            {
                var a=centers[i-1];var b=centers[i];float middle=(a.y+b.y)/2;var color=Floor.Visited[i]?new Color(.58f,.59f,.58f,.9f):new Color(.24f,.26f,.27f,.9f);
                Box(mesh,new Rect(a.x-3,a.y,6,middle-a.y),color);Box(mesh,new Rect(Mathf.Min(a.x,b.x)-3,middle-3,Mathf.Abs(a.x-b.x)+6,6),color);Box(mesh,new Rect(b.x-3,middle,6,b.y-middle),color);
            }
            for(int i=0;i<6;i++)
            {
                var color=i==Floor.PlayerRoom?new Color(.98f,.98f,.94f):Floor.Visited[i]?new Color(.64f,.65f,.63f):new Color(.26f,.28f,.29f);
                var r=new Rect(centers[i]-sizes[i]/2,sizes[i]);Box(mesh,r,color);
                if(Floor.Cleared[i])Box(mesh,new Rect(r.xMax-5,r.yMin+1,4,3),new Color(.85f,.65f,.2f));
            }
        }
        static void Box(UnityEngine.UI.VertexHelper mesh,Rect rect,Color color)
        {
            int start=mesh.currentVertCount;var v=UnityEngine.UIVertex.simpleVert;v.color=color;
            v.position=new Vector3(rect.xMin,rect.yMin);mesh.AddVert(v);v.position=new Vector3(rect.xMin,rect.yMax);mesh.AddVert(v);
            v.position=new Vector3(rect.xMax,rect.yMax);mesh.AddVert(v);v.position=new Vector3(rect.xMax,rect.yMin);mesh.AddVert(v);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
