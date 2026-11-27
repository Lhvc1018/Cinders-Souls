using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Codice.Characters;
using Codice.Bosses;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.Presentation;

namespace Codice.Multiplayer
{
    public enum DefenseTiming { Unspecified=-1, Open, Roll, Guard, Deflect, Perfect }
    public sealed partial class LanSession
    {
        const string DefenseCheck="Combat:Contact";
        sealed class PendingContact
        {
            public int id,revision;
            public PlayerCharacter player;
            public DamageInfo hit;
            public EnemyProjectile projectile;
            public float deadline;
        }
        readonly List<PendingContact> contacts=new List<PendingContact>();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal DefenseTiming LastTestDefense{get;private set;}
#endif
        int contactSerial,localGuardPress;
        bool guardWasHeld;
        readonly Dictionary<int,int> usedGuardPress=new Dictionary<int,int>();
        readonly Queue<LanMessage> incomingContacts=new Queue<LanMessage>();
        float worldReceived;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal int ObservedMeleeReports {get;private set;}
        internal string LastMeleeObservation {get;private set;}
#endif
        public void ReportSwing(int sequence,WeaponInstance weapon,Vector3 forward)
        {
            SendCombat(new LanMessage{type="swingStart",id=sequence,slot=(int)weapon.kind,point=forward});
        }

        void SendCombat(LanMessage message)
        {
            message.floor=FloorNumber;
            message.scene=loadedScene;
            message.worldRevision=worldRevision;
            Send(message);
        }

        public void ObserveMelee(PlayerCharacter player,int sequence,WeaponInstance weapon,Vector3 forward,HashSet<int> seen)
        {
            var origin=HumanPreviewCamera.Origin(player);
            foreach(var pair in ghosts)
            {
                var g=pair.Value;
                if(!g||g.snapshot==null||g.snapshot.health<=0||seen.Contains(pair.Key))continue;
                var key=g.snapshot.key;
                if(!key.StartsWith("Enemy:")&&!key.StartsWith("Boss:"))continue;
                /* Ghosts have no gameplay colliders; use the host collider bounds, anchored to the rendered pose. */
                var e=g.snapshot;
                var bounds=new Bounds(g.transform.position+e.hitCenter,e.hitSize);
                var point=bounds.ClosestPoint(origin);
                var offset=point-origin;
                float range=PlayerCombat.MeleeRange(weapon.kind);
                float angle=Vector3.Angle(Vector3.ProjectOnPlane(forward,Vector3.up),Vector3.ProjectOnPlane(bounds.center-origin,Vector3.up));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if(e.name=="Coop melee target")LastMeleeObservation=$"distance={offset.magnitude:F2}m angle={angle:F1} bounds={e.hitSize}";
#endif
                if(offset.sqrMagnitude>range*range||angle>65)continue;
                bool blocked=false;
                foreach(var hit in Physics.RaycastAll(origin,offset.normalized,offset.magnitude,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.transform.IsChildOf(player.transform)&&!hit.transform.IsChildOf(g.transform))
                    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        if(e.name=="Coop melee target")LastMeleeObservation="cover: "+hit.collider.name;
#endif
                        blocked=true;break;
                    }
                if(blocked)continue;
                seen.Add(pair.Key);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                ObservedMeleeReports++;
#endif
                SendCombat(new LanMessage{type="meleeHit",id=sequence,interactEntity=pair.Key});
                PresentationVfx.Impact(point,-forward);
            }
        }

        static LanEntity WithHitBounds(LanEntity entity,Component target)
        {
            var col=target.GetComponent<Collider>()??target.GetComponentInChildren<Collider>();
            if(col){entity.hitCenter=col.bounds.center-target.transform.position;entity.hitSize=col.bounds.size;}
            else {entity.hitCenter=Vector3.up;entity.hitSize=new Vector3(1,2,1);}
            return entity;
        }

