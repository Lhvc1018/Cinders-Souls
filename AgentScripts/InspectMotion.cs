using UnityEngine;
using Codice.Characters;
using Codice.Presentation;
public static class InspectMotion {
 public static string Run(){var p=Object.FindAnyObjectByType<PlayerCharacter>();if(!p)return "No player";var c=p.GetComponent<CharacterController>();var a=p.GetComponent<ActorPresentation>().Body;return $"scene={p.gameObject.scene.name} pos={p.transform.position} capsule center={c.center} h={c.height} radius={c.radius} grounded={c.isGrounded} skin={c.skinWidth} model={a.transform.localPosition} scale={a.transform.lossyScale} avatar={a.avatar.isValid}/{a.isHuman} rootmotion={a.applyRootMotion} feet={a.GetBoneTransform(HumanBodyBones.LeftFoot).position} clips="+string.Join(",",System.Array.ConvertAll(a.runtimeAnimatorController.animationClips,x=>x.name+":"+x.isHumanMotion+":"+x.length));}
}
