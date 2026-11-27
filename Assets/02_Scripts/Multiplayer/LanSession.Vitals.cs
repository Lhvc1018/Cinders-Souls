using Codice.Characters;
using UnityEngine;

namespace Codice.Characters
{
    public sealed partial class CharacterState
    {
        internal struct Vitals
        {
            public float hp, armor, mana, delay;
            public int flasks;
        }
        internal float ManaDelay=>manaDelay;
        internal Vitals GetVitals()=>new Vitals{hp=health,armor=armor,mana=mana,delay=manaDelay,flasks=estusCharges};
        internal void SetVitals(Vitals value)
        {
            health=Mathf.Clamp(value.hp,0,MaxHealth);
            armor=Mathf.Clamp(value.armor,0,MaxArmor);
            mana=Mathf.Clamp(value.mana,0,MaxMana);
            manaDelay=Mathf.Max(0,value.delay);
            estusCharges=Mathf.Clamp(value.flasks,0,EstusCapacity);
        }
    }
}

namespace Codice.Multiplayer
{
    public sealed partial class LanSession
    {
        /* The host orders damage, costs, rewards and revival. Each owner keeps its resources,
           regenerates mana and drinks locally. Separate revisions protect concurrent changes. */
        sealed class VitalLedger
        {
            public Vector3Int version;
            public CharacterState.Vitals last;
            public int lastReport;
            public float seen;
            public void Reset(){version=default;lastReport=0;seen=Time.unscaledTime;}
            public void Observe(CharacterState state)
            {
                var now=state.GetVitals();
                if(version.x==0||last.hp!=now.hp||last.flasks!=now.flasks)version.x++;
                if(version.y==0||last.armor!=now.armor)version.y++;
                if(version.z==0||last.mana!=now.mana||last.delay!=now.delay)version.z++;
                last=now;
            }
        }

        Vector3Int ownedVersion;
        CharacterState.Vitals lastReportVitals;
        int ownerReport;
        float nextVitalReport;

        void ReceiveOwnedVitals(LanEntity e,bool first)
        {
            var state=context.player.State;
            var value=state.GetVitals();
            if(first||e.vitalVersion.x>ownedVersion.x){value.hp=e.health;value.flasks=e.flasks;}
            if(first||e.vitalVersion.y>ownedVersion.y)value.armor=e.armor;
            if(first||e.vitalVersion.z>ownedVersion.z){value.mana=e.mana;value.delay=e.manaDelay;}
            if(first||ownedVersion!=e.vitalVersion)
            {
                state.SetVitals(value);state.NotifyChanged();
                ownedVersion=e.vitalVersion;
            }
        }

        void UpdateOwnedVitals(PlayerCharacter player)
        {
            if(string.IsNullOrEmpty(localSaved)||floorHeld)return;
            var state=player.State;
            if(!player.ControlsBlocked&&state.IsAlive)state.TickMana(Time.deltaTime);
            if(Time.unscaledTime<nextVitalReport)return;
            var now=state.GetVitals();
            if(now.hp==lastReportVitals.hp&&now.armor==lastReportVitals.armor&&now.mana==lastReportVitals.mana
                &&now.flasks==lastReportVitals.flasks&&now.delay==lastReportVitals.delay)return;
            nextVitalReport=Time.unscaledTime+.2f;lastReportVitals=now;
            var own=new LanEntity{vitalVersion=ownedVersion,health=now.hp,armor=now.armor,mana=now.mana,manaDelay=now.delay,flasks=now.flasks};
            SendCombat(new LanMessage{type="ownerVitals",id=++ownerReport,entities=new[]{own}});
        }

        void AcceptVitals(Peer peer,LanMessage message)
        {
            if(!peer.player||message.worldRevision!=worldRevision||message.scene!=loadedScene||message.entities?.Length!=1)return;
            var data=message.entities[0];
            if(data==null||!Finite(data.health)||!Finite(data.mana)||!Finite(data.manaDelay))return;
            var ledger=peer.vitals;
            if(message.id<=ledger.lastReport)return;
            ledger.lastReport=message.id;
            var state=peer.player.State;
            ledger.Observe(state);
            var value=ledger.last;
            if(value.hp<=0)return;
            if(data.vitalVersion.x==ledger.version.x&&data.flasks<value.flasks)
            {
                int spent=value.flasks-Mathf.Clamp(data.flasks,0,CharacterState.EstusCapacity);
                if(spent>0){
                    value.hp=Mathf.Clamp(data.health,value.hp,Mathf.Min(state.MaxHealth,value.hp+spent*state.MaxHealth*CharacterState.EstusHealFraction*state.BoonFlask));
                    value.flasks-=spent;
                }
            }
            if(data.vitalVersion.z==ledger.version.z)
            {
                float allowance=CharacterState.ManaRegen*state.BoonManaRegen*Mathf.Max(.25f,Time.unscaledTime-ledger.seen+.15f);
                value.mana=Mathf.Clamp(data.mana,value.mana,Mathf.Min(state.MaxMana,value.mana+allowance));
                value.delay=Mathf.Clamp(data.manaDelay,0,value.delay);
            }
            ledger.seen=Time.unscaledTime;state.SetVitals(value);ledger.last=state.GetVitals();
        }
    }
}
