using System;
using System.Reflection;
using System.Threading.Tasks;
using Codice.Mechanics;
using Codice.Expedition;
using Codice.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyWorldProgression
{
    public static async Task<string> Run()
    {
        int horror=0,mutant=0;
        for(int i=0;i<100;i++){
            string resource=ExpeditionFloor.BossResource(i,1);
            if(!Resources.Load<BossCharacter>(resource))throw new Exception("Missing boss "+resource);
            if(resource.EndsWith("MonsterMutant"))mutant++;else horror++;
        }
        if(mutant==0||horror==0)throw new Exception("Boss selection lacks variety");
        var run=RunDirector.Instance;if(!run)throw new Exception("Run not ready");
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        typeof(RunDirector).GetField("<FloorNumber>k__BackingField",flags).SetValue(run,3);
        var operation=SceneManager.LoadSceneAsync("Descenso");
        while(!operation.isDone)await Task.Delay(50);
        await Task.Delay(900);
        var floor=run.Expedition;
        if(floor.context.arena.expeditionPrefab.kind!=BossKind.AngelKnight)throw new Exception("Angel not in 1-3");
        typeof(ExpeditionFloor).GetField("<CurrentRoom>k__BackingField",flags).SetValue(floor,floor.Layout.Boss);
        typeof(ExpeditionFloor).GetField("<InEncounter>k__BackingField",flags).SetValue(floor,true);
        typeof(ExpeditionFloor).GetMethod("CompleteRoom",flags).Invoke(floor,null);
        await Task.Delay(150);
        var encounter=floor.GetComponentInChildren<DestinyEncounter>();
        if(!encounter||run.TravelNextFloor())throw new Exception("Lore gate bypassed");
        encounter.Advance();
        if(!run.DestinyOrbCollected||run.TravelNextFloor())throw new Exception("Orb or dialogue gate invalid");
        for(int i=0;i<4;i++)encounter.Advance();
        for(int i=0;i<120&&(!run.Expedition||run.Expedition==floor);i++)await Task.Delay(50);
        if(run.FloorNumber!=4||!run.WatcherEncounterComplete||run.Expedition==floor)throw new Exception("World 2 transition failed");
        return "PASS 100 boss seeds; Horror="+horror+", Mutant="+mutant+"; Angel 1-3; orb and dialogue gate; actual scene transition to 2-1. Boss death callback simulated.";
    }
}
