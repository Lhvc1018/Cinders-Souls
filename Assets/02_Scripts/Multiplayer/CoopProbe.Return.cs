#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Codice.Characters;
using Codice.Mechanics;
using Codice.UI;

namespace Codice.Multiplayer
{
    public sealed partial class CoopProbe
    {
        string Signal(string key)=>Path.Combine(Path.GetTempPath(),"Cinders-"+room+"-"+key);
        IEnumerator Await(string key,float seconds=14)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!File.Exists(Signal(key))&&Time.realtimeSinceStartup<deadline)yield return null;
        }
        void Mark(string key)=>File.WriteAllText(Signal(key),"1");

        IEnumerator TestReturn(LanSession session)
        {
            if(!session||!RunDirector.Instance)yield break;
            if(host)
            {
                var local=RunDirector.Instance.Player;
                if(!local.State.IsAlive)local.ReviveFromAlly(1);
                var guest=session.Players().FirstOrDefault(p=>p!=local);
                if(!guest)yield break;
                var source=new GameObject("Probe melee attacker");
                source.transform.position=guest.transform.position+guest.transform.forward;
                foreach(string defense in new[]{"parry","roll","reflect"}){
                    float impact=Time.unscaledTime+1.6f;
                    File.WriteAllText(Signal(defense+"Ready"),impact.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                    while(Time.unscaledTime<impact)yield return null;
                    float before=guest.State.Health+guest.State.Armor;
                    Codice.Enemies.EnemyProjectile bolt=null;Codice.Enemies.BasicEnemy shooter=null;
                    if(defense=="reflect"){
                        var dir=guest.transform.forward;
                        shooter=Instantiate(Resources.Load<Codice.Enemies.BasicEnemy>("ExpeditionEnemies/Hellhound"),
                            guest.transform.position+dir*3,Quaternion.LookRotation(-dir));
                        shooter.enabled=false;shooter.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
                        var go=new GameObject("Probe hostile projectile");go.transform.position=guest.transform.position+Vector3.up+dir*.9f;
                        bolt=go.AddComponent<Codice.Enemies.EnemyProjectile>();bolt.Launch(shooter,guest,-dir,15,4);
                    }
                    else guest.ReceiveDamage(new DamageInfo{amount=4,source=source,direction=-guest.transform.forward,melee=true});
                    yield return new WaitForSecondsRealtime(.9f);
                    Write("HOST "+defense+" prevented damage="+(before==guest.State.Health+guest.State.Armor)+" result="+session.LastTestDefense);
                    if(shooter){
                        ProbeResult("projectile returned to attacker",shooter.Health<shooter.MaxHealth);
                        if(bolt)Destroy(bolt.gameObject);
                        Destroy(shooter.gameObject);
                    }
                    Mark(defense+"Done");
                }
                Destroy(source);
                yield return Await("returned",45);
                var returned=session.Players().FirstOrDefault(p=>p!=local);
                Write("HOST reconnect restored="+(returned&&returned!=guest&&!guest)+" class="+(returned?returned.State.Class.ToString():"missing"));
                yield return TestBossReturnAndPortal(session);
                yield break;
            }
            var player=RunDirector.Instance.Player;
            if(pad==null)pad=InputSystem.AddDevice<Gamepad>();
            foreach(string defense in new[]{"parry","roll","reflect"}){
                yield return Await(defense+"Ready");
                if(!float.TryParse(File.ReadAllText(Signal(defense+"Ready")),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var impact))yield break;
                float untilInput=Time.realtimeSinceStartup+3;
                while(session.DisplayedTime<impact-.12f&&Time.realtimeSinceStartup<untilInput)yield return null;
                Cursor.lockState=CursorLockMode.Locked;
                var key=defense=="roll"?GamepadButton.East:GamepadButton.LeftShoulder;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(key));
                yield return null;
                yield return new WaitForSecondsRealtime(.09f);
                Write("CLIENT "+defense+" local="+player.OwnedDefense()+" displayed="+session.DisplayedTime+" contact="+impact);
                yield return new WaitForSecondsRealtime(.33f);
                InputSystem.QueueStateEvent(pad,new GamepadState());
                yield return Await(defense+"Done");
            }
            int id=Field<int>(session,"localId"),seed=session.DungeonSeed;
            var beforeState=player.State;
            int charges=beforeState.EstusCharges,slot=beforeState.ActiveSlot;
            var cls=beforeState.Class;float hp=beforeState.Health;
            var weapon=beforeState.ActiveWeapon.kind;
            RunNavigation.MainMenu();
            yield return new WaitForSecondsRealtime(3);
            LanSession.Open();session=LanSession.Instance;session.useFusion=true;
            typeof(LanSession).GetField("nickname",F).SetValue(session,"Invitado");
            typeof(LanSession).GetField("address",F).SetValue(session,room);session.Join();
            float until=Time.realtimeSinceStartup+30;
            while(Time.realtimeSinceStartup<until&&(!session||string.IsNullOrEmpty(Field<string>(session,"localSaved"))))yield return null;
            var current=RunDirector.Instance?RunDirector.Instance.Player:null;
            bool ok=session&&LanSession.Client&&current&&Field<int>(session,"localId")==id&&session.DungeonSeed==seed
                &&current.State.Class==cls&&current.State.ActiveSlot==slot&&current.State.ActiveWeapon?.kind==weapon
                &&current.State.EstusCharges==charges&&Mathf.Abs(current.State.Health-hp)<.01f;
            Write("CLIENT reconnect slot/class/weapon/flasks/HP/seed="+ok);Mark("returned");
            if(ok)yield return TestBossReturnAndPortal(session);
        }
    }
}
#endif
