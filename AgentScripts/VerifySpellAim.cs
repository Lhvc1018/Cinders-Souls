using System;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;using Codice.Characters;using Codice.Presentation;using Codice.Enemies;using Codice.UI;
public static class VerifySpellAim{
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static async Task Wait(float seconds){float until=Time.time+seconds;for(int i=0;i<100&&Time.time<until;i++)await Task.Delay(50);Check(Time.time>=until,"Editor is paused");}
 public static async Task<string> Run(){
 var p=RunHUD.Instance.Player;p.SetNarrativeControlsBlocked(false);RunHUD.Instance.SetPaused(false);var ap=p.GetComponent<ActorPresentation>();var combat=p.GetComponent<PlayerCombat>();p.enabled=false;combat.enabled=false;
 var cc=p.GetComponent<CharacterController>();var saved=p.transform.position;var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1500,99.5f,1500);floor.transform.localScale=new Vector3(35,1,35);cc.enabled=false;p.transform.SetPositionAndRotation(new Vector3(1500,100.05f,1500),Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();for(int i=0;i<20;i++)p.SimulateMotor(Vector3.zero,true,1f/60);
 var target=new GameObject("Spell test target");target.transform.position=p.transform.position+Vector3.forward*5;var enemy=target.AddComponent<BasicEnemy>();enemy.healthMultiplier=100;enemy.Initialize(p);enemy.enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;enemy.GetComponent<ActorPresentation>().enabled=false;var collider=enemy.GetComponent<CapsuleCollider>();collider.center=Vector3.up;collider.height=2;collider.radius=.4f;
 var mode=p.GetComponent<HumanPreviewCamera>();if(!mode.ThirdPerson)mode.Toggle();p.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Staff),0);p.State.SelectWeapon(0);await Wait(.2f);p.view.transform.LookAt(target.transform.position+Vector3.up);Physics.SyncTransforms();var lockOn=p.GetComponent<PlayerLockOn>();Check(lockOn.Acquire(),"Lock target missing");
 try{
 var spells=p.GetComponent<StaffSpells>();
 foreach(int index in new[]{0,1,2}){
 enemy.Initialize(p);p.State.RestoreMana(200);p.State.SelectSpell(index);spells.GetType().GetField("globalReady",flags).SetValue(spells,0f);var ready=(float[])spells.GetType().GetField("ready",flags).GetValue(spells);Array.Clear(ready,0,ready.Length);
 float before=enemy.Health;Check(spells.TryCast(~0),"Spell rejected "+index);await Wait(1.2f);Check(enemy.Health<before,"Staff spell missed "+index+" health="+enemy.Health);
 }
 enemy.Initialize(p);p.State.RestoreMana(200);var book=p.GetComponent<RunSpellbook>();book.Learn(RunSpell.Fire);book.Ready=0;float health=enemy.Health;Check(book.Cast(),"Hand fire rejected");await Wait(1.3f);Check(enemy.Health<health,"Hand fire missed");
 Vector3 point=lockOn.Point;foreach(var offset in new[]{new Vector3(-.7f,0,0),new Vector3(.8f,-.6f,0),new Vector3(0,.9f,0)}){Vector3 origin=p.transform.position+Vector3.up+offset;float distance=Vector3.Cross(point-origin,HumanPreviewCamera.AimFrom(p,origin)).magnitude;Check(distance<.001f,"Parallax error "+distance);}
 p.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Bow),0);p.State.SelectWeapon(0);await Wait(.2f);combat.GetType().GetMethod("UpdateWeapon",flags).Invoke(combat,new object[]{p.State.ActiveWeapon});combat.GetType().GetField("nextAttack",flags).SetValue(combat,0f);enemy.Initialize(p);health=enemy.Health;Check(combat.TryAttack(),"Bow rejected");await Wait(1.5f);Check(enemy.Health<health,"Bow missed after native draw/release");
 Debug.Log("PASS arcane, ice, lightning, hand fire and bow damaged target; muzzle parallax corrected");return "PASS four spells and native bow hit; three offset aim rays converge";
 }finally{lockOn.Clear();p.enabled=true;combat.enabled=true;cc.enabled=false;p.transform.position=saved;cc.enabled=true;UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(floor);}
 }
}

