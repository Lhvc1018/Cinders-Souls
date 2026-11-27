#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Codice.Characters;
using Codice.Expedition;
using Codice.Mechanics;
using Codice.UI;
using UnityEngine;

namespace Codice.Multiplayer
{
    public sealed partial class CoopProbe
    {
        IEnumerator WaitForProbe(Func<bool> predicate,float seconds)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!predicate()&&Time.realtimeSinceStartup<deadline)yield return null;
        }
        void ProbeResult(string title,bool success)
        {
            Write((host?"HOST ":"CLIENT ")+title+"="+success);
            if(!success)error=error??title;
        }
        static string FloorSignature(ExpeditionFloor floor)
        {
            var layout=floor.Layout;
            string cells=string.Join(",",layout.Cells.OrderBy(v=>v.x).ThenBy(v=>v.y));
            return floor.Seed+"/"+layout.Boss+"/"+string.Join(",",layout.Rooms)+"/"+string.Join(",",layout.Links)+"/"+cells;
        }
        static void WarpForTest(PlayerCharacter actor,Vector3 position)
        {
            var cc=actor.GetComponent<CharacterController>();
            cc.enabled=false;actor.transform.position=position;cc.enabled=true;
            Physics.SyncTransforms();
        }
        IEnumerator TestBossReturnAndPortal(LanSession session)
        {
            if(!session||!RunDirector.Instance)yield break;
            int startFloor=session.FloorNumber;
            if(host)
            {
                yield return Await("bossTestGuest",15);
                var run=RunDirector.Instance;
                var floor=run.Expedition;
                var player=run.Player;
                player.State.LaboratoryImmortal=true;
                if(!player.State.IsAlive)player.ReviveFromAlly(1);
                for(int pass=0;floor.InEncounter&&pass<16;pass++)
                {
                    foreach(var enemy in FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))
                        if(enemy.IsAlive)enemy.ReceiveDamage(new DamageInfo{amount=1000000,ignoreGuard=true,source=player.gameObject});
                    yield return new WaitForSecondsRealtime(.1f);
                }
                yield return WaitForProbe(()=>!floor.InEncounter,5);
                int roomIndex=floor.Layout.Boss;
                foreach(int adjacent in floor.Layout.Neighbors(roomIndex))floor.Cleared[adjacent]=true;
                WarpForTest(player,floor.Center(roomIndex)+Vector3.up*.2f);
                ProbeResult("group gate waits for guest",!floor.EnterRoom(roomIndex,player)&&!floor.InEncounter);
                foreach(var mate in session.Players()) if(mate!=player) WarpForTest(mate,floor.Center(roomIndex)+Vector3.right);
                if(!floor.InEncounter)floor.EnterRoom(roomIndex,player);
                var scene=floor.GetComponent<RunScene>();
                var boss=scene.arena.ActiveBoss;
                ProbeResult("real boss encounter started",boss&&boss.IsAlive&&floor.InEncounter);
                if(!boss){Mark("abortedPortal");yield break;}
                boss.SetPaused(true);
                Mark("bossStarted");
                yield return Await("observedWaiting",40);
                ProbeResult("return during boss held before spawning",session.Players().Count()==1&&File.Exists(Signal("observedWaiting")));
                boss.SetPaused(false);
                boss.ReceiveDamage(new DamageInfo{amount=boss.MaxHealth+1,ignoreGuard=true,source=player.gameObject});
                yield return Await("backAfterBoss",24);
                ProbeResult("returned guest spawned after boss death",session.Players().Count()==2&&scene.arena.Completed);
                var portal=FindFirstObjectByType<DescentPortal>();
                ProbeResult("real floor exit opened",portal&&floor.Finished);
                if(!portal){Mark("abortedPortal");yield break;}
                yield return new WaitForSecondsRealtime(1.7f);
                WarpForTest(player,portal.transform.position+Vector3.up*.1f);
                yield return WaitForProbe(()=>session.FloorNumber==startFloor+1&&LanSession.WaitingForFloor,25);
                bool gated=LanSession.WaitingForFloor&&session.FloorNumber==startFloor+1;
                ProbeResult("blocked controls on next floor",gated&&RunDirector.Instance.Player.ControlsBlocked);
                float gateStart=Field<float>(session,"readyDeadline")-25f;
                yield return WaitForProbe(()=>!LanSession.WaitingForFloor,28);
                float delay=Time.realtimeSinceStartup-gateStart;
                ProbeResult("real portal/25s deadline",gated&&!LanSession.WaitingForFloor&&delay>=24.8f&&delay<27);
                Write("HOST floor wait measured seconds="+delay.ToString("F2"));
                File.WriteAllText(Signal("floorHash"),FloorSignature(RunDirector.Instance.Expedition));
                Mark("releasedByHost");
                yield return Await("portalSeen",15);
            }
            else
            {
                var id=Field<int>(session,"localId");
                var before=RunDirector.Instance.Player.State;
                var cls=before.Class;
                var weapon=before.ActiveWeapon.kind;
                int charges=before.EstusCharges;
                Mark("bossTestGuest");
                yield return WaitForProbe(()=>File.Exists(Signal("bossStarted"))||File.Exists(Signal("abortedPortal")),20);
                if(!File.Exists(Signal("bossStarted"))){ProbeResult("real boss prepared",false);yield break;}
                RunNavigation.MainMenu();
                yield return new WaitForSecondsRealtime(3);
                LanSession.Open();
                session=LanSession.Instance;session.useFusion=true;
                typeof(LanSession).GetField("nickname",F).SetValue(session,"Invitado");
                typeof(LanSession).GetField("address",F).SetValue(session,room);
                session.Join();
                yield return WaitForProbe(()=>Field<bool>(session,"waitingAtBoss"),22);
                bool waiting=Field<bool>(session,"waitingAtBoss");
                ProbeResult("waiting for boss in lobby",waiting&&!Field<bool>(session,"started")&&Field<int>(session,"localId")==id);
                if(waiting)Mark("observedWaiting");
                yield return WaitForProbe(()=>!string.IsNullOrEmpty(Field<string>(session,"localSaved")),48);
                var actor=RunDirector.Instance?RunDirector.Instance.Player:null;
                bool restored=actor&&actor.State.Class==cls&&actor.State.ActiveWeapon?.kind==weapon&&actor.State.EstusCharges==charges;
                ProbeResult("boss reconnect restored class/weapon/flasks/seat",waiting&&restored&&Field<int>(session,"localId")==id);
                /* Suppress exactly one ready message to exercise the real 25 s deadline. */
                session.OmitReadyInTest=Field<int>(session,"worldRevision")+1;
                Mark("backAfterBoss");
                yield return WaitForProbe(()=>session.FloorNumber==startFloor+1&&LanSession.WaitingForFloor,35);
                bool gated=LanSession.WaitingForFloor;
                yield return Await("releasedByHost",36);
                yield return WaitForProbe(()=>!LanSession.WaitingForFloor,5);
                var f=RunDirector.Instance?RunDirector.Instance.Expedition:null;
                bool same=f&&File.Exists(Signal("floorHash"))&&File.ReadAllText(Signal("floorHash"))==FloorSignature(f);
                ProbeResult("portal transported, identical map and timeout released",gated&&same&&!LanSession.WaitingForFloor&&session.FloorNumber==startFloor+1);
                Mark("portalSeen");
            }
        }
    }
}
#endif
