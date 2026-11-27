using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RelicButtonFace : MaskableGraphic
    {
        public bool primary,highlighted,pressed;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Color edge=highlighted?new Color(.78f,.69f,.47f):new Color(.37f,.33f,.23f,.65f);
            void Quad(float x,float y,float w,float h,Color tint){int n=vh.currentVertCount;vh.AddVert(new Vector2(x,y),tint*color,Vector2.zero);vh.AddVert(new Vector2(x+w,y),tint*color,Vector2.zero);vh.AddVert(new Vector2(x+w,y+h),tint*color,Vector2.zero);vh.AddVert(new Vector2(x,y+h),tint*color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
            Quad(r.xMin,r.yMin,r.width,r.height,pressed?new Color(.28f,.24f,.15f,.9f):highlighted?new Color(.27f,.24f,.16f,.8f):primary?new Color(.15f,.14f,.10f,.7f):new Color(.035f,.035f,.03f,.45f));
            Quad(r.xMin,r.yMin,r.width,1,edge);
            if(highlighted||primary){Quad(r.xMin,r.yMax-1,r.width,1,edge);Quad(r.xMin+21,r.center.y-3,6,6,edge);}
        }
    }
}
