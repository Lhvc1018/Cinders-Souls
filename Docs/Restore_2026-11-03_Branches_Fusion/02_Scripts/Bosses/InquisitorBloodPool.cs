using UnityEngine;
using Codice.Characters;
namespace Codice.Bosses
{
    public sealed class InquisitorBloodPool:MonoBehaviour
    {
        public BossCharacter Owner {get;private set;}
        public float Radius {get;private set;}
        public bool Active=>age>=.65f;
        float age,duration;LineRenderer edge;Mesh mesh;
        public static InquisitorBloodPool Create(BossCharacter owner,Vector3 point,float radius,float duration)
        {
            // Place blood on walkable floor, never on a wall or ceiling.
            if(!UnityEngine.AI.NavMesh.SamplePosition(point,out var floor,3,UnityEngine.AI.NavMesh.AllAreas))return null;
            var root=new GameObject("Inquisitor · toxic blood");root.transform.position=floor.position+Vector3.up*.035f;
            var pool=root.AddComponent<InquisitorBloodPool>();pool.Owner=owner;pool.Radius=radius;pool.duration=duration;
            var edge=BloodVisual.Ring(root.transform.position,radius,new Color(.8f,.25f,.1f),.045f);edge.transform.SetParent(root.transform,true);pool.edge=edge.GetComponent<LineRenderer>();
            pool.mesh=new Mesh();var vertices=new Vector3[34];var colors=new Color[34];var triangles=new int[96];
            vertices[0]=Vector3.zero;colors[0]=new Color(.08f,.002f,.008f,.85f);
            for(int i=0;i<33;i++){float a=i*Mathf.PI/16,r=radius*(.9f+.08f*Mathf.Sin(a*5));vertices[i+1]=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);colors[i+1]=new Color(.21f,.005f,.015f,.65f);if(i<32){triangles[i*3]=0;triangles[i*3+1]=i+2;triangles[i*3+2]=i+1;}}
            pool.mesh.vertices=vertices;pool.mesh.colors=colors;pool.mesh.triangles=triangles;pool.mesh.RecalculateNormals();
            root.AddComponent<MeshFilter>().sharedMesh=pool.mesh;root.AddComponent<MeshRenderer>().sharedMaterial=BloodVisual.Material;
            owner.Track(root);return pool;
        }
        void Update()
        {
            if(!Owner||!Owner.IsAlive||!Owner.Target||!Owner.Target.State.IsAlive){Destroy(gameObject);return;}
            if(Owner.Paused||Time.timeScale<=0)return;
            age+=Time.deltaTime;if(age>=duration){Destroy(gameObject);return;}
            var tint=Active?new Color(.38f,.025f,.06f,.75f):new Color(1,.4f,.1f,.7f+.3f*Mathf.Sin(age*20));edge.startColor=edge.endColor=tint;
            var p=Owner.Target;var delta=p.transform.position-transform.position;
            if(Active&&Mathf.Abs(delta.y)<.85f&&Vector3.ProjectOnPlane(delta,Vector3.up).magnitude<Radius&&!p.IsDashing&&Owner.HasLineOfSight(p.transform.position+Vector3.up))
            {var poison=p.GetComponent<InquisitorPoison>()??p.gameObject.AddComponent<InquisitorPoison>();poison.Infect(Owner);}
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
    public sealed class InquisitorPoison:MonoBehaviour
    {
        BossCharacter owner;PlayerCharacter player;float remaining,tick;
        public bool Active=>remaining>0&&owner&&owner.IsAlive;
        void Awake(){player=GetComponent<PlayerCharacter>();}
        public void Infect(BossCharacter source){owner=source;remaining=3;}
        void Update()
        {
            if(!Active||!player||!player.State.IsAlive){remaining=tick=0;return;}
            if(owner.Paused||Time.timeScale<=0)return;
            remaining=Mathf.Max(0,remaining-Time.deltaTime);tick+=Time.deltaTime;
            if(tick>=1){tick-=1;player.ReceiveDamage(new DamageInfo{amount=4,source=owner.gameObject,ignoreGuard=true,poison=true,secondary=true});}
        }
    }
}
