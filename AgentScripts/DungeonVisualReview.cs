using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Codice.Expedition;
using Codice.Characters;
using Codice.Enemies;
using Codice.UI;
public static class DungeonVisualReview
{
    public static async Task<string> Stage(int room)
    {
        Time.timeScale=1;ExpeditionFloor floor=null;
        for(int attempt=0;attempt<150;attempt++){floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();if(floor&&floor.context?.player&&RunHUD.Instance)break;await Task.Delay(100);}
        if(!floor||floor.context?.player==null||!RunHUD.Instance)throw new Exception("Dungeon startup did not finish within 15 seconds");
        var p=floor.context.player;p.enabled=false;RunHUD.Instance.SetPaused(false);
        if(floor.InEncounter)
        {
            foreach(var e in UnityEngine.Object.FindObjectsByType<BasicEnemy>())e.ReceiveDamage(new DamageInfo{amount=10000,source=p.gameObject,ignoreGuard=true});
            await Task.Delay(100);
        }
        for(int i=floor.CurrentRoom+1;i<=room;i++)
        {
            var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=floor.Center(i)+new Vector3(0,.15f,-5);cc.enabled=true;cc.Move(Vector3.down*.3f);
            floor.EnterRoom(i);await Task.Delay(100);
            if(i<room)foreach(var e in UnityEngine.Object.FindObjectsByType<BasicEnemy>())e.ReceiveDamage(new DamageInfo{amount=10000,source=p.gameObject,ignoreGuard=true});
            await Task.Delay(100);
        }
        var controller=p.GetComponent<CharacterController>();controller.enabled=false;p.transform.position=floor.Center(room)+new Vector3(0,.15f,-7);controller.enabled=true;controller.Move(Vector3.down*.3f);
        p.transform.rotation=Quaternion.identity;p.view.transform.rotation=Quaternion.Euler(8,0,0);p.State.Heal(1000);
        await Task.Delay(150);Time.timeScale=0;return "Room "+room+" shape "+floor.Layout.Plans[room].shape+" staged for actual gameplay capture";
    }
    public static async Task<string> Backtrack()
    {
        var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();var p=floor.context.player;
        if(floor.CurrentRoom<1||floor.InEncounter)throw new Exception("Clear the first encounter");
        int progress=floor.CurrentRoom;var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=floor.Center(0)+Vector3.up*.15f;cc.enabled=true;
        await Task.Delay(100);if(floor.PlayerRoom!=0||floor.CurrentRoom!=progress||!floor.Cleared[0])throw new Exception("Backtracking tracking failed");
        return "Minimap tracks returning to room 1 without resetting progression or spawning another encounter.";
    }
}
