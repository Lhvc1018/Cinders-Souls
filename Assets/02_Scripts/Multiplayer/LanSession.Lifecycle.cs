using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Characters;
using Codice.Mechanics;
using Codice.UI;
using UnityEngine;

namespace Codice.Multiplayer
{
    public sealed partial class LanSession
    {
        static readonly string AppSessionId = Guid.NewGuid().ToString("N");
        const float FloorWaitLimit = 25f;
        readonly List<Peer> disconnected = new List<Peer>();
        bool floorHeld, welcomed, waitingAtBoss;
        int readySent = -1, released = -1;
        float readyDeadline, nextPingSample;
        LanMessage announcedFloor;
        public static bool WaitingForFloor => Instance && Instance.floorHeld;
        bool FightingBoss => context && context.arena && context.arena.ActiveBoss && RunDirector.Instance && RunDirector.Instance.CombatActive;

        static void SendReliable(Peer peer,LanMessage message)
        {
            peer.wire.Send(JsonUtility.ToJson(message));
        }
        static string CleanName(string name) => new string((name??"").Where(c=>char.IsLetterOrDigit(c)||c==' ').Take(20).ToArray()).Trim();
        void KeepSeat(Peer peer)
        {
            if(!started||!peer.accepted)return;
            CaptureSeat(peer);
            disconnected.RemoveAll(p=>p.id==peer.id);
            disconnected.Add(peer);
        }
        bool Admit(Peer peer,LanMessage msg)
        {
            if(!Guid.TryParseExact(msg.state,"N",out _)||peers.Any(p=>p!=peer&&p.accepted&&p.ticket==msg.state))return false;
            peer.ticket=msg.state;
            peer.name=CleanName(msg.name);
            if(peer.name.Length==0)peer.name="Jugador";
            peer.playerClass=msg.playerClass;
            if(started)
            {
                var seat=disconnected.FirstOrDefault(p=>p.ticket==peer.ticket);
                if(seat==null)
                {
                    var byName=disconnected.Where(p=>string.Equals(p.name,peer.name,StringComparison.OrdinalIgnoreCase)).ToArray();
                    if(byName.Length!=1||peers.Any(p=>p!=peer&&p.accepted&&string.Equals(p.name,peer.name,StringComparison.OrdinalIgnoreCase)))return false;
                    seat=byName[0];
                }
                peer.id=seat.id;
                peer.name=seat.name;
                peer.playerClass=seat.playerClass;
                peer.saved=seat.saved;
                peer.savedSpells=seat.savedSpells;
                peer.savedSpellChoice=seat.savedSpellChoice;
                peer.spellTimes=seat.spellTimes;
                peer.returning=true;
                peer.waitingForBoss=FightingBoss;
                disconnected.Remove(seat);
            }
            peer.memories=msg.memories&7;
            peer.accepted=true;
            RefreshMembers();
            SendReliable(peer,new LanMessage{type="welcome",id=peer.id,playerClass=peer.playerClass,members=members,encounter=peer.waitingForBoss});
            if(started&&!peer.waitingForBoss&&context&&!RunNavigation.Loading)
                RestoreSeat(peer);
            return true;
        }
        void RestoreSeat(Peer peer)
        {
            peer.waitingForBoss=false;
            SpawnPeer(peer);
            AnnounceFloor(peer);
        }
        void AnnounceFloor(Peer peer)
        {
            SendReliable(peer,new LanMessage{type="floorGate",scene=loadedScene,seed=DungeonSeed,floor=FloorNumber,worldRevision=worldRevision,encounter=floorHeld});
        }
        void BeginFloorGate()
        {
            floorHeld=peers.Any(p=>p.accepted);
            readyDeadline=Time.realtimeSinceStartup+FloorWaitLimit;
            foreach(var peer in peers.Where(p=>p.accepted))
            {
                peer.readyRevision=-1;
                peer.waitingForBoss=false;
                AnnounceFloor(peer);
            }
        }
        void ReleaseFloorGate()
        {
            floorHeld=false;
            foreach(var p in peers.Where(p=>p.accepted))
                SendReliable(p,new LanMessage{type="floorGo",worldRevision=worldRevision});
        }
        void HostLifecycle()
        {
            if(started&&context&&!RunNavigation.Loading)
            {
                foreach(var p in peers)
                    if(p.accepted && p.returning && !p.player && !FightingBoss)RestoreSeat(p);
                if(floorHeld&&(Time.realtimeSinceStartup>=readyDeadline||peers.All(p=>!p.accepted||p.readyRevision==worldRevision)))
                    ReleaseFloorGate();
            }
            if(Time.realtimeSinceStartup<nextPingSample)return;
            nextPingSample=Time.realtimeSinceStartup+.5f;
            foreach(var p in peers)
            {
                if(!p.accepted)continue;
                float ms=fusion?fusion.GetWireRttMs(p.wire):-1;
                var member=Array.Find(members,m=>m.id==p.id);
                if(member!=null)member.pingMs=ms<0?-1:Mathf.RoundToInt(ms);
            }
            foreach(var p in peers)
                if(p.accepted)SendReliable(p,new LanMessage{type="peerPing",members=members});
        }
        bool ClientLifecycleMessage(LanMessage msg)
        {
            switch(msg.type)
            {
                case "welcome":
                    localId=msg.id;chosenClass=msg.playerClass;members=msg.members;
                    welcomed=true;waitingAtBoss=msg.encounter;
                    if(waitingAtBoss)status="Plaza recuperada. Esperando al final del combate de jefe.";
                    return true;
                case "peerPing":
                    if(msg.members!=null)members=msg.members;
                    return true;
                case "floorGate":
                    if(msg.worldRevision<worldRevision || announcedFloor!=null&&msg.worldRevision<announcedFloor.worldRevision)return true;
                    waitingAtBoss=false;
                    announcedFloor=msg;
                    floorHeld=msg.encounter&&released<msg.worldRevision;
                    return true;
                case "floorGo":
                    released=Math.Max(released,msg.worldRevision);
                    if(worldRevision<=released&&(announcedFloor==null||announcedFloor.worldRevision<=released))floorHeld=false;
                    return true;
                default:return false;
            }
        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal int OmitReadyInTest=-1;
#endif
        void ClientLifecycle()
        {
            var floor=announcedFloor;
            if(floor==null||RunNavigation.Loading||!welcomed||waitingAtBoss)return;
            if(!started||worldRevision!=floor.worldRevision||FloorNumber!=floor.floor)
            {
                if(!Application.CanStreamedLevelBeLoaded(floor.scene)){Disconnect("Falta el escenario del anfitrión.");return;}
                context=null;ghosts.Clear();started=true;DungeonSeed=floor.seed;FloorNumber=floor.floor;worldRevision=floor.worldRevision;
                RunNavigation.StartCoop((PlayerClass)chosenClass,floor.scene);
            }
            else if(context && readySent!=worldRevision)
            {
                readySent=worldRevision;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if(OmitReadyInTest==worldRevision)return;
#endif
                Send(new LanMessage{type="ready",worldRevision=worldRevision,floor=FloorNumber,scene=loadedScene});
            }
        }
        public static string ColoredPing(float ms)
        {
            if(ms<0)return "…";
            string color=ms<80?"#82D59A":ms<150?"#F2C66D":"#F07C71";
            return "<color="+color+">"+Mathf.RoundToInt(ms)+" ms</color>";
        }
        string GuestPingText()
        {
            var samples=members.Where(m=>m.id!=0&&m.pingMs>=0).Select(m=>ColoredPing(m.pingMs));
            string result=string.Join(" · ",samples);
            return result.Length>0?result:null;
        }
        float OwnerPing()
        {
            if(!client)return -1;
            var me=Array.Find(members,m=>m.id==localId);
            if(me!=null&&me.pingMs>=0)return me.pingMs;
            return Time.realtimeSinceStartup-pingReceived<5?pingMs:-1;
        }
    }
}
