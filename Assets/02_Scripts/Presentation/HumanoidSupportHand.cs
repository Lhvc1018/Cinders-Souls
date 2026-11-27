using UnityEngine;
using Codice.Characters;
using Codice.Mechanics;
namespace Codice.Presentation
{
 public sealed class HumanoidSupportHand:MonoBehaviour
 {
  PlayerCharacter player;Animator animator;Transform leftSocket,rightSocket;GameObject flask;
  public float SupportWeight {get;private set;}
  public void Initialize(PlayerCharacter value){player=value;animator=GetComponent<Animator>();leftSocket=HumanoidWeaponGrip.Socket(animator,true);rightSocket=HumanoidWeaponGrip.Socket(animator,false);var prefab=Resources.Load<GameObject>("CindersDungeon/EstusBottle");if(prefab){flask=Instantiate(prefab,rightSocket,false);PickupScale.Fit(flask,.2f);flask.SetActive(false);}}
    void LateUpdate()
  {
   if(flask&&player)flask.SetActive((player.GetComponent<HumanPreviewCamera>()?.ThirdPerson??false)&&(player.GetComponent<EstusFlask>()?.Drinking??false));
   if(!player||!animator)return;
   CorrectStaffGrip();
   if(SupportWeight<.9f)return;
   var mode=player.GetComponent<HumanPreviewCamera>();if(!mode||!mode.ThirdPerson||!mode.HeldWeapon)return;
   var w=player.State.ActiveWeapon;if(w==null)return;
   var weapon=mode.HeldWeapon;float along=w.katana||w.kind==WeaponKind.Hammer?-.11f:.32f;
   Quaternion rotation=weapon.rotation*Quaternion.Inverse(leftSocket.localRotation);
   Vector3 target=weapon.TransformPoint(new Vector3(0,along,0))-rotation*Vector3.Scale(leftSocket.localPosition,leftSocket.parent.lossyScale);
   SolveSupportArm(target,rotation);
  }
  void SolveSupportArm(Vector3 target,Quaternion rotation)=>SolveArm(target,rotation,true);
  void SolveArm(Vector3 target,Quaternion rotation,bool left)
  {
   var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
   var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
   var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
   Vector3 origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,hand.position);
   Vector3 offset=target-origin;float distance=Mathf.Clamp(offset.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
   Vector3 direction=offset.normalized;
   Vector3 bend=Vector3.ProjectOnPlane(lower.position-origin,direction).normalized;
   if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(-player.transform.right,direction).normalized;
   float adjacent=(a*a+distance*distance-b*b)/(2*distance);
   Vector3 elbow=origin+direction*adjacent+bend*Mathf.Sqrt(Mathf.Max(0,a*a-adjacent*adjacent));
   upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
   lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,origin+direction*distance-lower.position)*lower.rotation;
   hand.rotation=rotation;
  }
  void CorrectStaffGrip()
  {
   var mode=player.GetComponent<HumanPreviewCamera>();var weapon=player.State.ActiveWeapon;
   if(!mode||!mode.ThirdPerson||!mode.HeldWeapon||weapon==null||weapon.kind!=WeaponKind.Staff||!player.State.IsAlive||player.IsDashing||(player.GetComponent<EstusFlask>()?.Drinking??false))return;
   // Left hand carries an upright staff; the native right-hand clip performs the spell.
   Quaternion rotation=Quaternion.LookRotation(transform.forward,transform.up)*Quaternion.Inverse(leftSocket.localRotation);
   var shoulder=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
   Vector3 grip=shoulder.position-transform.up*.25f+transform.forward*.18f;
   SolveArm(grip-rotation*Vector3.Scale(leftSocket.localPosition,leftSocket.parent.lossyScale),rotation,true);
  }
  void OnAnimatorIK(int layer)
  {
   if(!player||!animator||layer!=0)return;
   var mode=player.GetComponent<HumanPreviewCamera>();var w=player.State.ActiveWeapon;var drink=player.GetComponent<EstusFlask>();
   animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0);animator.SetIKRotationWeight(AvatarIKGoal.RightHand,0);
   if(mode&&mode.ThirdPerson&&drink&&drink.Drinking){
    float phase=drink.Progress;float weight=Mathf.SmoothStep(0,1,Mathf.Min(phase/.2f,(1-phase)/.22f));
    var head=animator.GetBoneTransform(HumanBodyBones.Head);Vector3 mouth=head.position+player.transform.forward*.14f-Vector3.up*.045f;
    Quaternion rotation=Quaternion.LookRotation(player.transform.forward,Vector3.up)*Quaternion.Euler(0,0,75)*Quaternion.Inverse(rightSocket.localRotation);
    animator.SetIKPositionWeight(AvatarIKGoal.RightHand,weight);animator.SetIKRotationWeight(AvatarIKGoal.RightHand,weight);
    animator.SetIKPosition(AvatarIKGoal.RightHand,mouth-rotation*Vector3.Scale(rightSocket.localPosition,rightSocket.parent.lossyScale));animator.SetIKRotation(AvatarIKGoal.RightHand,rotation);
    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,0);animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,0);SupportWeight=0;return;
   }
   bool supports=w!=null&&(w.kind==WeaponKind.Musket);
   bool active=supports&&mode&&mode.ThirdPerson&&mode.HeldWeapon&&!player.State.DualWield&&!player.IsDashing&&player.Grounded&&player.State.IsAlive;
   SupportWeight=active?1:0;animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,SupportWeight);animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,SupportWeight*.9f);
   if(!active)return;
   var weapon=mode.HeldWeapon;float along=w.katana||w.kind==WeaponKind.Hammer?-.11f:.32f;
   var rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);Quaternion handRotation=rightHand.rotation;
   Vector3 socketPosition=rightHand.position+handRotation*Vector3.Scale(rightSocket.localPosition,rightSocket.parent.lossyScale);
   Quaternion weaponRotation=handRotation*rightSocket.localRotation*weapon.localRotation;
   Vector3 grip=socketPosition+weaponRotation*new Vector3(0,along,0);
   Quaternion rotationLeft=weaponRotation*Quaternion.Inverse(leftSocket.localRotation);
      Vector3 leftTarget=grip-rotationLeft*Vector3.Scale(leftSocket.localPosition,leftSocket.parent.lossyScale);
   // One-handed source clips can extend beyond the support arm. Retract the dominant
   // hand into the shared reach volume instead of stretching the other arm.
   var shoulder=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
   var elbow=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
   var wrist=animator.GetBoneTransform(HumanBodyBones.LeftHand);
   float reach=(Vector3.Distance(shoulder.position,elbow.position)+Vector3.Distance(elbow.position,wrist.position))*.88f;
   Vector3 corrected=shoulder.position+Vector3.ClampMagnitude(leftTarget-shoulder.position,reach);
   Vector3 correction=corrected-leftTarget;
   animator.SetIKPositionWeight(AvatarIKGoal.RightHand,1);animator.SetIKRotationWeight(AvatarIKGoal.RightHand,1);
   animator.SetIKPosition(AvatarIKGoal.RightHand,rightHand.position+correction);animator.SetIKRotation(AvatarIKGoal.RightHand,handRotation);
   animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,1);
   animator.SetIKPosition(AvatarIKGoal.LeftHand,corrected);animator.SetIKRotation(AvatarIKGoal.LeftHand,rotationLeft);
  }
 }
}



