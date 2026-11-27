using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using Codice.Characters;using Codice.Presentation;using Codice.Mechanics;using Codice.UI;
public static class PoseSheet
{
 public static async Task<string> Run(){
 if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Descenso")UnityEngine.SceneManagement.SceneManager.LoadScene("Descenso");for(int i=0;i<100&&(!RunHUD.Instance||!RunHUD.Instance.Player);i++)await Task.Delay(100);await Task.Delay(300);
 var p=RunHUD.Instance.Player;p.SetNarrativeControlsBlocked(false);RunHUD.Instance.SetPaused(false);p.State.Initialize(PlayerClass.Warrior);await Task.Delay(200);
 var ap=p.GetComponent<ActorPresentation>();var combat=p.GetComponent<PlayerCombat>();var mode=p.GetComponent<HumanPreviewCamera>();if(!mode.ThirdPerson)mode.Toggle();p.enabled=false;ap.enabled=false;combat.enabled=false;mode.enabled=false;p.GetComponent<ThirdPersonPresentation>().enabled=false;
 var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.SetPositionAndRotation(new Vector3(1000,1.04f,1000),Quaternion.identity);cc.enabled=true;
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Review floor";floor.transform.position=new Vector3(1000,.5f,1000);floor.transform.localScale=new Vector3(8,1,8);Physics.SyncTransforms();p.SimulateMotor(Vector3.zero,true,.02f);
 var light=new GameObject("Review light").AddComponent<Light>();light.type=LightType.Point;light.range=10;light.intensity=9;light.transform.position=p.transform.position+new Vector3(2,2,3);
 var camera=new GameObject("Review camera").AddComponent<Camera>();camera.transform.position=p.transform.position+new Vector3(2,1.5f,3);camera.transform.LookAt(p.transform.position+Vector3.up*.9f);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.21f);camera.farClipPlane=12;camera.nearClipPlane=.05f;
 var specs=new[]{("SwordCombo1",WeaponKind.Sword),("AxeAttack",WeaponKind.Axe),("ShootBow",WeaponKind.Bow),("DualSlice",WeaponKind.Sword),("KatanaSlashA",WeaponKind.Sword),("Thrust",WeaponKind.Polearm),("StaffCast",WeaponKind.Staff),("Heal",WeaponKind.Sword),("Walk",WeaponKind.Sword),("Movement",WeaponKind.Sword),("ThrowShuriken",WeaponKind.Bow),("BlockIdle",WeaponKind.Sword)};
 var sheet=new Texture2D(1440,1200,TextureFormat.RGB24,false);var rt=new RenderTexture(360,400,24);var tile=new Texture2D(360,400,TextureFormat.RGB24,false);camera.targetTexture=rt;var previous=RenderTexture.active;
 for(int i=0;i<specs.Length;i++){
  var spec=specs[i];var weapon=new WeaponInstance(spec.Item2);weapon.katana=i==4;weapon.shuriken=i==10;p.State.EquipIntoSlot(weapon,0);p.State.EquipIntoSlot(new WeaponInstance(i==3?WeaponKind.Sword:WeaponKind.Axe),1);p.State.SelectWeapon(0);combat.GetType().GetMethod("UpdateWeapon",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(combat,new object[]{weapon});mode.GetType().GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(mode,null);await Task.Delay(80);
  var body=ap.Body;body.speed=0;body.SetLayerWeight(1,0);body.SetFloat("MoveX",0);body.SetFloat("MoveY",1);body.SetFloat("LocomotionRate",1);body.Play(spec.Item1,0,.36f);if(i==3)body.Play("DualSlice",1,.36f);body.Update(0);p.GetComponent<HumanGroundClearance>().Resolve();
  if(i==7){p.State.Damage(50,true);p.GetComponent<EstusFlask>().TryDrink();body.speed=1;await Task.Delay(430);body.speed=0;body.Update(0);}
  await Task.Delay(100);mode.GetType().GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(mode,null);camera.Render();RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,360,400),0,0);tile.Apply();sheet.SetPixels(i%4*360,(2-i/4)*400,360,400,tile.GetPixels());if(i==7)p.GetComponent<EstusFlask>().Interrupt();
 }
 sheet.Apply();System.IO.File.WriteAllBytes("Temp/all-third-person-poses.png",sheet.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tile);UnityEngine.Object.Destroy(sheet);return "Temp/all-third-person-poses.png";
 }
}



