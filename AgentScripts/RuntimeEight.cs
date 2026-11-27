using System;
using System.Linq;
using UnityEngine;
using Codice.Characters;
using Codice.UI;
using Codice.Mechanics;
using Codice.Presentation;
public static class RuntimeEight {
 public static string Load(){UnityEditor.EditorApplication.isPaused=false;MainMenuUI.UnlockCheats("jam");RunNavigation.StartExpeditionTest(PlayerClass.Archer,4);return "Loading 2-1";}
 public static string Inspect(){var r=RunDirector.Instance;return "floor="+r.Expedition.FloorNumber+" seed="+r.Expedition.Seed+" boss="+Codice.Expedition.ExpeditionFloor.BossResource(r.Expedition.Seed,r.Expedition.FloorNumber);}
 public static string ClassChecks(){var p=RunDirector.Instance.Player;var old=p.State.Capture();var i=p.GetComponent<ClassIdentity>();p.State.Initialize(PlayerClass.Tank);float damage=i.Incoming(new DamageInfo{amount=100,melee=true});if(damage!=80)throw new Exception("Tank reduction");var target=new GameObject("Test target");float power=i.Outgoing(target.transform,new WeaponInstance(WeaponKind.Hammer));UnityEngine.Object.Destroy(target);if(power!=1.8f)throw new Exception("Tank discharge");p.State.Initialize(PlayerClass.Mage);i.SpellPower(StaffSpell.Arcane);i.SpellPower((StaffSpell)1);if(i.SpellPower(StaffSpell.Arcane)!=1.65f)throw new Exception("Mage overload");p.State.Restore(old);Debug.Log("[CombatEight] PASS tank reduction and discharge; Mage alternating overload");return "PASS class mechanics";}
 public static string PrepareBoss(){var r=RunDirector.Instance;var f=r.Expedition;for(int n=0;n<f.Layout.Boss;n++)f.Cleared[n]=true;var c=r.Player.GetComponent<CharacterController>();c.enabled=false;r.Player.transform.position=f.Center(f.Layout.Boss);c.enabled=true;r.Player.State.LaboratoryImmortal=true;return "Boss prepared";}
 public static string Death(){var b=RunDirector.Instance.Expedition.context.arena.ActiveBoss;if(!b)throw new Exception("No boss");b.GetComponent<ActorPresentation>().Die();return "Started corpse animation";}
 public static string Corpse(){var g=GameObject.Find("Animated remains");if(!g)throw new Exception("Corpse missing");var a=g.GetComponent<Animator>();var s=a.GetCurrentAnimatorStateInfo(0);if(!s.IsName("Death")||s.normalizedTime<=0)throw new Exception("Death not playing");return "PASS moving corpse at "+s.normalizedTime;}

 public static async System.Threading.Tasks.Task<string> DeathCheck(){var b=RunDirector.Instance.Expedition.context.arena.ActiveBoss;b.GetComponent<ActorPresentation>().Die();await System.Threading.Tasks.Task.Delay(800);return Corpse();}
 public static string ArrowCheck(){var p=RunDirector.Instance.Player;var c=p.GetComponent<HumanPreviewCamera>();if(!c.ThirdPerson)c.Toggle();var arrow=p.transform.Find("Third person nocked arrow");if(!arrow||!arrow.gameObject.activeInHierarchy||!arrow.GetComponentInChildren<Renderer>().enabled)throw new Exception("Arrow missing");return "PASS nocked arrow rendered";}
 public static async System.Threading.Tasks.Task<string> SpecialCheck(){var p=RunDirector.Instance.Player;p.State.Initialize(PlayerClass.Warrior);p.State.ActiveWeapon.ultimateCharge=100;p.State.LaboratoryImmortal=true;var m=p.GetComponent<PlayerMechanics>();if(!m.TryUltimate())throw new Exception("Q rejected");await System.Threading.Tasks.Task.Delay(180);var a=p.GetComponent<ActorPresentation>();if(!m.UltimateActive||!a.CurrentState.Contains("SwordCombo"))throw new Exception("No melee pose: "+a.CurrentState);return "PASS Q pose="+a.CurrentState+" progress="+a.Body.GetCurrentAnimatorStateInfo(0).normalizedTime;}
}

