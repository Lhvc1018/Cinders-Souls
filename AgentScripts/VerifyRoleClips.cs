using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class VerifyRoleClips{
 public static string Run(){
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Presentation/Human/HumanPlayer.controller");var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();int count=0;
  foreach(string role in new[]{"Sword","Axe","Hammer","Spear","Katana","Bow","Staff","Dagger","Shield","Musket"})foreach(string action in new[]{"Idle","Walk","Run"}){
   var clip=states.Single(s=>s.name==role+action).motion as AnimationClip;if(!clip||!clip.isHumanMotion||!clip.isLooping||clip.length<.1f)throw new Exception("Invalid native role "+role+action);count++;
  }
  foreach(string name in new[]{"Walk","Movement"}){var tree=states.Single(s=>s.name==name).motion as BlendTree;if(!tree||tree.children.Length!=8)throw new Exception("Incomplete directional gait "+name);}
  foreach(string name in new[]{"ShootBow","StaffCast","Conjure","DaggerAttack","ShinobiGuard","ShinobiDeflect","ShinobiGuardHit"}){var clip=states.Single(s=>s.name==name).motion as AnimationClip;if(!clip||!clip.isHumanMotion||clip.length<.1f)throw new Exception("Invalid action "+name);}
  Debug.Log("PASS 30 native role clips, two eight-direction trees, seven bow/spell/guard actions");return "PASS 30 native role clips, two eight-direction trees, seven bow/spell/guard actions";
 }
}
