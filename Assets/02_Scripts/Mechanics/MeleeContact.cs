using UnityEngine;
using Codice.Characters;
namespace Codice.Mechanics
{
    public static class MeleeContact
    {
        // World metres, independent of avoidance radius. Resolve at impact, not at windup.
        public static bool CanHit(Transform attacker,PlayerCharacter target,float reach,float minDot)
        {
            if(!attacker||!target||!target.State.IsAlive)return false;
            var delta=target.transform.position-attacker.position;
            if(Mathf.Abs(delta.y)>1.25f)return false;
            var flat=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(flat.magnitude>reach||flat.sqrMagnitude>.01f&&Vector3.Dot(attacker.forward,flat.normalized)<minDot)return false;
            var origin=attacker.position+Vector3.up;
            var end=target.transform.position+Vector3.up;
            foreach(var hit in Physics.RaycastAll(origin,(end-origin).normalized,Vector3.Distance(origin,end),~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(attacker)&&!hit.transform.IsChildOf(target.transform))return false;
            return true;
        }
    }
}
