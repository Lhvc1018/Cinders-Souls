using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Codice.Characters;using Codice.UI;
public static class VerifyMotorCollisions
{
 static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 public static string Run()
 {
  var p=RunHUD.Instance.Player;var c=p.GetComponent<CharacterController>();var combat=p.GetComponent<PlayerCombat>();var original=p.transform.position;p.enabled=false;combat.enabled=false;
  var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(600,99.5f,600);floor.transform.localScale=new Vector3(20,1,20);
  var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(600,102.2f,600);ceiling.transform.localScale=new Vector3(4,.4f,4);
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(600,101,601.4f);wall.transform.localScale=new Vector3(5,2,.4f);
  try{c.enabled=false;p.transform.position=new Vector3(600,100.05f,600);c.enabled=true;Physics.SyncTransforms();for(int i=0;i<20;i++)p.SimulateMotor(Vector3.zero,true,1f/60);float y=p.transform.position.y;Check(p.TryJump(),"ceiling jump rejected");float peak=y;for(int i=0;i<90;i++){p.SimulateMotor(Vector3.zero,true,1f/60);peak=Mathf.Max(peak,p.transform.position.y);}Check(peak-y<.3f&&p.Grounded,"Head collision failed");typeof(PlayerCharacter).GetField("nextDash",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(p,0f);Check(p.TryDash(Vector3.forward),"Wall dodge rejected");for(int i=0;i<30;i++)p.SimulateMotor(Vector3.zero,true,1f/60);Check(p.transform.position.z<600.95f,"Dodge crossed wall");string result=$"PASS head collision height={peak-y:F3}, dodge wall z={p.transform.position.z:F3}";Debug.Log(result);return result;}
  finally{UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(ceiling);UnityEngine.Object.Destroy(wall);c.enabled=false;p.transform.position=original;c.enabled=true;p.enabled=true;combat.enabled=true;}
 }
 public static async Task<string> MainLevel()
 {
  UnityEngine.SceneManagement.SceneManager.LoadScene("Calabozo");await Task.Delay(2000);
  var p=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();Check(p&&p.view,"Main level player missing");
  Check(RenderSettings.ambientMode==AmbientMode.Trilight,"Main ambient not shared");Check(p.view.GetComponentsInChildren<Light>().Any(x=>x.name=="Camera visibility fill"),"Main fill missing");
  var v=UnityEngine.Object.FindObjectsByType<Volume>().First(x=>x.name=="Main level visibility grade");Check(v.profile.TryGet<ColorAdjustments>(out var grade)&&Mathf.Approximately(grade.postExposure.value,.6f),"Main exposure mismatch");Check(v.profile.TryGet<Tonemapping>(out var tone)&&tone.mode.value==TonemappingMode.ACES,"Main ACES missing");Debug.Log("PASS Calabozo shared ambient, camera fill, exposure +0.6 EV and ACES");return "PASS Calabozo shared ambient, camera fill, exposure +0.6 EV and ACES";
 }
}

