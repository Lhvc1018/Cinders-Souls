using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Codice.Characters;
using Codice.Mechanics;

namespace Codice.Multiplayer
{
    public sealed partial class LanMessage
    {
        public LanMessage Copy()=>(LanMessage)MemberwiseClone();
        public int catalogRevision, orbs, dialogueStage;
        public string endingTitle, endingText;
        public bool seal, sanctumOpen, reviveHeld;
        public int[] removed;
        public CoopPose[] poses;
    }

    [Serializable] public struct CoopPose
    {
        public int id;
        public Vector3 position, end;
        public Quaternion rotation;
        public string animation;
        public float duration, left;
        public bool forced;
        public Vector3[] points;

        public CoopPose(LanEntity e)
        {
            id=e.id; position=e.position; rotation=e.rotation; animation=e.animation;
            duration=e.animDur; left=e.animLeft; forced=e.forced; end=e.end; points=e.points;
        }

        public void Apply(LanEntity e)
        {
            e.position=position; e.rotation=rotation; e.animation=animation;
            e.animDur=duration; e.animLeft=left; e.forced=forced; e.end=end; e.points=points;
        }
    }

    public sealed partial class LanSession
    {
        sealed class Catalog
        {
            public int generation=-1, revision;
            public string header;
            public readonly Dictionary<int,string> signatures=new Dictionary<int,string>();
        }

        readonly Dictionary<int,LanEntity> remoteCatalog=new Dictionary<int,LanEntity>();
        readonly SortedDictionary<int,LanMessage> waitingCatalog=new SortedDictionary<int,LanMessage>();
        public int LatestPoseSequence{get;private set;}
        int appliedCatalog;
        LanMessage remoteHeader, bufferedPose;
        bool lastReviveHeld;
        readonly Dictionary<PlayerCharacter,(PlayerCharacter ally,float since)> reviving=new Dictionary<PlayerCharacter,(PlayerCharacter,float)>();
        void TickRevival()
        {
            if(!context||floorHeld)return;
            foreach(var player in Players())
            {
                var peer=peers.FirstOrDefault(p=>p.player==player);
                bool held=peer==null?GameInput.Held(GameAction.Interact)&&!(Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.Paused):
                    peer.reviveHeld&&Time.unscaledTime-peer.reviveSeen<.4f;
                var ally=held&&player.State.IsAlive&&!player.ControlsBlocked?FallenAllyNear(player):null;
                if(!ally){reviving.Remove(player);continue;}
                if(!reviving.TryGetValue(player,out var entry)||entry.ally!=ally){reviving[player]=(ally,Time.unscaledTime);continue;}
                if(Time.unscaledTime-entry.since<1f)continue;
                ally.ReviveFromAlly(.4f);reviving.Remove(player);
                EmitEffect("Say:COMPAÑERO REVIVIDO",ally.transform.position,1,0);
                Codice.UI.RunHUD.Instance?.Announce("COMPAÑERO REVIVIDO");
            }
        }
        void ApplyStory(LanMessage m)
        {
            var run=RunDirector.Instance;
            run.NetworkEnding(m.orbs,m.seal,m.endingTitle,m.endingText);
            var floor=context.Expedition;if(!floor)return;
            if(m.dialogueStage>=-1){
                Codice.Expedition.DestinyEncounter.Create(floor);
                floor.GetComponentInChildren<Codice.Expedition.DestinyEncounter>()?.GuestPage(m.dialogueStage);
            }
            if(m.floor==15&&m.cleared!=null&&m.cleared.Length==floor.RoomCount&&m.cleared[floor.Layout.Boss]){
                Codice.Expedition.FinalSanctum.Create(floor);
                if(m.sanctumOpen)floor.GetComponentInChildren<Codice.Expedition.FinalSanctum>()?.GuestOpen();
            }
        }

        static bool IsFlying(string key)=>key=="Shot"||key=="BloodBolt"||key.StartsWith("Projectile:");
        static bool DynamicPose(string key)=>key.StartsWith("Player:")||key.StartsWith("Enemy:")||
            key.StartsWith("Boss:")||key.StartsWith("Companion:")||key=="Line"||key=="Beam"||key=="Gold"||key=="Loot";

