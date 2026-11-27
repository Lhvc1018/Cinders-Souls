using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Expedition;
using Codice.Mechanics;
using Codice.UI;
using UnityEngine;
using UnityEngine.AI;

public static class VerifySixStages
{
    public static async Task<string> Run()
    {
        for(int floor=1;floor<=6;floor++)
        for(int seed=0;seed<128;seed++){
            var layout=new ExpeditionLayout(seed,floor);
            if(layout.Count-1<6||layout.Count-1>9||layout.Boss<6||layout.Boss>9||!layout.Connected())
                throw new Exception($"Layout {floor}/{seed}");
        }
        MainMenuUI.UnlockCheats("jam");
        string report="";
        foreach(int floor in new[]{1,2,3,4,5,6}){
            var old=RunDirector.Instance;
            RunNavigation.StartExpeditionTest(PlayerClass.Warrior,floor);
            for(int i=0;i<600;i++){
                await Task.Delay(50);
                if(!RunNavigation.Loading && RunDirector.Instance && RunDirector.Instance!=old && RunDirector.Instance.Expedition)break;
            }
            var run=RunDirector.Instance;var f=run.Expedition;
            if(run==old||f.FloorNumber!=floor||f.CanEnterBoss())throw new Exception("Entry/gate "+floor);
            if(floor>=4){
                var gallery=f.GetComponentsInChildren<Transform>().First(t=>t.name=="Galería transitable");
                var center=gallery.position;center.y=1.6f;
                foreach(int sign in new[]{-1,1}){
                    var bottom=center+Vector3.forward*(sign*6.8f);bottom.y=0;
                    var route=new NavMeshPath();
                    if(!NavMesh.SamplePosition(center,out var top,.8f,NavMesh.AllAreas)||top.position.y<1.2f||
                       !NavMesh.SamplePosition(bottom,out var foot,1.5f,NavMesh.AllAreas)||
                       !NavMesh.CalculatePath(foot.position,top.position,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete)
                        throw new Exception("Gallery inaccessible on stage "+floor+" side "+sign);
                }
            }
            foreach(var link in f.Layout.Links){
                if(link.y==f.Layout.SecretRoom)continue;
                var path=new NavMeshPath();
                if(!NavMesh.SamplePosition(f.Center(link.x),out var a,5,NavMesh.AllAreas)||
                   !NavMesh.SamplePosition(f.Center(link.y),out var b,5,NavMesh.AllAreas)||
                   !NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                    throw new Exception($"Blocked route floor={floor} seed={f.Seed} {link}");
            }
            for(int i=0;i<f.Layout.Boss;i++)f.Cleared[i]=true;
            if(!f.CanEnterBoss())throw new Exception("Secret required "+floor);
            report+=$"{floor}: {f.RoomCount-1} pre-boss, {f.context.arena.expeditionPrefab.name}; ";
        }
        return "PASS 768 layouts and all public links on six live stages. "+report;
    }
}
