using System;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using Codice.Characters;using Codice.Presentation;using Codice.UI;
public static class VerifyShieldComboStep{
 static BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);}
 public static async Task<string> Run(){
 if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Descenso")UnityEngine.SceneManagement.SceneManager.LoadScene("Descenso");for(int i=0;i<100&&(!RunHUD.Instance||!RunHUD.Instance.Player);i++)await Task.Delay(100);await Task.Delay(400);
 var p=RunHUD.Instance.Player;p.State.Initialize(PlayerClass.Warrior);p.SetNarrativeControlsBlocked(false);RunHUD.Instance.SetPaused(false);await Task.Delay(200);var c=p.GetComponent<PlayerCombat>();var ap=p.GetComponent<ActorPresentation>();p.enabled=false;c.enabled=false;
 try{
 c.GetType().GetMethod("UpdateWeapon",F).Invoke(c,new object[]{p.State.ActiveWeapon});for(int step=1;step<=3;step++){c.GetType().GetField("nextAttack",F).SetValue(c,Time.time);Check(c.TryAttack(),"Attack rejected "+step);Check(c.Combo==step,"Combo resets between hits");Check(ap.CurrentState==(step==2?"ShieldStrike":"SwordCombo"+step),"Incorrect combo action "+ap.CurrentState);c.GetType().GetMethod("UpdateSwing",F).Invoke(c,new object[]{10f});}
 c.GetType().GetMethod("InterruptCombo",F).Invoke(c,null);p.GetType().GetField("nextDash",F).SetValue(p,0f);Check(p.HandleDodgePress(Vector3.right),"Short dodge rejected");Check(p.ShortDodge&&ap.CurrentState=="QuickStep","First Shift rolls");await Task.Delay(100);Check(p.HandleDodgePress(Vector3.right),"Double Shift rejected");Check(!p.ShortDodge&&ap.CurrentState=="Dash","Second Shift does not roll");await Task.Delay(500);Check(!p.IsDashing&&!p.DodgeInvulnerable,"Dodge never ends");Check(!p.HandleDodgePress(Vector3.right),"Cooldown bypassed");
 Debug.Log("PASS sword-shield-sword native combo; single Shift short dodge; double Shift roll; finite invulnerability and cooldown");return "PASS 3-hit sword/shield chain and single/double Shift dodge controls";
 }finally{p.enabled=true;c.enabled=true;}
 }
}
