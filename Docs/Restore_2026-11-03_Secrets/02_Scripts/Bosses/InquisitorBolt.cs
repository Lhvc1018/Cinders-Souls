using UnityEngine;
using Codice.Characters;
namespace Codice.Bosses
{
    public sealed class InquisitorBolt:MonoBehaviour
    {
        BossCharacter owner;Vector3 velocity;float age;
        public static void Create(BossCharacter owner,Vector3 origin,Vector3 direction)
        {
            var root=new GameObject("Blood lance projectile");root.transform.position=origin;
            var bolt=root.AddComponent<InquisitorBolt>();bolt.owner=owner;bolt.velocity=direction.normalized*11;
            var line=BloodVisual.Line(root,new Color(.95f,.04f,.12f),.16f);line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,Vector3.zero);line.SetPosition(1,-direction*.65f);
            owner.Track(root);
        }
        void Update()
        {
            if(!owner||!owner.IsAlive||owner.State==BossState.Stunned||!owner.Target||!owner.Target.State.IsAlive){Destroy(gameObject);return;}
            if(owner.Paused||Time.timeScale<=0)return;
            float dt=Time.deltaTime;age+=dt;if(age>4){Destroy(gameObject);return;}
            var step=velocity*dt;var hits=Physics.SphereCastAll(transform.position,.15f,velocity.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.transform.IsChildOf(owner.transform)||hit.transform.IsChildOf(transform))continue;
                var p=hit.collider.GetComponentInParent<PlayerCharacter>();if(p)p.ReceiveDamage(new DamageInfo{amount=18,source=owner.gameObject,direction=velocity.normalized});
                InquisitorBloodPool.Create(owner,hit.point,.95f,4.5f);Destroy(gameObject);return;
            }
            transform.position+=step;
        }
    }
}
