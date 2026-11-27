using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.UI;
using Codice.Presentation;
public static class RuntimeReview
{
    static void Require(bool value,string text){if(!value)throw new Exception(text);}
    public static async Task<string> Combat()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();var floor=UnityEngine.Object.FindFirstObjectByType<Codice.Expedition.ExpeditionFloor>();
        Require(p&&floor,"Descenso must be running");RunHUD.Instance.SetPaused(false);
        var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=floor.Center(1)+new Vector3(0,.15f,-7);cc.enabled=true;
        floor.EnterRoom(1);await Task.Delay(300);
        var dogs=UnityEngine.Object.FindObjectsByType<BasicEnemy>();Require(dogs.Length>0,"Room has enemies");
        var dog=dogs[0];var presentation=dog.GetComponent<ActorPresentation>();Require(dog.lungingHound&&dog.animator,"Hound prefab configured");
        var states=new HashSet<string>();float moved=0;Vector3 old=dog.transform.position;Quaternion initial=dog.animator.GetComponentsInChildren<Transform>().First(t=>t.name=="front_L").localRotation;float bend=0;
        for(int i=0;i<45;i++)
        {
            await Task.Delay(100);p.State.Heal(1000);if(!dog)break;
            states.Add(presentation.CurrentState);moved+=Vector3.Distance(old,dog.transform.position);old=dog.transform.position;
            bend=Mathf.Max(bend,Quaternion.Angle(initial,dog.animator.GetComponentsInChildren<Transform>().First(t=>t.name=="front_L").localRotation));
        }
        Require(moved>.5f&&bend>8,"Hound must move and deform");Require(states.Contains("Chase")&&states.Contains("Melee"),"Run and bite states required");
        dog.ApplyStun(1);await Task.Delay(180);Require(presentation.CurrentState=="Stun","Stun animation");
        var relics=p.GetComponent<RunRelics>();p.State.TickPassives(100);Require(relics.Activate(1),"Frost bell activates");Require(!relics.Activate(1),"Cooldown denies second cast");
        RunHUD.Instance.SetPaused(true);float wait=p.State.RelicWait(1);await Task.Delay(220);Require(Mathf.Abs(wait-p.State.RelicWait(1))<.001f,"Cooldown freezes during pause");Require(!relics.Activate(0),"Paused cast rejected");RunHUD.Instance.SetPaused(false);
        foreach(var e in UnityEngine.Object.FindObjectsByType<BasicEnemy>())e.ReceiveDamage(new DamageInfo{amount=10000,source=p.gameObject,direction=Vector3.forward,ignoreGuard=true});
        await Task.Delay(250);Require(UnityEngine.Object.FindObjectsByType<Animator>().Any(a=>a.name=="Animated remains"),"Death corpse survives actor destruction");
        Require(UnityEngine.Object.FindObjectsByType<RelicAltar>().Length==1,"Cleared room creates one altar");
        var report=$"[Runtime validation] Hound moved {moved:0.00} m; front-leg motion {bend:0.0} degrees; states {string.Join(",",states)}. Stun, death persistence, frost, cooldown, pause and room reward passed.";
        Debug.Log(report);System.IO.File.WriteAllText("Docs/RuntimeValidation.txt",report);return report;
    }
    public static string OpenAltar()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();var altar=UnityEngine.Object.FindFirstObjectByType<RelicAltar>();
        Require(altar,"Reward altar missing");var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=altar.transform.position+Vector3.back*2;cc.enabled=true;altar.Interact(p);
        Require(RunHUD.Instance.RelicSelectionVisible,"Altar opens");return "Relic menu open";
    }
    public static string Choose()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();var altar=UnityEngine.Object.FindFirstObjectByType<RelicAltar>();
        Require(altar&&RunHUD.Instance.RelicSelectionVisible,"Selection open");
        var panel=RunHUD.Instance.transform.Find("Relic choice");panel.Find("Relic offer 0/ELEGIR").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        panel.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name==""&&b.gameObject.activeInHierarchy).onClick.Invoke();
        Require(p.State.PassiveCount==1,"Mutation granted");Require(!RunHUD.Instance.RelicSelectionVisible&&Time.timeScale==1,"Choice resumes gameplay");
        Require(!altar||altar.Claimed,"Altar spent");Debug.Log("[Runtime validation] Reward selection via buttons passed; passive equipped, altar spent, gameplay resumed.");return "Choice completed";
    }
}
