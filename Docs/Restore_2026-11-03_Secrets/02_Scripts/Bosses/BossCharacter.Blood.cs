using UnityEngine;
using Codice.Characters;
namespace Codice.Bosses
{
    public sealed partial class BossCharacter
    {
        GameObject bloodTelegraph;
        Vector3 bloodImpact;
        float chargeTime;
        void BeginCharge()
        {
            if(bloodTelegraph)Destroy(bloodTelegraph);
            var path=new UnityEngine.AI.NavMeshPath();var goal=Vector3.ProjectOnPlane(aim,Vector3.up);goal.y=transform.position.y;
            if(!agent.CalculatePath(goal,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete){State=BossState.Recovery;attackTimer=1;CurrentAttack=BossAttack.None;return;}
            agent.stoppingDistance=1.8f;agent.acceleration=24;agent.speed=8.2f;agent.isStopped=false;agent.SetPath(path);chargeTime=1.7f;State=BossState.Charging;
            GetComponent<Codice.Presentation.ActorPresentation>()?.Play("Run",0,true);
        }
        void TickCharge(float dt)
        {
            if(!agent.isOnNavMesh)return;chargeTime-=dt;agent.speed=8.2f;
            if(agent.velocity.sqrMagnitude>.1f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(agent.velocity),300*dt);
            if(chargeTime>0&&(agent.pathPending||agent.remainingDistance>2))return;
            agent.isStopped=true;agent.stoppingDistance=0;facing=transform.forward;bloodImpact=transform.position+facing*1.5f;
            CurrentAttack=BossAttack.Slam;windup=.65f;State=BossState.Windup;ShowBloodTelegraph();
            GetComponent<Codice.Presentation.ActorPresentation>()?.Attack(false,windup);
        }
        void ShowBloodTelegraph()
        {
            if(bloodTelegraph)Destroy(bloodTelegraph);
            bloodImpact=transform.position+facing*2.2f;
            if(CurrentAttack==BossAttack.Slam)bloodTelegraph=BloodVisual.Ring(bloodImpact+Vector3.up*.05f,3.4f,new Color(.95f,.22f,.08f),.055f);
            else if(CurrentAttack==BossAttack.Sweep)bloodTelegraph=BloodVisual.Arc(transform.position+Vector3.up*.05f,3.5f,facing,140,new Color(.95f,.35f,.12f));
            else bloodTelegraph=BloodVisual.Beam(transform.position+Vector3.up*1.6f,aim,new Color(.85f,.06f,.23f),.035f);
            Track(bloodTelegraph);Debug.Log("[Inquisitor] Windup "+CurrentAttack+" phase="+Phase);
        }
        void ExecuteBloodAttack()
        {
            if(bloodTelegraph)Destroy(bloodTelegraph);
            if(!Target||!Target.State.IsAlive)return;
            if(CurrentAttack==BossAttack.Bow)
            {
                int count=Phase==1?1:Phase==2?3:5;var origin=transform.position+Vector3.up*1.65f+facing*.8f;
                for(int i=0;i<count;i++)InquisitorBolt.Create(this,origin,Quaternion.Euler(0,(i-(count-1)*.5f)*9,0)*(aim-origin).normalized);
            }
            else
            {
                bool slam=CurrentAttack==BossAttack.Slam;var delta=Target.transform.position-transform.position;delta.y=0;
                bool hit=slam?Vector3.ProjectOnPlane(Target.transform.position-bloodImpact,Vector3.up).magnitude<=3.4f:delta.magnitude<=3.5f&&Vector3.Dot(facing,delta.normalized)>=.34f;
                if(hit&&HasLineOfSight(Target.transform.position+Vector3.up))Target.ReceiveDamage(new DamageInfo{amount=slam?38:28,source=gameObject,direction=facing,melee=true});
                InquisitorBloodPool.Create(this,slam?bloodImpact:transform.position+facing*2.6f,slam?2.1f:1.35f,6);
                Codice.Mechanics.RelicPulse.Spawn(bloodImpact+Vector3.up*.06f,slam?3.4f:1.7f,new Color(.65f,.04f,.08f));
            }
            Debug.Log("[Inquisitor] Resolved "+CurrentAttack+" phase="+Phase);
        }
        void ClearBloodHazards()
        {
            foreach(var h in hazards)if(h){h.SetActive(false);Destroy(h);}hazards.Clear();bloodTelegraph=null;
        }
    }
    public static class BloodVisual
    {
        public static LineRenderer Line(GameObject root,Color tint,float width)
        {
            var line=root.AddComponent<LineRenderer>();line.sharedMaterial=Material;line.startColor=line.endColor=tint;
            line.startWidth=line.endWidth=width;line.useWorldSpace=true;line.numCapVertices=4;return line;
        }
        static Material material;
        public static Material Material {get{if(!material)material=new Material(Shader.Find("Sprites/Default"));return material;}}
        public static GameObject Ring(Vector3 center,float radius,Color tint,float width)
        {
            var root=new GameObject("Blood warning ring");var line=Line(root,tint,width);line.loop=true;line.positionCount=64;
            for(int i=0;i<64;i++){float a=i*Mathf.PI/32;line.SetPosition(i,center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}return root;
        }
        public static GameObject Arc(Vector3 center,float radius,Vector3 facing,float degrees,Color tint)
        {
            var root=new GameObject("Censer sweep warning");var line=Line(root,tint,.055f);line.positionCount=35;
            line.SetPosition(0,center);for(int i=0;i<33;i++)line.SetPosition(i+1,center+Quaternion.Euler(0,-degrees/2+degrees*i/32,0)*facing*radius);
            line.SetPosition(34,center);return root;
        }
        public static GameObject Beam(Vector3 start,Vector3 end,Color tint,float width)
        {var root=new GameObject("Blood aim");var line=Line(root,tint,width);line.positionCount=2;line.SetPosition(0,start);line.SetPosition(1,end);return root;}
    }
}
