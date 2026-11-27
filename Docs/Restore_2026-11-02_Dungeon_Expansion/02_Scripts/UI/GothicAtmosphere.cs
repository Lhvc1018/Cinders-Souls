using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
    // Original architectural silhouettes, drawn without external textures.
    public sealed class GothicAtmosphere : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            void Quad(float x,float y,float w,float h,Color c){int n=mesh.currentVertCount;mesh.AddVert(new Vector3(x,y),c,Vector2.zero);mesh.AddVert(new Vector3(x+w,y),c,Vector2.zero);mesh.AddVert(new Vector3(x+w,y+h),c,Vector2.zero);mesh.AddVert(new Vector3(x,y+h),c,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);}
            for(int i=0;i<9;i++){
                float x=r.xMin+r.width*(.44f+i*.075f), h=r.height*(.35f+.18f*Mathf.Sin(i*2.3f));
                Quad(x,r.yMin,22,h,new Color(.09f,.085f,.095f,.7f));
                for(int j=0;j<8;j++) Quad(x+22+j*3,r.yMin+h-j*8,3,2,new Color(.16f,.13f,.12f,.5f));
            }
            for(int i=0;i<55;i++){
                float x=r.xMin+Mathf.Repeat(i*173.7f,r.width),y=r.yMin+Mathf.Repeat(i*97.3f+Time.unscaledTime*(5+i%4),r.height);
                Quad(x,y,1.5f,2.5f,new Color(.63f,.39f,.26f,.12f+.12f*Mathf.Sin(i+Time.unscaledTime)));
            }
        }
        void Update(){SetVerticesDirty();}
    }
}
