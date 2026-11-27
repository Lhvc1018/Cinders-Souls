using System;using System.Threading.Tasks;using UnityEngine;using Codice.Characters;using Codice.Presentation;using Codice.UI;
public static class VerifySupportGrip {
 public static async Task<string> Run(){
 var p=RunHUD.Instance.Player;var ap=p.GetComponent<ActorPresentation>();var mode=p.GetComponent<HumanPreviewCamera>();
 p.State.Initialize(PlayerClass.Warrior);await Task.Delay(200);p.enabled=false;ap.enabled=false;mode.enabled=true;
 var cc=p.GetComponent<CharacterController>();var saved=p.transform.position;var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(800,99.5f,800);floor.transform.localScale=new Vector3(20,1,20);
 cc.enabled=false;p.transform.position=new Vector3(800,100.05f,800);cc.enabled=true;Physics.SyncTransforms();for(int i=0;i<20;i++)p.SimulateMotor(Vector3.zero,true,1f/60);
 try {
 if(!mode.ThirdPerson)mode.Toggle();p.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Sword){katana=true},0);p.State.SelectWeapon(0);mode.RefreshBody();await Task.Delay(200);
 if(!mode.HeldWeapon)throw new Exception("No third-person held weapon; mode="+mode.ThirdPerson+" enabled="+mode.enabled);
 var a=ap.Body;a.speed=0;a.Play("KatanaSlashA",0,.36f);a.Update(0);await Task.Delay(250);
 var left=HumanoidWeaponGrip.Socket(a,true);float error=Vector3.Distance(left.position,mode.HeldWeapon.TransformPoint(new Vector3(0,-.11f,0)));float weight=a.GetComponent<HumanoidSupportHand>().SupportWeight;
 var upper=a.GetBoneTransform(HumanBodyBones.LeftUpperArm);var lower=a.GetBoneTransform(HumanBodyBones.LeftLowerArm);var hand=a.GetBoneTransform(HumanBodyBones.LeftHand);string result="support="+weight+" grip error="+error+" reach="+(Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position))+" targetDistance="+Vector3.Distance(upper.position,mode.HeldWeapon.TransformPoint(new Vector3(0,-.11f,0)))+" hand="+hand.position+" grip="+left.position;Debug.Log(result);if(weight<.9f||error>.03f)throw new Exception(result);return "PASS "+result;
 }finally{ap.Body.speed=1;ap.enabled=true;p.enabled=true;cc.enabled=false;p.transform.position=saved;cc.enabled=true;UnityEngine.Object.Destroy(floor);}
 }
}


