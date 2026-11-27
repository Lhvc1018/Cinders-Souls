using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;
using UnityEngine;using UnityEngine.Rendering.Universal;using Codice.Characters;using Codice.Mechanics;using Codice.Presentation;using Codice.UI;
public static class VerifyMotionLootLight
{
 static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 public static async Task<string> Run()
 {
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Descenso")UnityEngine.SceneManagement.SceneManager.LoadScene("Descenso");
  for(int i=0;i<100&&(!RunHUD.Instance||!RunHUD.Instance.Player);i++)await Task.Delay(100);
  await Task.Delay(600);var p=RunHUD.Instance.Player;RunHUD.Instance.SetPaused(false);p.SetNarrativeControlsBlocked(false);p.State.Initialize(PlayerClass.Warrior);
  p.enabled=false;var combat=p.GetComponent<PlayerCombat>();combat.enabled=false;
  var controller=p.GetComponent<CharacterController>();var presentation=p.GetComponent<ActorPresentation>();presentation.enabled=false;
  var original=p.transform.position;var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Temporary motor test floor";platform.transform.position=new Vector3(500,99.5f,500);platform.transform.localScale=new Vector3(20,1,20);
  controller.enabled=false;p.transform.position=new Vector3(500,100.1f,500);controller.enabled=true;Physics.SyncTransforms();
  try
  {
   for(int i=0;i<30;i++)p.SimulateMotor(Vector3.zero,true,1f/60);
   Check(p.Grounded,"Ground probe failed");float start=p.transform.position.y;Check(p.TryJump(),"Jump rejected");float peak=start;
   for(int i=0;i<120;i++){p.SimulateMotor(Vector3.zero,true,1f/60);peak=Mathf.Max(peak,p.transform.position.y);}
   Check(peak-start>1.1f&&peak-start<1.4f,"Jump height "+(peak-start));Check(p.Grounded&&Mathf.Abs(p.transform.position.y-start)<.06f,"Landing drift");
   Check(p.TryJump(),"Second jump rejected");float shortPeak=start;for(int i=0;i<120;i++){p.SimulateMotor(Vector3.zero,false,1f/60);shortPeak=Mathf.Max(shortPeak,p.transform.position.y);}Check(shortPeak<peak-.3f,"Release did not shorten jump");
   Set(p,"nextDash",0f);Check(p.TryDash(Vector3.forward),"Dodge rejected");Check(!p.DodgeInvulnerable,"Startup incorrectly invulnerable");await Task.Delay(100);Check(p.DodgeInvulnerable,"Missing dodge i-frames");await Task.Delay(240);Check(!p.DodgeInvulnerable,"Recovery incorrectly invulnerable");await Task.Delay(150);
   var body=presentation.Body;body.speed=1;body.Play("Ready",0,0);body.Update(0);
   var pairs=new[]{(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg),(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot),(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm)};
   float[] lengths=pairs.Select(pair=>Vector3.Distance(body.GetBoneTransform(pair.Item1).position,body.GetBoneTransform(pair.Item2).position)).ToArray();float maxRatio=1;float minFoot=1000;
   for(int frame=0;frame<=20;frame++){
    body.Play("Dash",0,frame/20f);body.Update(0);p.GetComponent<HumanGroundClearance>().Resolve();
    for(int j=0;j<pairs.Length;j++){float length=Vector3.Distance(body.GetBoneTransform(pairs[j].Item1).position,body.GetBoneTransform(pairs[j].Item2).position);maxRatio=Mathf.Max(maxRatio,length/lengths[j]);}
    minFoot=Mathf.Min(minFoot,body.GetBoneTransform(HumanBodyBones.LeftFoot).position.y-start,body.GetBoneTransform(HumanBodyBones.RightFoot).position.y-start);
   }
   Check(maxRatio<1.15f,"Roll limb stretching "+maxRatio);Check(minFoot>=.05f,"Roll feet below capsule "+minFoot);
   Check(!body.applyRootMotion,"Animator moves capsule");foreach(string state in new[]{"JumpRise","Fall","Land"})Check(body.HasState(0,Animator.StringToHash(state)),"Missing "+state);
   controller.enabled=false;p.transform.position=original;controller.enabled=true;Physics.SyncTransforms();
   var chest=UnityEngine.Object.FindObjectsByType<TreasureChest>(FindObjectsSortMode.None).First(x=>!x.Opened);controller.enabled=false;p.transform.position=chest.transform.position+Vector3.forward*2;controller.enabled=true;p.State.AddGold(1000);
   chest.relicReward=false;Check(chest.TryOpen(p),"Chest rejected");var loot=UnityEngine.Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None).First(x=>x.Emerging);
   Check(Vector3.Distance(loot.transform.position,chest.LootOrigin)<.01f,"Loot not inside socket");var born=loot.transform.position;await Task.Delay(850);Check(!loot.Emerging&&loot.transform.position.y>born.y+.3f,"Loot reveal failed");Check(Vector2.Distance(new Vector2(born.x,born.z),new Vector2(loot.transform.position.x,loot.transform.position.z))<.01f,"Loot lateral drift");
   var fill=p.view.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name=="Camera visibility fill");Check(fill&&fill.intensity>0&&fill.range==7,"Missing fill light");Check(UniversalRenderPipeline.asset.supportsHDR,"Pipeline HDR disabled");Check(p.view.GetUniversalAdditionalCameraData().renderPostProcessing,"Postprocessing disabled");
   string result=$"PASS jump={peak-start:F3}m short={shortPeak-start:F3}m landing, i-frames, roll stretch={maxRatio:F3} minFoot={minFoot:F3}m, chest socket/reveal, HDR/fill";Debug.Log(result);return result;
  }
  finally{UnityEngine.Object.Destroy(platform);controller.enabled=false;p.transform.position=original;controller.enabled=true;p.enabled=true;combat.enabled=true;presentation.enabled=true;}
 }
}