        void Publish(Peer peer,LanMessage world,List<LanEntity> entities,float now)
        {
            var catalog=peer.catalog;
            bool reset=catalog.generation!=world.worldRevision;
            if(reset){catalog.signatures.Clear();catalog.header=null;catalog.generation=world.worldRevision;}
            world.entities=null; world.type="catalog";world.id=peer.id;
            float time=world.sampleTime,elapsed=world.elapsed,round=world.roundTime;
            world.seq=world.catalogRevision=0;world.sampleTime=world.elapsed=world.roundTime=0;
            world.removed=null;
            string header=JsonUtility.ToJson(world);
            var changed=new List<LanEntity>();
            var poses=new List<CoopPose>();
            var live=new HashSet<int>();
            foreach(var item in entities)
            {
                live.Add(item.id);
                var meta=item.Copy();
                if(item.owner>=0&&item.owner!=peer.id)meta.state=null;
                bool dynamic=DynamicPose(item.key);
                if(dynamic)poses.Add(new CoopPose(item));
                var signature=meta.Copy();
                if(dynamic||IsFlying(item.key))
                {
                    signature.position=Vector3.zero;signature.rotation=Quaternion.identity;
                    signature.animDur=signature.animLeft=0;signature.forced=false;signature.animation=null;
                    signature.points=null;if(!IsFlying(item.key))signature.end=Vector3.zero;
                }
                signature.flightTime=0;signature.hitCenter=signature.hitSize=Vector3.zero;
                string key=JsonUtility.ToJson(signature);
                if(catalog.signatures.TryGetValue(item.id,out var prior)&&key==prior)continue;
                catalog.signatures[item.id]=key;
                /* Existing moving actors keep their pose; only a spawn or shot redirects carries a new origin. */
                if(prior!=null&&dynamic){signature.hitCenter=item.hitCenter;signature.hitSize=item.hitSize;meta=signature;meta.keepPose=true;}
                if(IsFlying(item.key)&&meta.flightTime<=0)meta.flightTime=time;
                changed.Add(meta);
            }
            var gone=catalog.signatures.Keys.Where(id=>!live.Contains(id)).ToArray();
            foreach(var id in gone)catalog.signatures.Remove(id);
            world.seq=++peer.sentSeq;world.sampleTime=time;world.elapsed=elapsed;world.roundTime=round;
            peer.sendTimes[world.seq%64]=now;peer.sentAt=now;
            if(changed.Count!=0||gone.Length!=0||header!=catalog.header)
            {
                world.catalogRevision=++catalog.revision;
                world.entities=changed.ToArray();world.removed=gone;
                peer.wire.Send(JsonUtility.ToJson(world));
                catalog.header=header;
            }
            var packet=new LanMessage{type="poses",id=peer.id,seq=world.seq,worldRevision=world.worldRevision,
                catalogRevision=catalog.revision,sampleTime=time,elapsed=elapsed,roundTime=round,poses=poses.ToArray()};
            var json=JsonUtility.ToJson(packet);
            peer.wire.SendFast(json);
            LastWorldBytes=json.Length;LastEntities=entities.Count;
        }

        void ApplyPoses(LanMessage packet)
        {
            LatestPoseSequence=packet.seq;
            foreach(var pose in packet.poses??Array.Empty<CoopPose>())
                if(remoteCatalog.TryGetValue(pose.id,out var actor))pose.Apply(actor);
        }

        LanMessage Assemble(LanMessage packet)
        {
            if(packet.type=="catalog")
            {
                if(packet.catalogRevision<=appliedCatalog)return null;
                waitingCatalog[packet.catalogRevision]=packet;
                bool advanced=false;
                while(waitingCatalog.TryGetValue(appliedCatalog+1,out var next))
                {
                    waitingCatalog.Remove(++appliedCatalog);
                    if(remoteHeader==null||next.worldRevision!=remoteHeader.worldRevision)remoteCatalog.Clear();
                    foreach(int id in next.removed??Array.Empty<int>())remoteCatalog.Remove(id);
                    foreach(var e in next.entities??Array.Empty<LanEntity>())
                    {
                        if(e.keepPose&&remoteCatalog.TryGetValue(e.id,out var old))new CoopPose(old).Apply(e);
                        remoteCatalog[e.id]=e;
                    }
                    remoteHeader=next;advanced=true;
                }
                if(!advanced)return null;
                packet=remoteHeader;
                if(bufferedPose!=null && bufferedPose.worldRevision==packet.worldRevision &&
                   bufferedPose.catalogRevision<=appliedCatalog)
                {
                    if(bufferedPose.seq>=packet.seq){packet=bufferedPose;ApplyPoses(packet);}
                    bufferedPose=null;
                }
            }
            else
            {
                if(packet.seq<=LatestPoseSequence)return null;
                if(remoteHeader==null||packet.worldRevision>remoteHeader.worldRevision||packet.catalogRevision>appliedCatalog)
                {
                    if(bufferedPose==null||packet.seq>bufferedPose.seq)bufferedPose=packet;
                    return null;
                }
                if(packet.worldRevision!=remoteHeader.worldRevision||packet.seq<remoteHeader.seq)return null;
                ApplyPoses(packet);
            }
            var full=remoteHeader.Copy();
            full.type="world";full.seq=packet.seq;full.sampleTime=packet.sampleTime;
            full.elapsed=packet.elapsed;full.roundTime=packet.roundTime;
            full.entities=remoteCatalog.Values.Select(e=>e.Copy()).ToArray();
            return full;
        }
    }
}
