using System;using System.Threading.Tasks;using UnityEngine;using Codice.Characters;using Codice.Presentation;using Codice.UI;
public static class VerifyPoseRoot
{
 public static async Task<string> Run(){var p=RunHUD.Instance.Player;var ap=p.GetComponent<ActorPresentation>();var body=ap.Body;p.enabled=false;ap.enabled=false;p.GetComponent<PlayerCombat>().enabled=false;float max=0;
 foreach(string state in new[]{"Dash","JumpRise","SwordCombo1","AxeAttack","Conjure","StaffCast"})for(int i=0;i<=16;i++){body.Play(state,0,i/16f);body.Update(0);var hip=body.GetBoneTransform(HumanBodyBones.Hips);var offset=Vector3.ProjectOnPlane(hip.position-body.transform.position,Vector3.up).magnitude;max=Mathf.Max(max,offset);if(offset>1)throw new Exception(state+" pelvis escapes capsule: "+offset);}
 body.speed=1;ap.enabled=true;p.enabled=true;p.GetComponent<PlayerCombat>().enabled=true;await Task.Delay(150);Debug.Log("PASS six pose families remain attached to capsule; max pelvis planar offset="+max);return "PASS max pelvis offset="+max;}
}
