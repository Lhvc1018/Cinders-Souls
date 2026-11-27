using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Codice.Characters;
using Codice.Presentation;
using Codice.UI;
public static class ValidateSpells
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static async Task<string> Run()
 {
  for(int i=0;i<100&&!RunHUD.Instance;i++)await Task.Delay(100);var hud=RunHUD.Instance;Check(hud!=null,"No HUD");hud.SetPaused(false);var p=hud.Player;p.SetNarrativeControlsBlocked(false);p.enabled=false;
  foreach(var e in UnityEngine.Object.FindObjectsByType<Codice.Enemies.BasicEnemy>())e.enabled=false;
  var book=p.GetComponent<RunSpellbook>();Check(book!=null,"Spellbook missing");
  foreach(RunSpell spell in Enum.GetValues(typeof(RunSpell))){p.State.Initialize(PlayerClass.Mage);book.Ready=0;book.Learn(spell);float mana=p.State.Mana;Check(book.Cast(),"Cast failed "+spell);Check(Mathf.Approximately(mana-p.State.Mana,RunSpellbook.Costs[(int)spell]),"Mana cost "+spell);}
  Check(book.Known.Count==6&&book.DamageFactor>2,"Buff multiplication missing");
  Time.timeScale=10;await Task.Delay(2300);Time.timeScale=1;
  Check(p.State.Health>=p.State.MaxHealth*.2f-.01f&&p.State.Health<=p.State.MaxHealth*.21f,"Drain floor "+p.State.Health);
  Check(book.DamageFactor==1,"Buff expired incorrectly");
  var status=p.GetComponent<ElementalStatus>();p.State.Initialize(PlayerClass.Warrior);status.Add(Element.Poison,45,null);Check(status.Buildup[3]==45,"Buildup partial");status.Add(Element.Poison,55,null);Check(status.Until[3]>Time.time&&status.Buildup[3]==0,"Poison threshold");
  status.Add(Element.Frost,100,null);Check(status.Frozen&&p.IsStaggered,"Freeze does not immobilize");
  var camera=p.GetComponent<HumanPreviewCamera>();p.State.Initialize(PlayerClass.Warrior);p.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Sword,1),0);p.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Sword,1),1);
  if(!camera.ThirdPerson)camera.Toggle();await Task.Delay(150);
  Check(p.GetComponent<ActorPresentation>().Body.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Third person"))==2,"Dual grips missing");camera.Toggle();
  Check(p.GetComponent<ActorPresentation>().Arms.gameObject.activeSelf,"First person restoration");
  var pickups=UnityEngine.Object.FindObjectsByType<SpellPickup>();Check(pickups.Length>0,"No random spells");
  return "Six casts and mana costs; timed buffs; health floor 20%; poison accumulation; freeze; dual hand grips; random spell pickups passed";
 }
}

