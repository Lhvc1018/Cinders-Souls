using System;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Mechanics;
using Codice.UI;
using UnityEngine;

public static class VerifyJamCheats
{
    public static async Task<string> Run()
    {
        MainMenuUI.UnlockCheats("jam");
        SessionCheats.Execute("Good!");SessionCheats.Execute("Good!");
        if(!SessionCheats.God)throw new Exception("Duplicate submission disabled God");
        int previousSeed=0;
        foreach(int destination in new[]{1,2,3,4,4})
        {
            var old=RunDirector.Instance;
            RunNavigation.StartExpeditionTest(PlayerClass.Warrior,destination);
            bool arrived=false;
            for(int i=0;i<400;i++){
                await Task.Delay(50);
                var run=RunDirector.Instance;
                if(!RunNavigation.Loading&&run&&run!=old&&run.Expedition&&run.Player){arrived=true;break;}
            }
            if(!arrived)throw new Exception("Travel timeout "+destination);
            var current=RunDirector.Instance;
            if(current.FloorNumber!=destination||current.Expedition.FloorNumber!=destination)throw new Exception("Wrong procedural destination");
            if(previousSeed!=0&&previousSeed==current.Expedition.Seed)throw new Exception("Repeated seed");
            previousSeed=current.Expedition.Seed;
            var state=current.Player.State;
            float health=state.Health;
            state.Damage(100000,true);
            if(state.Health!=health||!state.IsAlive||!SessionCheats.God)throw new Exception("God lost across travel");
            if(!state.SpendMana(100000)||!state.ConsumeRelic(0)||state.Mana!=state.MaxMana)throw new Exception("Infinite mana/relic failed");
        }
        SessionCheats.Execute("Good! off");if(SessionCheats.God)throw new Exception("Cannot disable God");
        SessionCheats.Execute("Good!");
        return "PASS jam destinations 1-1, 1-2, 1-3, 2-1 and regeneration; God duplicate submission, immortality and spell/relic mana across five scene loads";
    }
}
