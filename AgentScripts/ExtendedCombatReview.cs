using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using Codice.Mechanics;
using Codice.UI;
using Codice.Presentation;
public static class ExtendedCombatReview
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static async Task<string> Powers()
    {
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();var state=player.State;var original=state.Capture();
        var relics=player.GetComponent<RunRelics>();RunHUD.Instance.SetPaused(false);
        var start=player.transform.position;var rotation=player.view.transform.rotation;
        var enemy=UnityEngine.Object.Instantiate(Resources.Load<BasicEnemy>("ExpeditionEnemies/Hellhound"),start+Vector3.forward*3,Quaternion.identity);
        enemy.enabled=false;enemy.healthMultiplier=20;enemy.Initialize(player,1);enemy.GetComponent<NavMeshAgent>().enabled=false;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary line of sight check";wall.transform.position=start+new Vector3(0,1,1.5f);wall.transform.localScale=new Vector3(3,3,.2f);
        try
        {
            state.TickPassives(100);Physics.SyncTransforms();float hp=enemy.Health;
            Check(relics.Activate(0),"Censer starts");Check(enemy.Health==hp,"Censer respects wall");
            wall.SetActive(false);Physics.SyncTransforms();state.TickPassives(100);
            Check(relics.Activate(0)&&Mathf.Abs(hp-enemy.Health-70)<.01f,"Censer applies 70");hp=enemy.Health;
            state.TickPassives(100);Check(relics.Activate(1)&&Mathf.Abs(hp-enemy.Health-24)<.01f&&enemy.IsStunned,"Frost applies 24 and stun");
            state.EquipRelic(ActiveRelic.BloodLance,0);state.TickPassives(100);player.view.transform.LookAt(enemy.GetComponent<Collider>().bounds.center);
            wall.SetActive(true);Physics.SyncTransforms();hp=enemy.Health;Check(relics.Activate(0)&&enemy.Health==hp,"Lance blocked by wall");
            wall.SetActive(false);Physics.SyncTransforms();state.TickPassives(100);Check(relics.Activate(0)&&Mathf.Abs(hp-enemy.Health-85)<.01f,"Lance applies 85");
            state.EquipRelic(ActiveRelic.AshenWard,0);state.TickPassives(100);var mechanics=player.GetComponent<PlayerMechanics>();var hit=new DamageInfo{amount=100};float before=mechanics.ModifyIncoming(hit);
            Check(relics.Activate(0)&&Mathf.Abs(mechanics.ModifyIncoming(hit)-before*.65f)<.01f,"Ward reduces incoming damage 35 percent");
            await Task.Delay(100);return "Four active powers passed: real damage, stun, wall occlusion, 35% ward reduction.";
        }
        finally{state.Restore(original);player.view.transform.rotation=rotation;UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(enemy.gameObject);}
    }
    public static string ActiveReward()
    {
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var altar=RelicAltar.Create(player.transform.position+Vector3.forward,67,true);altar.Interact(player);
        Check(RunHUD.Instance.RelicSelectionVisible&&altar.Offers[0].active,"Active altar opens");var offered=(ActiveRelic)altar.Offers[0].kind;
        var panel=RunHUD.Instance.transform.Find("Relic choice");panel.Find("Relic offer 0/ELEGIR").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        var buttons=panel.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name==""&&b.gameObject.activeInHierarchy).ToArray();
        Check(buttons.Length==2,"Two active replacement slots");buttons[1].onClick.Invoke();
        Check(player.State.GetRelic(1)==offered&&!RunHUD.Instance.RelicSelectionVisible&&Time.timeScale==1,"G replacement grants and resumes");
        Check(!altar.Claim(player,0,0),"Second claim rejected");return "Active reward selection and replacement in G passed; double claim rejected.";
    }
    public static async Task<string> Boss()
    {
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();RunHUD.Instance.SetPaused(false);
        var boss=UnityEngine.Object.Instantiate(Resources.Load<BossCharacter>("ExpeditionEnemies/HorrorInquisitor"),player.transform.position+Vector3.forward*3,Quaternion.identity);boss.Initialize(player,1);
        var presentation=boss.GetComponent<ActorPresentation>();var states=new HashSet<string>();var attacks=new HashSet<BossAttack>();
        try
        {
            for(int i=0;i<100;i++){await Task.Delay(100);player.State.Heal(1000);states.Add(presentation.CurrentState);attacks.Add(boss.CurrentAttack);}
            Check(attacks.Contains(BossAttack.Sweep)&&attacks.Contains(BossAttack.Slam),"Boss uses sweep and slam");
            Check(states.Contains("Melee")&&states.Contains("Slam"),"Distinct boss animation states");boss.ApplyStun(.5f);await Task.Delay(150);Check(presentation.CurrentState=="Stun","Boss stun loop");
            boss.ReceiveDamage(new DamageInfo{amount=10000,source=player.gameObject,ignoreGuard=true});await Task.Delay(150);
            Check(UnityEngine.Object.FindObjectsByType<Animator>().Any(a=>a.name=="Animated remains"),"Boss death persists");
            string report="Boss passed: "+string.Join(",",states)+"; sweep, slam, stun and death.";System.IO.File.WriteAllText("Docs/BossValidation.txt",report);return report;
        }
        finally{if(boss)UnityEngine.Object.Destroy(boss.gameObject);}
    }
}
