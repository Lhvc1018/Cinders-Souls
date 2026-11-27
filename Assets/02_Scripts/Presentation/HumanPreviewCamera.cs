using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Codice.Characters;
namespace Codice.Presentation
{
 [DefaultExecutionOrder(200)]
 public sealed class HumanPreviewCamera:MonoBehaviour
 {
  PlayerCharacter player;ActorPresentation presentation;Vector3 cameraHome;bool preview;GameObject weapon;WeaponKind shown=(WeaponKind)(-1);GameObject leftWeapon;string signature="";
  public bool ThirdPerson=>preview;
  public Transform HeldWeapon=>weapon?weapon.transform:null;
  public Transform OffhandWeapon=>leftWeapon?leftWeapon.transform:null;
  public Vector3 CombatOrigin=>transform.TransformPoint(cameraHome);
  void Start(){player=GetComponent<PlayerCharacter>();presentation=GetComponent<ActorPresentation>();cameraHome=player.view.transform.localPosition;preview=true;Apply();}
  public void RefreshBody(){signature="";if(weapon)Destroy(weapon);if(leftWeapon)Destroy(leftWeapon);if(player&&presentation)Apply();}
  // First person was removed: the game is always played in third person.
  public void Toggle(){preview=true;Apply();}
  void Apply(){if(!presentation.Body)return;foreach(var r in presentation.Body.GetComponentsInChildren<Renderer>())r.shadowCastingMode=preview?ShadowCastingMode.On:ShadowCastingMode.ShadowsOnly;if(presentation.Arms)presentation.Arms.gameObject.SetActive(!preview);if(!preview){player.view.transform.localPosition=cameraHome;if(weapon)Destroy(weapon);if(leftWeapon)Destroy(leftWeapon);signature="";shown=(WeaponKind)(-1);}}
  public static Vector3 Origin(PlayerCharacter p){if(Codice.Multiplayer.LanAvatar.IsRemote(p))return p.transform.position+Vector3.up*1.35f;   /* coop guest on the host: its third-person rig is not updated, so CombatOrigin sat ~3 m behind it and its hits missed */var camera=p.GetComponent<HumanPreviewCamera>();return camera&&camera.ThirdPerson?camera.CombatOrigin:p.view.transform.position;}
  public static Vector3 ReleaseOrigin(PlayerCharacter p,bool handSpell=false)
  {
   Vector3 origin=Origin(p);var mode=p.GetComponent<HumanPreviewCamera>();if(!mode||!mode.ThirdPerson)return origin;
   Vector3 point=origin;
   if(handSpell){var hand=mode.presentation.Body.GetBoneTransform(HumanBodyBones.LeftHand);if(hand)point=hand.position;}
   else if(mode.HeldWeapon){point=mode.HeldWeapon.position;foreach(var t in mode.HeldWeapon.GetComponentsInChildren<Transform>())if(t.name=="Muzzle"){point=t.position;break;}}
   Vector3 offset=point-origin;
   foreach(var hit in Physics.RaycastAll(origin,offset.normalized,offset.magnitude,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(p.transform))return origin;
   return point;
  }
  public static Vector3 AimPoint(PlayerCharacter p)
  {
   var target=p.GetComponent<PlayerLockOn>();if(target&&target.Target)return target.Point;
   Vector3 start=p.view.transform.position,forward=p.view.transform.forward,end=start+forward*80;float nearest=80;
   foreach(var hit in Physics.RaycastAll(start,forward,80,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(p.transform)&&hit.distance<nearest){nearest=hit.distance;end=hit.point;}
   return end;
  }
  public static Vector3 AimFrom(PlayerCharacter p,Vector3 origin){var av=p?p.GetComponent<Codice.Multiplayer.LanAvatar>():null;if(av&&av.remote){var lockOn=p.GetComponent<PlayerLockOn>();if(lockOn&&lockOn.Target)return (lockOn.Point-origin).normalized;return Quaternion.Euler(Mathf.Clamp(av.pitch,-60,60),av.yaw,0)*Vector3.forward;}return (AimPoint(p)-origin).normalized;}   /* coop guest on the host: aim where the guest looks (its camera rig isn't simulated here; was ~40° off) */
  public static Vector3 Aim(PlayerCharacter p)=>AimFrom(p,Origin(p));
  void LateUpdate()
  {
   if(!player||!presentation.Body)return;
   if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Camera)&&!(Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.Paused))Toggle();
   if(!preview)return;
   var origin=transform.TransformPoint(new Vector3(.3f,1.45f,0));var back=-player.view.transform.forward;float distance=2.75f;
   foreach(var hit in Physics.SphereCastAll(origin,.2f,back,distance,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(transform))distance=Mathf.Min(distance,Mathf.Max(.3f,hit.distance-.1f));
   player.view.transform.position=origin+back*distance;
   var lockOn=player.GetComponent<PlayerLockOn>();if(lockOn&&lockOn.Target)player.view.transform.rotation=Quaternion.LookRotation(lockOn.Point-player.view.transform.position,Vector3.up);
   var w=player.State.ActiveWeapon;var second=player.State.GetWeapon(1-player.State.ActiveSlot);
   bool knightShield=player.GetComponent<PlayerCombat>()?.HasKnightShield??false;
   string key=w==null?"":$"{w.kind}:{w.tier}:{w.katana}:{w.shuriken}:{player.State.DualWield}:{second?.kind}:{second?.tier}:{knightShield}";
   if(presentation.Arms)presentation.Arms.gameObject.SetActive(false);
   bool drinking=player.GetComponent<Codice.Mechanics.EstusFlask>()?.Drinking??false;if(weapon)weapon.SetActive(!drinking);if(leftWeapon)leftWeapon.SetActive(!drinking);
   if(key==signature)return;signature=key;if(weapon)Destroy(weapon);if(leftWeapon)Destroy(leftWeapon);if(w==null)return;
   weapon=Held(w,(w.kind==WeaponKind.Bow&&!w.shuriken)||w.kind==WeaponKind.Shield||w.kind==WeaponKind.Staff);
   if(player.State.DualWield&&second!=null)leftWeapon=Held(second,true);
   else if(knightShield)leftWeapon=Held(new WeaponInstance(WeaponKind.Shield,w.tier),true);
  }
  GameObject Held(WeaponInstance item,bool left)=>HumanoidWeaponGrip.Attach(presentation.Body,item,left);
 }
}