        bool HandleCombat(Peer peer,LanMessage m)
        {
            if(m.type!="swingStart"&&m.type!="meleeHit"&&m.type!="defenseAnswer")return false;
            if(floorHeld||!peer.player||m.scene!=loadedScene||m.worldRevision!=worldRevision||m.floor!=FloorNumber)return true;
            if(m.type=="swingStart"&&Finite(m.point.x)&&Finite(m.point.y)&&Finite(m.point.z)&&m.point.sqrMagnitude<1.1f)
                peer.player.GetComponent<PlayerCombat>().StartGuestSwing(m.id,m.slot,m.point);
            else if(m.type=="meleeHit")
            {
                var target=objectIds.FirstOrDefault(k=>k.Value==m.interactEntity).Key as Component;
                if(target is BasicEnemy e&&e.IsAlive||target is BossCharacter b&&b.IsAlive)
                    peer.player.GetComponent<PlayerCombat>().VerifyGuestContact(m.id,target);
            }
            else if(m.type=="defenseAnswer")
            {
                int index=contacts.FindIndex(c=>c.player==peer.player&&c.id==m.id&&c.revision==worldRevision);
                if(index<0)return true;
                var pendingHit=contacts[index];contacts.RemoveAt(index);
                var result=(DefenseTiming)Mathf.Clamp(m.slot,0,(int)DefenseTiming.Perfect);
                bool shinobi=peer.player.State.Class==PlayerClass.Shinobi;
                bool hasGuard=shinobi||peer.player.State.Class==PlayerClass.Warrior||peer.player.State.ActiveWeapon?.kind==WeaponKind.Shield;
                if(result>=DefenseTiming.Guard&&(!hasGuard||pendingHit.hit.breaksGuard||pendingHit.hit.ignoreGuard))result=DefenseTiming.Open;
                if(pendingHit.projectile&&result!=DefenseTiming.Roll&&result!=DefenseTiming.Perfect&&!(shinobi&&result==DefenseTiming.Deflect))result=DefenseTiming.Open;
                if(result>=DefenseTiming.Deflect)
                {
                    if(m.choice<=0||usedGuardPress.TryGetValue(peer.id,out int old)&&m.choice<=old)result=DefenseTiming.Guard;
                    else usedGuardPress[peer.id]=m.choice;
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                LastTestDefense=result;
#endif
                FinishContact(pendingHit,result);
            }
            return true;
        }

        void FinishContact(PendingContact contact,DefenseTiming timing)
        {
            if(contact.projectile){
                contact.projectile.ResolveContact(contact.player,contact.hit,timing);
                return;
            }
            contact.player.ResolveOwnedDefense(contact.hit,timing);
        }

        public bool AskDefense(PlayerCharacter target,DamageInfo hit,EnemyProjectile projectile=null)
        {
            if(!host||!context||!target.State.IsAlive||!hit.source||hit.secondary||hit.amount<=0)return false;
            var peer=peers.FirstOrDefault(p=>p.accepted&&p.player==target);
            if(peer==null||contacts.Count>=96)return false;
            var entry=new PendingContact{id=++contactSerial,revision=worldRevision,player=target,hit=hit,projectile=projectile,
                deadline=Time.unscaledTime+Mathf.Clamp(peer.snapshotRtt*2+.4f,.55f,1.5f)};
            contacts.Add(entry);
            peer.wire.Send(JsonUtility.ToJson(new LanMessage{type="effect",name=DefenseCheck,id=entry.id,
                scene=loadedScene,worldRevision=worldRevision,floor=FloorNumber,sampleTime=Time.unscaledTime,pos=hit.direction,
                attack=hit.melee,special=projectile!=null,heavy=hit.breaksGuard,block=hit.ignoreGuard}));
            return true;
        }

        void TickCombatContacts()
        {
            if(host)
            {
                for(int i=contacts.Count-1;i>=0;i--)
                {
                    var c=contacts[i];
                    if(c.player&&c.revision==worldRevision&&Time.unscaledTime<c.deadline)continue;
                    contacts.RemoveAt(i);
                    if(c.player&&c.revision==worldRevision)FinishContact(c,DefenseTiming.Open);
                    else if(c.projectile)Destroy(c.projectile.gameObject);
                }
                return;
            }
        }

        struct GuardFrame
        {
            public float time;
            public DefenseTiming timing;
            public Vector3 forward;
            public int press;
        }
        readonly GuardFrame[] guardFrames=new GuardFrame[256];
        int guardHead,guardCount,guardFloor=-1;
        internal float DisplayedTime=>pending==null?0:pending.sampleTime+Mathf.Min(.3f,Time.unscaledTime-worldReceived)-.12f;

        void LateUpdate()
        {
            if(!client||!context||!context.player||pending==null||pending.worldRevision!=worldRevision)return;
            if(guardFloor!=worldRevision){guardFloor=worldRevision;guardCount=guardHead=0;}
            var player=context.player;
            bool held=GameInput.Held(GameAction.Block);
            if(held&&!guardWasHeld)localGuardPress++;
            guardWasHeld=held;
            float shown=DisplayedTime;
            var frame=new GuardFrame{time=shown,timing=player.OwnedDefense(),forward=player.transform.forward,press=localGuardPress};
            var previous=guardFrames[(guardHead+guardFrames.Length-1)%guardFrames.Length];
            if(guardCount==0||shown>=previous.time+.005f||frame.timing!=previous.timing||frame.press!=previous.press)
            {
                if(guardCount>0)frame.time=Mathf.Max(shown,previous.time);
                guardFrames[guardHead]=frame;guardHead=(guardHead+1)%guardFrames.Length;
                guardCount=Mathf.Min(guardCount+1,guardFrames.Length);
            }
            while(incomingContacts.Count>0)
            {
                var m=incomingContacts.Peek();
                if(m.worldRevision!=worldRevision||m.scene!=loadedScene){incomingContacts.Dequeue();continue;}
                if(m.sampleTime>shown&&Time.unscaledTime-m.lifetime<.32f)break;
                incomingContacts.Dequeue();
                var at=frame;
                for(int i=1;i<=guardCount;i++){
                    at=guardFrames[(guardHead+guardFrames.Length-i)%guardFrames.Length];
                    if(at.time<=m.sampleTime)break;
                }
                var result=at.timing;
                float facing=Vector3.Dot(at.forward,-Vector3.ProjectOnPlane(m.pos,Vector3.up).normalized);
                float threshold=m.special?.3f:player.State.Class==PlayerClass.Shinobi?-.15f:.5f;
                if(result>=DefenseTiming.Guard&&(m.heavy||m.block||(!m.attack&&!m.special)||facing<threshold))result=DefenseTiming.Open;
                SendCombat(new LanMessage{type="defenseAnswer",id=m.id,slot=(int)result,choice=at.press});
            }
        }
    }
}
