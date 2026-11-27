using UnityEngine;
namespace Codice.Presentation {
 // Ground only the retargeted, non-jumping executioner visual; navigation remains authoritative.
 [DefaultExecutionOrder(180)]
 public sealed class CemeteryFootContact:MonoBehaviour {
  Animator body;Codice.Enemies.BasicEnemy enemy;Transform left,right;Vector3 rest;
  void Start(){enemy=GetComponent<Codice.Enemies.BasicEnemy>();body=GetComponentInChildren<Animator>();if(!body||!body.isHuman){enabled=false;return;}left=body.GetBoneTransform(HumanBodyBones.LeftFoot);right=body.GetBoneTransform(HumanBodyBones.RightFoot);rest=body.transform.localPosition;}
  void LateUpdate(){if(!enemy||!enemy.IsAlive||!left||!right)return;body.transform.localPosition=rest;float bottom=Mathf.Min(left.position.y,right.position.y);float correction=transform.position.y+.09f-bottom;body.transform.position+=Vector3.up*Mathf.Clamp(correction,-1.5f,1.5f);}
 }
}
