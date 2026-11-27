using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Codice.UI;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
public static class ValidateFinalUI
{
 public static async Task<string> Run()
 {
  for(int i=0;i<100&&!RunHUD.Instance;i++)await Task.Delay(100);await Task.Delay(300);var hud=RunHUD.Instance;var p=hud.Player;p.enabled=false;p.SetNarrativeControlsBlocked(false);hud.SetPaused(false);
  p.State.Initialize(PlayerClass.Warrior);p.State.EquipPassive(PassiveKind.Frenzy);p.State.EquipPassive(PassiveKind.VampiricHeart);p.State.EquipPassive(PassiveKind.Overload);
  var pickup=LootPickup.Create(p.transform.position+Vector3.forward,LootKind.Passive,1);pickup.passive=PassiveKind.MercuryBoots;hud.OpenPassiveChoice(pickup);
  if(!hud.RelicSelectionVisible||!hud.Paused)throw new Exception("Passive comparison missing");
  typeof(RunHUD).GetMethod("ConfirmRelic",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,new object[]{1});
  if(p.State.GetPassive(1)!=PassiveKind.MercuryBoots||hud.Paused)throw new Exception("Passive replacement failed");
  var camera=p.GetComponent<HumanPreviewCamera>();if(!camera.ThirdPerson)camera.Toggle();await Task.Delay(100);
  if(!camera.ThirdPerson||p.GetComponent<ActorPresentation>().Arms.gameObject.activeSelf||Vector3.Distance(camera.CombatOrigin,p.view.transform.position)<.3f)throw new Exception("Third person camera");
  camera.Toggle();await Task.Delay(50);if(camera.ThirdPerson||!p.GetComponent<ActorPresentation>().Arms.gameObject.activeSelf)throw new Exception("First person restore");
  hud.SetPaused(true);await Task.Delay(50);
  var labels=hud.GetComponentsInChildren<TMPro.TextMeshProUGUI>();foreach(var label in labels)if(label.transform.IsChildOf(hud.transform)&&label.isActiveAndEnabled&&label.text.Contains("ESTADO DEL PERSONAJE")&&label.isTextOverflowing)throw new Exception("Inventory overflow");
  Debug.Log("[Upgrade validation] Passive comparison/replacement, third/first camera toggle and inventory text passed");return "Final UI checks passed";
 }
}

