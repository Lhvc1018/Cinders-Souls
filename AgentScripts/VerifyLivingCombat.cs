using System;using System.Linq;using System.Threading.Tasks;using UnityEngine;using Codice.Mechanics;using Codice.Expedition;using Codice.Characters;
public static class VerifyLivingCombat {
 public static string Start(){var r=RunDirector.Instance;JamDebug.Teleport(r.Expedition.Layout.Boss);r.Player.State.SpendMana(40);return "Boss combat started";}
 public static async Task<string> Attack(){var r=RunDirector.Instance;var boss=r.Expedition.context.arena.ActiveBoss;if(!boss)throw new Exception("No boss");float hp=boss.Health;float mana=r.Player.State.Mana;await Task.Delay(8000);if(boss.Health>=hp)throw new Exception("Companion did not hit boss: "+hp+"/"+boss.Health);return "PASS companion hits boss: "+hp+" -> "+boss.Health+"; mana "+mana+" -> "+r.Player.State.Mana;}
 public static string Travel(){var r=RunDirector.Instance;var b=r.Expedition.context.arena.ActiveBoss;b.ReceiveDamage(new DamageInfo{amount=1000000,source=r.Player.gameObject,ignoreGuard=true});return "Boss defeated; travel="+r.TravelNextFloor();}
 public static string Carried(){var r=RunDirector.Instance;if(r.FloorNumber!=3||!r.HiredPilgrim||!r.BondedWisp||UnityEngine.Object.FindObjectsByType<DescentCompanion>().Length!=2)throw new Exception("Lost contracts on floor transition");return "PASS contracts and both companions restored on floor 3";}
 public static string Restart(){var r=RunDirector.Instance;r.SetExpeditionPhase(RunPhase.Defeated,"test");return "Restart="+r.RestartRun();}
 public static string Reset(){var r=RunDirector.Instance;if(r.HiredPilgrim||r.BondedWisp||UnityEngine.Object.FindObjectsByType<DescentCompanion>().Length!=0)throw new Exception("Contracts survive run reset");if(DescentMemory.Keys!=7)throw new Exception("Keys lost on reset");return "PASS restart clears contracts, retains permanent keys";}
}


