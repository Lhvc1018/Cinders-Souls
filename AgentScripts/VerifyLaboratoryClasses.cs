using System;using System.Linq;using System.Threading.Tasks;using UnityEngine;using Codice.Mechanics;using Codice.Characters;using Codice.Presentation;
public static class VerifyLaboratoryClasses{
 public static async Task<string> Run(){
  var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();var player=RunDirector.Instance.Player;if(!room||!player)throw new Exception("Open Laboratory in play mode");
  int clips=0;
  foreach(PlayerClass role in Enum.GetValues(typeof(PlayerClass))){
   if(!room.ChangeClass(role))throw new Exception("Class switch rejected: "+role);await Task.Delay(160);
   var presentation=player.GetComponent<ActorPresentation>();var body=presentation.Body;
   if(player.State.Class!=role||!body||!body.avatar.isHuman||!player.State.LaboratoryImmortal)throw new Exception("Class state invalid "+role);
   if(player.State.Health!=player.State.MaxHealth||player.State.Mana!=player.State.MaxMana)throw new Exception("Resources not restored");
   int available=body.runtimeAnimatorController.animationClips.Where(c=>c.name.StartsWith("AS_")).Distinct().Count();if(available<13)throw new Exception("Missing Lords preview clips "+role);
   if(!room.PreviewAnimation(0))throw new Exception("Preview failed "+role);await Task.Delay(120);
   if(!presentation.LaboratoryPreviewActive||!player.ControlsBlocked||!body.GetCurrentAnimatorStateInfo(0).IsName("LordsPreview_"+body.runtimeAnimatorController.animationClips.Where(c=>c.name.StartsWith("AS_")).Distinct().OrderBy(c=>c.name).First().name))throw new Exception("Preview state not active "+role);
   clips+=available;Debug.Log("[LabClassTest] PASS "+role+": avatar, equipment, resources, immortality, "+available+" previews");
  }
  room.ChangeClass(PlayerClass.Warrior);await Task.Delay(150);return "PASS six laboratory class switches and "+clips+" preview entries; resources restored, controls locked during playback, immortality retained.";
 }
}
