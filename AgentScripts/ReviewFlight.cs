using System;
using System.Threading.Tasks;
using UnityEngine;
using Codice.Presentation;
using Codice.UI;
public static class ReviewFlight
{
 public static async Task<string> Run()
 {
  RunHUD.Instance.SetPaused(false);var p=RunHUD.Instance.Player;var c=p.GetComponent<HumanPreviewCamera>();if(c.ThirdPerson)c.Toggle();p.enabled=false;p.view.transform.localRotation=Quaternion.identity;Time.timeScale=1;
  var roots=new GameObject[4];for(int i=0;i<4;i++){roots[i]=new GameObject("Review projectile "+i);roots[i].transform.position=p.transform.position+new Vector3((i-1.5f)*1.5f,1.3f,4);SpellFlightVisual.Add(roots[i],(ProjectileStyle)i);}
  for(int n=0;n<15;n++){foreach(var root in roots)root.transform.position+=Vector3.forward*.025f;await Task.Delay(25);}
  Time.timeScale=0;
  foreach(var root in roots){if(root.GetComponentsInChildren<LineRenderer>().Length<2||root.GetComponent<MeshRenderer>())throw new Exception("Primitive projectile regression");}
  Debug.Log("[Upgrade validation] Four differentiated flight visuals; no sphere meshes");return "Flight visual samples ready";
 }
}
