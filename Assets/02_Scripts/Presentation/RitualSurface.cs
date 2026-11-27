using System.Collections.Generic;
using UnityEngine;
namespace Codice.Presentation
{
    // Locally owned geometry and materials; the combat hazard remains the authoritative radius.
    public sealed class RitualSurface:MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Transform> blisters=new List<Transform>();
        public static void Portal(Transform parent)
        {
            var root=new GameObject("Umbral de piedra y velo");root.transform.SetParent(parent,false);
            var art=root.AddComponent<RitualSurface>();var stone=art.Material(new Color(.18f,.19f,.2f),.15f,.35f);var metal=art.Material(new Color(.32f,.24f,.12f),.65f,.6f);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)art.Block(new Vector3(side*1.45f,.25f+i*.48f,0),new Vector3(.48f,.46f,.7f),stone,Quaternion.Euler(0,side*(i%2)*3,0));
            for(int i=0;i<9;i++){float a=i*Mathf.PI/8;art.Block(new Vector3(Mathf.Cos(a)*1.45f,2.3f+Mathf.Sin(a)*1.25f,0),new Vector3(.5f,.48f,.7f),stone,Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90));}
            foreach(int side in new[]{-1,1})art.Block(new Vector3(side*1.45f,1.3f,-.38f),new Vector3(.13f,1.6f,.08f),metal,Quaternion.identity);
            var shader=Resources.Load<Shader>("Presentation/AshenMembrane");if(!shader)return;
            var veil=new Material(shader);art.materials.Add(veil);var mesh=art.Disc(1,48,false);var face=new GameObject("Velo animado");face.transform.SetParent(root.transform,false);face.transform.localPosition=new Vector3(0,1.65f,0);face.transform.localRotation=Quaternion.Euler(90,0,0);face.transform.localScale=new Vector3(1.2f,1,1.65f);face.AddComponent<MeshFilter>().sharedMesh=mesh;face.AddComponent<MeshRenderer>().sharedMaterial=veil;
        }
        public static void Pool(Transform parent,float radius,bool poison)
        {
            var root=new GameObject(poison?"Resina venenosa y vesículas":"Sangre coagulada y vesículas");root.transform.SetParent(parent,false);root.transform.localScale=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1/parent.lossyScale.z);
            var art=root.AddComponent<RitualSurface>();var wet=art.Material(poison?new Color(.12f,.19f,.035f):new Color(.19f,.007f,.017f),.2f,.92f);
            var mesh=art.Disc(radius,64,true);root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=wet;
            for(int i=0;i<18;i++){float a=i*2.39996f,r=radius*Mathf.Sqrt((i+.5f)/18)*.82f;var bubble=GameObject.CreatePrimitive(PrimitiveType.Sphere);bubble.name="Vesícula";bubble.transform.SetParent(root.transform,false);bubble.transform.localPosition=new Vector3(Mathf.Cos(a)*r,.05f,Mathf.Sin(a)*r);float size=.08f+(i%4)*.035f;bubble.transform.localScale=new Vector3(size*2,size,size*2);Destroy(bubble.GetComponent<Collider>());bubble.GetComponent<Renderer>().sharedMaterial=wet;art.blisters.Add(bubble.transform);}
        }
        Material Material(Color color,float metallic,float smooth){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);materials.Add(m);return m;}
        void Block(Vector3 p,Vector3 size,Material m,Quaternion rotation){var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name="Dovela";b.transform.SetParent(transform,false);b.transform.localPosition=p;b.transform.localRotation=rotation;b.transform.localScale=size;b.GetComponent<Renderer>().sharedMaterial=m;Destroy(b.GetComponent<Collider>());}
        Mesh Disc(float radius,int count,bool uneven)
        {
            var v=new Vector3[count+1];var uv=new Vector2[count+1];var t=new int[count*3];uv[0]=Vector2.one*.5f;
            for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;float r=radius*(uneven?.96f+.04f*Mathf.Sin(a*7):1);v[i+1]=new Vector3(Mathf.Cos(a)*r,uneven?.035f+.018f*Mathf.Sin(a*5):0,Mathf.Sin(a)*r);uv[i+1]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.5f+Vector2.one*.5f;t[i*3]=0;t[i*3+1]=(i+1)%count+1;t[i*3+2]=i+1;}
            var mesh=new Mesh{name="Superficie ritual"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
        }
        void Update(){for(int i=0;i<blisters.Count;i++){var s=blisters[i].localScale;s.y=(.08f+(i%4)*.035f)*(.7f+.3f*Mathf.Sin(Time.time*2+i));blisters[i].localScale=s;}}
        void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);foreach(var mesh in meshes)if(mesh)Destroy(mesh);}
    }
}
