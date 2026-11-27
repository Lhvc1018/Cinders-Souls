using UnityEngine;
using Codice.Characters;
namespace Codice.Presentation {
 [DefaultExecutionOrder(220)] public sealed class BowArrowPresentation:MonoBehaviour {
  GameObject arrow;PlayerCharacter player;
  void Awake(){player=GetComponent<PlayerCharacter>();}
  void LateUpdate(){
   var mode=GetComponent<HumanPreviewCamera>();var weapon=player.State.ActiveWeapon;var pose=GetComponent<ActorPresentation>();
   bool show=mode&&mode.ThirdPerson&&mode.HeldWeapon&&weapon!=null&&weapon.kind==WeaponKind.Bow&&!weapon.shuriken&&(weapon.ammunition>0||GetComponent<PlayerCombat>().IsSwinging)&&player.State.IsAlive&&!player.IsDashing;
   if(show&&pose.Body){var info=pose.Body.GetCurrentAnimatorStateInfo(0);if(GetComponent<PlayerCombat>().IsSwinging&&info.normalizedTime%1>=.65f)show=false;}
   if(!arrow&&show){var prefab=Resources.Load<GameObject>("PlayerArrow");if(prefab){arrow=Instantiate(prefab,transform);arrow.name="Third person nocked arrow";foreach(var c in arrow.GetComponentsInChildren<Collider>())Destroy(c);}}
   if(!arrow)return;arrow.SetActive(show);if(!show)return;
   Vector3 center=mode.HeldWeapon.position+Vector3.up*.08f;
   arrow.transform.position=center;arrow.transform.rotation=Quaternion.LookRotation(HumanPreviewCamera.AimFrom(player,center));
   foreach(var r in arrow.GetComponentsInChildren<Renderer>()){r.enabled=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
  }
 }
}
