using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Codice.Expedition;
using Codice.Characters;
using Codice.UI;
public static class ReviewUpgrade
{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 public static async Task<string> Run()
 {
  ExpeditionFloor f=null;for(int i=0;i<150;i++){f=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();if(f&&RunHUD.Instance&&RunHUD.Instance.Player)break;await Task.Delay(100);}
  Check(f&&RunHUD.Instance,"Runtime not ready");await Task.Delay(1000);var p=f.context.player;p.SetNarrativeControlsBlocked(false);RunHUD.Instance.SetPaused(false);
  var wall=UnityEngine.Object.FindAnyObjectByType<SecretWall>();Check(wall&&!f.SecretRevealed,"Secret starts closed");
  wall.ReceiveDamage(new DamageInfo{amount=20,source=p.gameObject});Check(!f.SecretRevealed,"First hit preserves wall");wall.ReceiveDamage(new DamageInfo{amount=20,source=p.gameObject});Check(f.SecretRevealed&&!wall.gameObject.activeSelf,"Second hit opens wall");
  var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=f.Center(f.Layout.SecretRoom)+Vector3.up*.1f;cc.enabled=true;
  Check(f.EnterRoom(f.Layout.SecretRoom)||f.Cleared[f.Layout.SecretRoom],"Secret enter");Check(f.Chests[f.Layout.SecretRoom]&&f.Chests[f.Layout.SecretRoom].rewardTier==3&&!f.InEncounter,"Secret reward");
  var original=p.State.GetWeapon(0);var pickup=p.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance(WeaponKind.Axe,2),p.transform.position+Vector3.forward);
  RunHUD.Instance.OpenWeaponChoice(pickup);Check(RunHUD.Instance.WeaponSelectionVisible&&RunHUD.Instance.Paused,"Comparison opens");
  typeof(RunHUD).GetMethod("ConfirmWeapon",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(RunHUD.Instance,new object[]{0});
  Check(p.State.GetWeapon(0).kind==WeaponKind.Axe&&pickup.weapon==original&&!RunHUD.Instance.Paused,"Exact-slot swap preserves old weapon");
  RunHUD.Instance.SetPaused(true);await Task.Delay(100);Check(RunHUD.Instance.Paused&&Time.timeScale==0,"Inventory pauses solo");RunHUD.Instance.SetPaused(false);
  cc.enabled=false;p.transform.SetPositionAndRotation(new Vector3(0,.1f,-3),Quaternion.identity);cc.enabled=true;p.view.transform.localRotation=Quaternion.identity;
  var presentation=p.GetComponent<Codice.Presentation.ActorPresentation>();Check(presentation.Body&&presentation.Body.isHuman,"New humanoid active");
  foreach(var state in new[]{"Movement","Melee","BlockIdle","Dash","Death"})Check(presentation.Body.HasState(0,Animator.StringToHash(state)),"Animation "+state);
  foreach(var name in new[]{"Fire","Lightning"})foreach(var renderer in Resources.Load<GameObject>("CindersVfx/"+name).GetComponentsInChildren<Renderer>(true))foreach(var mat in renderer.sharedMaterials)Check(mat&&mat.shader.isSupported&&mat.shader.name=="Cinders/Particles URP","VFX shader "+name);
  var fx=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("CindersVfx/Fire"),p.transform.position+Vector3.forward*4+Vector3.up,Quaternion.identity);fx.transform.localScale=Vector3.one*.8f;
  Debug.Log("[Upgrade validation] secret break/reward, weapon swap, inventory pause, humanoid states and URP materials passed");
  return "Secret, loot, inventory, swap, humanoid, VFX passed";
 }
}
