using UnityEngine;
namespace Codice.Characters
{
 public sealed partial class PlayerCombat
 {
  float bufferedUntil;WeaponInstance bufferedWeapon;Vector3 attackDirection;
  public static bool SwordCombo(WeaponInstance w)=>w!=null&&w.kind==WeaponKind.Sword&&!w.katana;
  // Katana chains share the input buffer and soft aim of the sword, without its third-hit pause.
  public static bool BufferedMelee(WeaponInstance w)=>w!=null&&!w.Ranged&&w.kind!=WeaponKind.Staff;
  public float SwingProgress=>swinging==null?1:Mathf.Clamp01(swingElapsed/Mathf.Max(.01f,swingDuration));
  public bool CanDefend=>swinging==null||!SwordCombo(swinging)||SwingProgress>=.62f;
  public bool RequestAttack()
  {
   if(TryAttack())return true;
   if(BufferedMelee(player.State.ActiveWeapon)&&player.State.IsAlive&&!player.ControlsBlocked&&!player.IsStaggered&&!player.IsDashing&&!Blocking&&Time.timeScale>0)
   {bufferedWeapon=player.State.ActiveWeapon;bufferedUntil=Time.time+(SwordCombo(player.State.ActiveWeapon)?.28f:.30f);}
   return false;
  }
  void TickBufferedAttack()
  {
   if(bufferedWeapon==null)return;
   if(!player.State.IsAlive||player.ControlsBlocked||player.IsStaggered||player.IsDashing||bufferedWeapon!=player.State.ActiveWeapon||Time.time>bufferedUntil){bufferedWeapon=null;bufferedUntil=0;return;}
   if(Time.timeScale>0&&Time.time>=nextAttack&&!IsSwinging)TryAttack();
  }
  void InterruptCombo(){CancelSwing();bufferedWeapon=null;bufferedUntil=0;combo=0;}
  public bool TryCancelForDodge()
  {
   // Dodge owns the action immediately, including windup and pending ranged releases.
   // Spent ammunition and mana stay spent; already released projectiles remain alive.
   bool interrupted=IsSwinging||IsReloading;
   InterruptCombo();reloading=null;reloadElapsed=0;
   nextAttack=Mathf.Min(nextAttack,Time.time+PlayerCharacter.DodgeDuration);
   GetComponent<StaffSpells>()?.CancelForDodge();
   GetComponent<RunSpellbook>()?.CancelForDodge();
   var special=GetComponent<Codice.Mechanics.PlayerMechanics>();
   if(special&&(special.UltimateActive||special.UltimateQueued))special.Cancel();
   GetComponent<ShinobiGuard>()?.SetGuard(false);
   GetComponent<ChargedAttack>()?.Cancel();
   if(interrupted)Debug.Log("[DodgeCancel] Interrupted action; pending hits/releases invalidated");
   return true;
  }
  Vector3 AssistDirection(WeaponInstance weapon)
  {
   var origin=Codice.Presentation.HumanPreviewCamera.Origin(player);var forward=Codice.Presentation.HumanPreviewCamera.Aim(player);
   if(!BufferedMelee(weapon))return forward;
   float best=18;Vector3 result=forward;
   foreach(var target in Codice.Mechanics.CombatTargets.InRadius(origin,MeleeRange(weapon.kind)))
   {var col=target.GetComponent<Collider>();if(!col)continue;Vector3 direction=(col.bounds.center-origin).normalized;float angle=Vector3.Angle(forward,direction);if(angle<best){best=angle;result=Vector3.RotateTowards(forward,direction,12*Mathf.Deg2Rad,0);}}
   return result;
  }
 }
}
