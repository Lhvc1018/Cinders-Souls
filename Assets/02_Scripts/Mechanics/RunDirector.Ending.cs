using UnityEngine;
namespace Codice.Mechanics
{
    public sealed partial class RunDirector
    {
        public int DestinyOrbs {get;private set;}
        public bool LastBellSeal {get;private set;}
        public string EndingTitle {get;private set;}="";
        public string EndingText {get;private set;}="";
        public bool TrueEndingReady=>DestinyOrbs==7&&LastBellSeal;
        public void CollectDestinyOrb(){int orb=FloorNumber==3?0:FloorNumber==6?1:FloorNumber==9?2:-1;if(orb>=0)DestinyOrbs|=1<<orb;}
        public void RecoverBellSeal(){if(FloorNumber==12)LastBellSeal=true;}
        public string EndingRequirements=>
            ((DestinyOrbs&1)!=0?"[+]":"[-]")+" Memoria · 1-3\n"+
            ((DestinyOrbs&2)!=0?"[+]":"[-]")+" Voluntad · 2-3\n"+
            ((DestinyOrbs&4)!=0?"[+]":"[-]")+" Duelo · 3-3\n"+
            (LastBellSeal?"[+]":"[-]")+" Sello de la última campana · 4-3";
        public bool FinishDescent(bool releaseNames)
        {
            if(FloorNumber!=FinalExpeditionFloor||!Expedition||!Expedition.Finished||Phase!=RunPhase.Cleared||!Player||!Player.State.IsAlive||Codice.Multiplayer.LanSession.Client||releaseNames&&!TrueEndingReady)return false;
            EndingTitle=releaseNames?"EL ALBA SIN DEUDA":"EL ÚLTIMO VIGÍA";
            EndingText=releaseNames?"Rompes el sello. Los nombres vuelven a sus muertos.\nPor primera vez, el alba no exige otra vida.":"Abandonas el altar y custodias su entrada.\nLa Horda ha caído; bajo Vespera, la campana aún espera.";
            PlayerPrefs.SetInt(releaseNames?"Cinders.Ending.Dawn":"Cinders.Ending.Vigil",1);PlayerPrefs.Save();
            Debug.Log("[Ending] "+EndingTitle+" orbs="+DestinyOrbs+" bellSeal="+LastBellSeal);if(!Codice.UI.MainMenuUI.CheatsUnlocked)SaveSlots.EndRun(true);SetExpeditionPhase(RunPhase.Victory,EndingTitle);return true;
        }
        internal void NetworkEnding(int orbs,bool seal,string title,string text){DestinyOrbs=orbs;LastBellSeal=seal;EndingTitle=title;EndingText=text;}
        void ResetEnding(){DestinyOrbs=0;LastBellSeal=false;EndingTitle="";EndingText="";}
    }
}
