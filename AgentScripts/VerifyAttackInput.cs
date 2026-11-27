using System;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using Codice.Characters;using Codice.Presentation;using Codice.UI;
public static class VerifyAttackInput{
 public static async Task<string> Run(){
  var p=RunHUD.Instance.Player;var combat=p.GetComponent<PlayerCombat>();var ap=p.GetComponent<ActorPresentation>();p.State.Initialize(PlayerClass.Warrior);p.SetNarrativeControlsBlocked(false);RunHUD.Instance.SetPaused(false);p.enabled=false;var original=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  try{
   await Task.Delay(200);combat.GetType().GetMethod("UpdateWeapon",flags).Invoke(combat,new object[]{p.State.ActiveWeapon});combat.GetType().GetField("nextAttack",flags).SetValue(combat,0f);Cursor.lockState=CursorLockMode.Locked;
   InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});InputSystem.Update();combat.GetType().GetMethod("Update",flags).Invoke(combat,null);if(combat.Combo!=1)throw new Exception("First press did not start exactly one swing");
   await Task.Delay(2400);if(combat.Combo!=1||combat.IsSwinging)throw new Exception("Holding mouse repeated a sword attack: combo="+combat.Combo);
   InputSystem.QueueStateEvent(mouse,new MouseState{buttons=0});InputSystem.Update();await Task.Delay(100);InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});InputSystem.Update();combat.GetType().GetMethod("Update",flags).Invoke(combat,null);if(!combat.IsSwinging)throw new Exception("New press failed");if(ap.Body.GetFloat("ActionSpeed")>1.251f)throw new Exception("Attack playback too fast");
   Debug.Log("PASS held click produced one sword swing; new press starts another; native playback <=1.25x");return "PASS held click does not auto-combo; fresh click attacks";
  }finally{InputSystem.RemoveDevice(mouse);if(original!=null)original.MakeCurrent();p.enabled=true;Cursor.lockState=CursorLockMode.None;}
 }
}
