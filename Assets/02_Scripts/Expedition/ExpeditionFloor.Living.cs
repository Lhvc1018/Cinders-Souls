using UnityEngine;
using Codice.Mechanics;
namespace Codice.Expedition
{
    public sealed partial class ExpeditionFloor
    {
        public bool IsMemoryEncounter(int room)=>Layout!=null&&Layout.SideRooms.Contains(room)&&((Seed^room)&1)==0;
        void BeginLivingDescent()
        {
            if(Codice.Multiplayer.LanSession.Client)return;
            if(director.HiredPilgrim)context.player.State.HiredPilgrim=true;if(director.BondedWisp)context.player.State.BondedWisp=true;// legacy run flags
            EnsureCompanions();
            // Every run can meet a recruit, even before the first permanent unlock.
            int refuge=Layout.MerchantRoom>=0?Layout.MerchantRoom:Layout.StatueRoom;
            DescentEncounter.Create(Center(refuge)+new Vector3(3,.1f,-3),transform,(Seed&1)==0?DescentOffer.Pilgrim:DescentOffer.Wisp);
            foreach(int room in Layout.SideRooms)if(IsMemoryEncounter(room))
                DescentEncounter.Create(Center(room)+new Vector3(0,.1f,2),transform,(DescentOffer)((uint)(Seed^room*7919)%3));
            EnsureLedger();
        }
        bool ledgerBuilt;float nextCompanionCheck;
        /// The ledger shows each machine's own permanent keys, so guests build theirs locally.
        void EnsureLedger()
        {
            if(ledgerBuilt)return;ledgerBuilt=true;
            var ledger=new GameObject("Memoria del descenso");ledger.transform.SetParent(transform);ledger.transform.position=Center(0)+new Vector3(-3,.8f,-2);
            ledger.AddComponent<DescentLedger>();var col=ledger.AddComponent<SphereCollider>();col.radius=.65f;col.isTrigger=true;
            var model=Resources.Load<GameObject>("CindersDungeon/RelicToken");if(model)PickupScale.Fit(Instantiate(model,ledger.transform,false),.5f);
        }
        /// Host/solo: every player keeps the companions their own contracts paid for (guests join after Begin).
        void EnsureCompanions()
        {
            if(Codice.Multiplayer.LanSession.Client||!context||!context.player)return;
            var team=Codice.Multiplayer.LanSession.Host?Codice.Multiplayer.LanSession.Instance.Players():new[]{context.player};
            foreach(var p in team){if(!p)continue;if(p.State.HiredPilgrim)DescentCompanion.Create(p,false);if(p.State.BondedWisp)DescentCompanion.Create(p,true);}
        }
        void TickCompanions(){if(Time.time<nextCompanionCheck)return;nextCompanionCheck=Time.time+1;EnsureCompanions();}
    }
    public sealed class DescentLedger:MonoBehaviour,Codice.Characters.IPlayerInteractable
    {
        public string Description {
            get {var text="MEMORIA DEL DESCENSO\n";for(int i=0;i<3;i++)text+=(DescentMemory.Owns(i)?"[+] ":"[-] ")+DescentMemory.Names[i]+"\n";return text+"[E] Consultar · Las llaves sobreviven a la muerte";}
        }
        public void Interact(Codice.Characters.PlayerCharacter player){if(player&&Vector3.Distance(player.transform.position,transform.position)<3.5f)Codice.UI.RunHUD.Instance?.Announce("Las llaves de los guardianes 1-1, 2-1 y 3-1 abren otras cámaras en futuros descensos.");}
    }
}
