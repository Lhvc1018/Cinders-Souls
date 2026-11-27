using System;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using Codice.Characters;using Codice.Mechanics;using Codice.Presentation;
public static class VerifyClassCombat{
 public static async Task<string> Run(){
  var p=RunDirector.Instance.Player;var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();if(!p||!room)throw new Exception("Laboratory required");
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var combat=p.GetComponent<PlayerCombat>();int checkedRoles=0;
  foreach(PlayerClass role in Enum.GetValues(typeof(PlayerClass))){
   if(!room.ChangeClass(role))throw new Exception("Cannot switch "+role);await Task.Delay(180);
   typeof(CombatTestRoom).GetMethod("SetPanel",flags).Invoke(room,new object[]{false});
   typeof(PlayerCombat).GetField("nextAttack",flags).SetValue(combat,0f);p.SetNarrativeControlsBlocked(false);
   if(!combat.TryAttack())throw new Exception("Attack rejected "+role+" controls="+p.ControlsBlocked);
   await Task.Delay(160);var presentation=p.GetComponent<ActorPresentation>();var a=presentation.Body;
   if(!p.AttackMovementLocked)throw new Exception("Attack does not lock movement "+role);
   var start=p.transform.position;p.SimulateMotor(Vector3.forward,false,.02f);
   if(Vector3.ProjectOnPlane(p.transform.position-start,Vector3.up).magnitude>.01f)throw new Exception("Attack slides "+role);
   float lean=Vector3.Angle(a.GetBoneTransform(HumanBodyBones.Head).position-a.GetBoneTransform(HumanBodyBones.Hips).position,Vector3.up);
   if(lean>45)throw new Exception("Excessive live torso lean "+role+" "+lean);
   string state=presentation.CurrentState;
   if(!p.HandleDodgePress(p.transform.forward)||!p.IsDashing||p.DodgeAnimation!="Dash")throw new Exception("Original roll/cancel failed "+role);
   if(combat.IsSwinging||p.GetComponent<StaffSpells>().Casting||p.GetComponent<RunSpellbook>().Casting)throw new Exception("Dodge left active attack "+role);
   Debug.Log("[ClassCombat] PASS "+role+" state="+state+" lean="+lean+" movement locked; Shift cancels to original roll");checkedRoles++;await Task.Delay(550);
  }
  room.ChangeClass(PlayerClass.Warrior);typeof(CombatTestRoom).GetMethod("SetPanel",flags).Invoke(room,new object[]{true});
  return "PASS "+checkedRoles+" class attacks, movement locks, bounded torso lean and original Shift dodge cancellation.";
 }
}
