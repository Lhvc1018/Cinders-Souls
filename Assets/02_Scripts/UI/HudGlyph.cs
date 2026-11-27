using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
    public enum HudSymbol { Flask, Censer, Bell, Lance, Ward, Entrance, Chest, Boss, Special, Merchant }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudGlyph:MaskableGraphic
    {
        public HudSymbol Symbol;
        public void Set(HudSymbol symbol){if(Symbol==symbol)return;Symbol=symbol;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;float unit=Mathf.Min(r.width,r.height)/12;
            void Box(float x,float y,float w,float h,Color tint)
            {
                int n=mesh.currentVertCount;Vector2 a=r.center+new Vector2(x-6,y-6)*unit;var size=new Vector2(w,h)*unit;
                mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(a+new Vector2(0,size.y),tint,Vector2.zero);mesh.AddVert(a+size,tint,Vector2.zero);mesh.AddVert(a+new Vector2(size.x,0),tint,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
            Color gold=new Color(.9f,.64f,.2f),ivory=new Color(.87f,.83f,.69f),shadow=new Color(.13f,.1f,.07f);
            if(Symbol==HudSymbol.Merchant){Box(2,2,8,6,gold);Box(1,8,10,2,new Color(.62f,.24f,.17f));Box(2,1,1,8,ivory);Box(9,1,1,8,ivory);Box(5,4,2,2,shadow);}
            else if(Symbol==HudSymbol.Entrance){var green=new Color(.15f,.78f,.35f);Box(2,1,8,6,green);for(int i=0;i<4;i++)Box(1+i,7+i,10-i*2,1,green);Box(5,1,2,4,shadow);}
            else if(Symbol==HudSymbol.Chest){Box(1,2,10,8,gold);Box(2,6,8,3,new Color(1,.8f,.3f));Box(1,5,10,1,shadow);Box(5,4,2,3,ivory);}
            else if(Symbol==HudSymbol.Boss){var purple=new Color(.52f,.32f,.65f);Box(2,2,8,8,purple);Box(1,8,2,3,purple);Box(9,8,2,3,purple);Box(3,5,6,2,new Color(.95f,.16f,.16f));Box(5,1,2,4,shadow);}
            else if(Symbol==HudSymbol.Special){Box(5,4,2,7,gold);Box(5,1,2,2,gold);}
            else if(Symbol==HudSymbol.Flask){Box(4,9,4,2,ivory);Box(5,7,2,2,gold);Box(3,5,6,2,gold);Box(2,2,8,3,gold);Box(3,1,6,1,gold);Box(3,3,2,3,new Color(1,.87f,.45f));}
            else if(Symbol==HudSymbol.Bell){Box(5,10,2,1,ivory);Box(4,8,4,2,ivory);Box(3,3,6,5,new Color(.49f,.67f,.73f));Box(2,2,8,1,ivory);Box(5,0,2,2,gold);}
            else if(Symbol==HudSymbol.Lance){for(int i=0;i<8;i++)Box(2+i,1+i,1,2,new Color(.72f,.22f,.23f));Box(8,8,3,3,ivory);Box(2,1,3,1,gold);}
            else if(Symbol==HudSymbol.Ward){Box(3,4,6,6,ivory);Box(4,2,4,2,ivory);Box(5,1,2,1,ivory);Box(5,4,2,5,gold);}
            else{Box(3,2,6,4,gold);Box(4,1,4,1,gold);Box(2,6,8,1,ivory);Box(4,8,1,2,ivory);Box(7,9,1,2,ivory);}
        }
        public static HudGlyph Create(Transform parent,HudSymbol symbol,float x,float y,float size)
        {var rect=UIWidgets.Rect(parent,symbol.ToString(),x,y,size,size);var icon=rect.gameObject.AddComponent<HudGlyph>();icon.Symbol=symbol;icon.raycastTarget=false;return icon;}
    }
}
