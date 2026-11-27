using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
    public sealed class AshenSigil:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var center=r.center;float scale=Mathf.Min(r.width,r.height)/200;
            void Line(Vector2 a,Vector2 b,float w){a=center+a*scale;b=center+b*scale;var n=new Vector2(-(b-a).y,(b-a).x).normalized*w*scale/2;int k=vh.currentVertCount;vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);}
            for(int i=0;i<90;i++){float a=i*Mathf.PI*2/90,b=(i+1)*Mathf.PI*2/90;if(i%30<25)Line(new Vector2(Mathf.Cos(a)*65,Mathf.Sin(a)*65),new Vector2(Mathf.Cos(b)*65,Mathf.Sin(b)*65),.7f);}
            Line(new Vector2(0,-92),new Vector2(0,91),1.3f);
            for(int side=-1;side<=1;side+=2){Line(new Vector2(0,-36),new Vector2(side*35,12),1);Line(new Vector2(side*35,12),new Vector2(side*17,52),1);Line(new Vector2(side*17,52),new Vector2(0,72),1);Line(new Vector2(0,-8),new Vector2(side*16,18),1);}
        }
    }
}
