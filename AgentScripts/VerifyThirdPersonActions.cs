using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using Codice.Characters;using Codice.Presentation;using Codice.Mechanics;using Codice.UI;
public static class VerifyThirdPersonActions
{
 static BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);}
 public static async Task<string> Run(){
 if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Descenso")UnityEngine.SceneManagement.SceneManager.LoadScene("Descenso");for(int i=0;i<100&&(!RunHUD.Instance||!RunHUD.Instance.Player);i++)await Task.Delay(100);await Task.Delay(700);
 var p=RunHUD.Instance.Player;RunHUD.Instance.SetPaused(false);p.SetNarrativeControlsBlocked(false);Cursor.lockState=CursorLockMode.None;
 var ap=p.GetComponent<ActorPresentation>();
 foreach(PlayerClass cls in Enum.GetValues(typeof(PlayerClass))){p.State.Initialize(cls);await Task.Delay(130);Check(p.GetComponentsInChildren<Animator>().Count(a=>a.name=="Humanoid pilgrim")==1,"Duplicate bodies for "+cls);Check(p.GetComponents<HumanPreviewCamera>().Length==1,"Duplicate cameras");}
 p.State.Initialize(PlayerClass.Warrior);await Task.Delay(150);var mode=p.GetComponent<HumanPreviewCamera>();if(!mode.ThirdPerson)mode.Toggle();p.enabled=false;var combat=p.GetComponent<PlayerCombat>();combat.enabled=false;ap.enabled=false;
 var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(900,99.5f,900);ground.transform.localScale=new Vector3(30,1,30);var cc=p.GetComponent<CharacterController>();var saved=p.transform.position;cc.enabled=false;p.transform.position=new Vector3(900,100.05f,900);cc.enabled=true;Physics.SyncTransforms();for(int i=0;i<20;i++)p.SimulateMotor(Vector3.zero,true,1f/60);
 try{
 foreach(var state in new[]{"SwordCombo1","SwordCombo2","SwordCombo3","AxeAttack","ShootBow","DualSlice","DualStab","KatanaSlashA","KatanaSlashB","Conjure","StaffCast","ThrowShuriken","Heal","Movement","Dash","JumpRise","Fall","Land"})Check(ap.Body.HasState(0,Animator.StringToHash(state)),"Missing "+state);
 foreach(var kind in new[]{WeaponKind.Sword,WeaponKind.Axe,WeaponKind.Bow,WeaponKind.Hammer,WeaponKind.Staff}){
 var weapon=new WeaponInstance(kind,1);p.State.EquipIntoSlot(weapon,0);p.State.SelectWeapon(0);combat.GetType().GetMethod("UpdateWeapon",Flags).Invoke(combat,new object[]{weapon});await Task.Delay(100);Check(mode.HeldWeapon&&mode.HeldWeapon.IsChildOf(ap.Body.transform),"Missing grip "+kind);
 if(kind==WeaponKind.Staff){p.State.SelectSpell(0);Check(p.GetComponent<StaffSpells>().TryCast(~0),"Staff cast rejected");Check(p.GetComponent<StaffSpells>().Casting,"Staff release not delayed");await Task.Delay(1000);continue;}
 Set(combat,"nextAttack",0f);Check(combat.TryAttack(),"Attack rejected "+kind);if(kind==WeaponKind.Bow){Check(combat.IsSwinging,"Bow draw not pending");await Task.Delay(1000);Check(!combat.IsSwinging,"Bow release stuck");}else{combat.GetType().GetMethod("UpdateSwing",Flags).Invoke(combat,new object[]{3f});}
 }
 p.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Sword),0);p.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Sword),1);p.State.SelectWeapon(0);await Task.Delay(150);Check(p.State.DualWield&&p.GetComponentsInChildren<Transform>().Any(t=>t.name=="Third person left grip"),"Dual grip missing");Set(combat,"nextAttack",0f);combat.GetType().GetMethod("UpdateWeapon",Flags).Invoke(combat,new object[]{p.State.ActiveWeapon});Check(combat.TryAttack(),"Dual attack rejected");await Task.Delay(100);Check(ap.Body.GetLayerWeight(1)==0,"Procedural offhand layer should stay disabled");combat.GetType().GetMethod("UpdateSwing",Flags).Invoke(combat,new object[]{3f});
 p.State.Initialize(PlayerClass.Warrior);var book=p.GetComponent<RunSpellbook>();book.Learn(RunSpell.Fire);book.Ready=0;Check(book.Cast(),"Conjure rejected");Check(book.Casting&&ap.CurrentState=="Conjure","Conjure state incorrect");await Task.Delay(1000);
 p.State.Damage(50,true);var flask=p.GetComponent<EstusFlask>();Check(flask.TryDrink(),"Drink rejected");await Task.Delay(350);Check(ap.CurrentState=="Heal","Heal state missing");Check(ap.Body.GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("EstusBottle")&&t.gameObject.activeInHierarchy),"Hand flask missing");float time=ap.Body.GetCurrentAnimatorStateInfo(0).normalizedTime;await Task.Delay(850);Check(!flask.Drinking&&!p.ControlsBlocked,"Heal did not unlock controls");Check(time>0,"Healing animator frozen");
 string result="PASS all 18 animation states, 5 class swaps without duplicates, sword/axe/bow/staff actions, delayed releases, native dual attacks, conjure, hand flask and healing unlock";Debug.Log(result);return result;
 }finally{cc.enabled=false;p.transform.position=saved;cc.enabled=true;UnityEngine.Object.Destroy(ground);p.enabled=true;combat.enabled=true;ap.enabled=true;}
 }
}

