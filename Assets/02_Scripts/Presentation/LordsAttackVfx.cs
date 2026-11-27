using System.Collections.Generic;
using UnityEngine;

namespace Codice.Presentation
{
    // Source masks: Lords of the Fallen. Emitters and URP rendering are authored for Cinders.
    // Visual only: no colliders, damage, lights, or gameplay ownership.
    public sealed class LordsAttackVfx : MonoBehaviour
    {
        static readonly List<LordsAttackVfx> Live = new List<LordsAttackVfx>();
        static readonly int Tint = Shader.PropertyToID("_Tint");
        Mesh ownedMesh;
        Renderer target;
        MaterialPropertyBlock properties;
        Color color;
        float born, duration;
        Vector3 initialScale;
        bool billboard;
        public static int ActiveCount { get { Live.RemoveAll(x=>!x); return Live.Count; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Live.Clear(); }

        public static bool Slash(Vector3 point, Vector3 forward)
        {
            var material=Resources.Load<Material>("Presentation/LordsVfx/Slash");
            if(!material || ActiveCount>=32)return false;
            var go=new GameObject("Lords mask • blade arc");
            go.transform.SetPositionAndRotation(point,Quaternion.LookRotation(Safe(forward))*Quaternion.Euler(0,0,-12));
            var effect=go.AddComponent<LordsAttackVfx>();
            effect.ownedMesh=Arc();
            go.AddComponent<MeshFilter>().sharedMesh=effect.ownedMesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            effect.Initialize(renderer,new Color(.78f,.65f,.42f,.52f),.2f,false);
            return true;
        }

        // Larger, tinted arc for finishers and the mortal blade; same Lords mask material.
        public static bool SlashTinted(Vector3 point, Vector3 forward, Color tint, float scale, float roll = -12, float life = .28f)
        {
            if (Codice.Multiplayer.LanSession.Host) Codice.Multiplayer.LanSession.Instance.EmitFx("LS:" + roll.ToString(System.Globalization.CultureInfo.InvariantCulture), point, forward, scale, life, tint);
            var material=Resources.Load<Material>("Presentation/LordsVfx/Slash");
            if(!material || ActiveCount>=32)return false;
            var go=new GameObject("Lords mask • heavy arc");
            go.transform.SetPositionAndRotation(point,Quaternion.LookRotation(Safe(forward))*Quaternion.Euler(0,0,roll));
            go.transform.localScale=Vector3.one*scale;
            var effect=go.AddComponent<LordsAttackVfx>();
            effect.ownedMesh=Arc();
            go.AddComponent<MeshFilter>().sharedMesh=effect.ownedMesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            effect.Initialize(renderer,tint,life,false);
            return true;
        }
        public static bool ImpactTinted(Vector3 point, Vector3 forward, Color tint, float size)
        { if (Codice.Multiplayer.LanSession.Host) Codice.Multiplayer.LanSession.Instance.EmitFx("LI:", point, forward, size, 0, tint); return Flash(point,forward,"Impact",tint,size,.16f); }

        public static bool Impact(Vector3 point, Vector3 forward)
        { return Flash(point,forward,"Impact",new Color(1,.69f,.3f,.85f),.36f,.12f); }

        public static bool Cast(Vector3 point, Vector3 forward)
        { return Flash(point,forward,"Core",new Color(.35f,.65f,1,.6f),.32f,.22f); }

        static bool Flash(Vector3 point,Vector3 forward,string asset,Color tint,float size,float lifetime)
        {
            var material=Resources.Load<Material>("Presentation/LordsVfx/"+asset);
            if(!material || ActiveCount>=32)return false;
            var go=new GameObject("Lords mask • "+asset);go.transform.position=point;
            go.transform.rotation=Quaternion.LookRotation(Safe(forward));go.transform.localScale=Vector3.one*size;
            var effect=go.AddComponent<LordsAttackVfx>();effect.ownedMesh=Quad();
            go.AddComponent<MeshFilter>().sharedMesh=effect.ownedMesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            effect.Initialize(renderer,tint,lifetime,true);return true;
        }

        void Initialize(Renderer renderer,Color tint,float lifetime,bool faceCamera)
        {
            target=renderer;color=tint;duration=lifetime;billboard=faceCamera;born=Time.time;
            initialScale=transform.localScale;properties=new MaterialPropertyBlock();Live.Add(this);
            properties.SetColor(Tint,color);target.SetPropertyBlock(properties);
            Destroy(gameObject,lifetime+.05f);
        }
        void Update()
        {
            float t=Mathf.Clamp01((Time.time-born)/duration);
            var faded=color;faded.a*=Mathf.Pow(1-t,2);properties.SetColor(Tint,faded);target.SetPropertyBlock(properties);
            transform.localScale=initialScale*Mathf.Lerp(1,1.15f,t);
            if(billboard && Camera.main)transform.rotation=Camera.main.transform.rotation;
        }
        void OnDestroy(){Live.Remove(this);if(ownedMesh)Destroy(ownedMesh);}
        static Vector3 Safe(Vector3 forward)=>forward.sqrMagnitude>.0001f?forward.normalized:Vector3.forward;
        static Mesh Quad()
        {
            var mesh=new Mesh{name="Lords flash quad"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();return mesh;
        }
        static Mesh Arc()
        {
            const int segments=24;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                float u=i/(float)segments,angle=Mathf.Lerp(-75,75,u)*Mathf.Deg2Rad;
                for(int edge=0;edge<2;edge++)
                {
                    int n=i*2+edge;float r=edge==0?.44f:.86f;
                    vertices[n]=new Vector3(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r-.45f);uv[n]=new Vector2(u,edge);
                    colors[n]=new Color(1,1,1,Mathf.Sin(u*Mathf.PI));
                }
                if(i<segments){int t=i*6,n=i*2;triangles[t]=n;triangles[t+1]=n+1;triangles[t+2]=n+2;triangles[t+3]=n+1;triangles[t+4]=n+3;triangles[t+5]=n+2;}
            }
            var mesh=new Mesh{name="Lords slash arc"};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
        }
    }
}
