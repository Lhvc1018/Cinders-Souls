using System.Collections.Generic;
using Codice.Multiplayer;
using Codice.Presentation;
using UnityEngine;

namespace Codice.Characters
{
    public sealed partial class PlayerCombat
    {
        int swingSequence, acceptedSwing;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public int GuestSwingsAccepted {get;private set;}
        public int GuestHitReports {get;private set;}
        public int GuestContactsApplied {get;private set;}
        public string LastContactCheck {get;private set;}
#endif
        void ContactCheck(string reason)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            LastContactCheck=reason;
#endif
        }
        bool startingNetworkSwing;
        float predictedContact, predictedEnd;
        WeaponInstance predictedWeapon;
        Vector3 predictedForward;
        readonly HashSet<int> observedTargets=new HashSet<int>();

        void StartLocalSwing(WeaponInstance weapon,float duration)
        {
            predictedWeapon=weapon;
            var lockOn=player.GetComponent<PlayerLockOn>();
            predictedForward=lockOn&&lockOn.Target?(lockOn.Point-player.transform.position).normalized:player.transform.forward;
            predictedContact=Time.time+duration*ContactStart(weapon.kind);
            predictedEnd=Time.time+duration*ContactEnd(weapon.kind);
            var pose=GetComponent<ActorPresentation>();
            if(pose && pose.TryAuthoredContactWindow(out var start,out var end))
            {
                predictedContact=Time.time+duration*start;
                predictedEnd=Time.time+duration*end;
            }
            observedTargets.Clear();
            LanSession.Instance.ReportSwing(++swingSequence,weapon,predictedForward);
        }

        void TrackLocalSwing()
        {
            if(predictedWeapon==null)return;
            if(player.IsDashing||player.IsStaggered||!player.State.IsAlive||Blocking||
                player.ControlsBlocked||player.State.ActiveWeapon!=predictedWeapon||Time.time>predictedEnd)
            { predictedWeapon=null; return; }
            if(Time.time<predictedContact)return;
            LanSession.Instance.ObserveMelee(player,swingSequence,predictedWeapon,predictedForward,observedTargets);
        }

        public void StartGuestSwing(int sequence,int kind,Vector3 forward)
        {
            if(sequence<=acceptedSwing)return;
            acceptedSwing=sequence;
            var weapon=player.State.ActiveWeapon;
            if(!BufferedMelee(weapon)||(int)weapon.kind!=kind)return;
            var planar=Vector3.ProjectOnPlane(forward,Vector3.up);
            if(planar.sqrMagnitude<.0001f)return;
            transform.rotation=Quaternion.LookRotation(planar);
            startingNetworkSwing=true;
            try
            {
                if(TryAttack())
                {
                    attackDirection=planar.normalized;
                    networkSwing=sequence;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    GuestSwingsAccepted++;
#endif
                }
                else ContactCheck("host action on cooldown or interrupted");
            }
            finally { startingNetworkSwing=false; }
        }

        int networkSwing;
        public void VerifyGuestContact(int sequence,Component target)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            GuestHitReports++;
#endif
            ContactCheck("inactive swing or interrupted");
            if(!target || swinging==null || sequence!=networkSwing || swinging!=player.State.ActiveWeapon ||
                !player.State.IsAlive||player.IsStaggered||player.IsDashing||Blocking||player.ControlsBlocked)return;
            /* The cooldown and contact window belong to the swing accepted on the host. */
            float progress=swingElapsed/Mathf.Max(.001f,swingDuration);
            ContactCheck("outside host contact window");
            if(progress<.04f||progress>.92f)return;
            var receiver=target as IDamageReceiver;
            if(receiver==null||swingHits.Contains(receiver))return;
            var col=target.GetComponent<Collider>()??target.GetComponentInChildren<Collider>();
            if(!col||!col.enabled)return;
            var origin=HumanPreviewCamera.Origin(player);
            var point=col.ClosestPoint(origin);
            var offset=point-origin;
            float reach=(ShieldStrike?1.5f:MeleeRange(swinging.kind))+.65f;
            ContactCheck("target outside host reach");
            if(offset.sqrMagnitude>reach*reach)return;
            var aim=Vector3.ProjectOnPlane(col.bounds.center-origin,Vector3.up);
            float arc=swinging.kind==WeaponKind.Axe||swinging.kind==WeaponKind.Hammer||swinging.kind==WeaponKind.Scythe?65:
                swinging.kind==WeaponKind.Dagger||swinging.kind==WeaponKind.Polearm?30:45;
            ContactCheck("target outside swing arc");
            if(Vector3.Angle(Vector3.ProjectOnPlane(attackDirection,Vector3.up),aim)>arc+5)return;
            foreach(var hit in Physics.RaycastAll(origin,offset.normalized,offset.magnitude,hitMask,QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.IsChildOf(transform)||hit.transform.IsChildOf(target.transform))continue;
                if(hit.collider.GetComponentInParent<IDamageReceiver>()==null){ContactCheck("cover: "+hit.collider.name);return;}
            }
            if(swingHits.Add(receiver))
            {
                Hit(col,swinging,offset.normalized);
                ContactCheck("damage applied");
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                GuestContactsApplied++;
#endif
            }
        }
    }
}
