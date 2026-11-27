using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Codice.Expedition;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
public static class ValidateBranches
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Layouts()
    {
        int checks=0,merchants=0;
        for(int seed=0;seed<2000;seed++)
        {
            var a=new ExpeditionLayout(seed);var b=new ExpeditionLayout(seed);
            Check(a.Connected()&&a.Rooms.Count==9&&a.Links.Count>=8&&a.Links.Count<=10,"Graph "+seed);checks++;
            Check(a.Rooms.SequenceEqual(b.Rooms)&&a.Cells.SetEquals(b.Cells)&&a.MerchantRoom==b.MerchantRoom,"Determinism "+seed);checks++;
            Check(a.MerchantRoom!=a.StatueRoom&&a.Plans[a.StatueRoom].traps.Count==0&&a.Plans[a.StatueRoom].width==1,"Forks "+seed);checks++;
            var reached=new HashSet<int>{0};for(int i=0;i<9;i++)foreach(var e in a.Links)if(reached.Contains(e.x))reached.Add(e.y);
            Check(reached.Count==9&&!a.Links.Any(e=>e.x==8),"Boss end "+seed);checks++;
            for(int i=0;i<9;i++){Check(a.RoomAt(new Vector3(a.Rooms[i].x*4,0,a.Rooms[i].y*4))==i,"Room overlap "+seed);checks++;}
            if(a.MerchantRoom>=0)merchants++;
        }
        string report=$"{checks} checks passed; 2000 seeds; 9 rooms; 0–2 seeded forks; safe statue room; final boss reachable; merchants in {merchants}/2000 seeds.";
        System.IO.File.WriteAllText("Docs/Branches_LayoutValidation.txt",report);return report;
    }
    public static async Task<string> Stage(int target)
    {
        ExpeditionFloor f=null;for(int i=0;i<200;i++){f=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();if(f&&f.context?.player&&Codice.UI.RunHUD.Instance)break;await Task.Delay(100);}
        Check(f&&f.context?.player,"Floor not ready");Time.timeScale=1;var p=f.context.player;p.enabled=false;
        Codice.UI.RunHUD.Instance.SetPaused(false);
        for(int room=1;room<=target;room++)
        {
            if(f.Cleared[room])continue;
            Clear(f);await Task.Delay(100);
            var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=f.Center(room)+new Vector3(0,.15f,-4);cc.enabled=true;cc.Move(Vector3.down*.3f);
            Check(f.EnterRoom(room),"Enter room "+room+" seed="+f.Seed);await Task.Delay(100);
            if(room!=target){Clear(f);await Task.Delay(100);}
        }
        p.State.Initialize(PlayerClass.Warrior);Time.timeScale=0;return "Staged "+target+" seed="+f.Seed;
    }
    static void Clear(ExpeditionFloor f){foreach(var e in UnityEngine.Object.FindObjectsByType<BasicEnemy>())e.ReceiveDamage(new DamageInfo{amount=100000,source=f.context.player.gameObject,ignoreGuard=true});}
    public static string Navigation()
    {
        var f=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();int count=0;
        foreach(var link in f.Layout.Links)
        {
            var path=new NavMeshPath();Check(NavMesh.CalculatePath(f.Center(link.x),f.Center(link.y),NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Nav edge "+link+" seed="+f.Seed);count++;
        }
        string report="All "+count+" graph edges have complete NavMesh paths; seed="+f.Seed;
        System.IO.File.AppendAllText("Docs/Branches_RuntimeValidation.txt",report+"\n");return report;
    }
    public static string Economy()
    {
        var f=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();var p=f.context.player;var saved=p.State.Capture();int checks=0;
        foreach(StatueBlessing b in Enum.GetValues(typeof(StatueBlessing))){p.State.Initialize(PlayerClass.Warrior);p.State.GrantBlessing(b);Check(p.State.HasBlessing(b)&&!p.State.GrantBlessing(b),"Duplicate blessing");var copy=new CharacterState();copy.Restore(p.State.Capture());Check(copy.HasBlessing(b),"Blessing snapshot");checks+=2;}
        var merchant=UnityEngine.Object.FindAnyObjectByType<Merchant>();if(merchant){var cc=p.GetComponent<CharacterController>();var old=p.transform.position;cc.enabled=false;p.transform.position=merchant.transform.position+Vector3.back*2;cc.enabled=true;p.State.Initialize(PlayerClass.Warrior);p.State.AddGold(500);merchant.Interact(p);Check(merchant.Customer==p,"Shop open");Check(!merchant.TryPurchase(p,0),"Full health wasted gold");p.State.Damage(30,true);Check(merchant.TryPurchase(p,0)&&!merchant.TryPurchase(p,0),"Shop repeat transaction");Check(merchant.TryPurchase(p,3),"Statue purchase");merchant.CloseShop();cc.enabled=false;p.transform.position=old;cc.enabled=true;checks+=4;}
        p.State.Restore(saved);return checks+" economy checks passed.";
    }
}
