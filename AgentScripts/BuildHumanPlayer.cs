using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildHumanPlayer
{
 const string Root="Assets/Resources/Presentation/Human";
 const string Mix="Assets/03_Models/Mixamo/SwordAndShield/";
 static AnimationClip Clip(string path,string name,bool loop=false)
 {
   var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));
   if(!source)throw new Exception("Missing clip "+path);
   var clip=UnityEngine.Object.Instantiate(source);clip.name=name;
   var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
   string dest=Root+"/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);return existing;}AssetDatabase.CreateAsset(clip,dest);return clip;
 }
 public static string Build()
 {
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();
   foreach(var path in Directory.GetFiles("Assets/03_Models/Mixamo","*.fbx",SearchOption.AllDirectories))
   {
     var importer=(ModelImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));if(importer.animationType==ModelImporterAnimationType.Human)continue;
     importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();
   }
   var humanPath="Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx";
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(humanPath);
   var avatar=AssetDatabase.LoadAllAssetsAtPath(humanPath).OfType<Avatar>().FirstOrDefault();if(!avatar||!avatar.isValid||!avatar.isHuman)throw new Exception("Human avatar invalid");
   var idle=Clip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx","Idle",true);
   var runPath=Directory.GetFiles("Assets/Kevin Iglesias/Human Animations/Animations/Male","HumanM@Run01_Forward.fbx",SearchOption.AllDirectories).First().Replace('\\','/');
   var move=Clip(runPath,"Movement",true);var ready=Clip(Mix+"sword and shield idle.fbx","Ready",true);
   var attack=Clip(Mix+"sword and shield attack.fbx","Melee");var block=Clip(Mix+"sword and shield block idle.fbx","BlockIdle",true);
   var hit=Clip(Mix+"sword and shield impact.fbx","Hit");var death=Clip(Mix+"sword and shield death.fbx","Death");var dash=Clip("Assets/03_Models/Mixamo/QuickRoll.fbx","Dash");
   var heavy=Clip(Mix+"sword and shield slash.fbx","HeavyAttack");var cast=Clip(Mix+"sword and shield casting.fbx","ShootMagic");
   var bow=Clip("Assets/07_Animacion/DoubleL/FBX Unity/Bow/Attack A/Bow_Attack_A_1_All.fbx","ShootBow");
   var bowIdle=Clip("Assets/07_Animacion/DoubleL/FBX Unity/Bow/Movement/Idle/Idle/Bow_Idle_B.fbx","BowIdle",true);
   string controllerPath=Root+"/HumanPlayer.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
   if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
   var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
   if(!controller.parameters.Any(p=>p.name=="ActionSpeed"))controller.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);
   var library=Codice.Presentation.PresentationLibrary.Load();var old=library.medium as AnimatorController;
   var names=old.layers[0].stateMachine.states.Select(s=>s.state.name).Concat(new[]{"Idle","Ready","Movement","Dash","BlockIdle","Melee","Hit","Death"}).Distinct();
   foreach(var name in names){var state=machine.AddState(name);state.motion=name=="ShootBow"?bow:name=="BowIdle"?bowIdle:name=="Death"?death:name=="Hit"?hit:name=="Dash"?dash:name=="Movement"?move:name.Contains("Block")||name.Contains("Guard")?block:name.Contains("Heavy")?heavy:name.Contains("Cast")||name.Contains("Shoot")?cast:name=="Idle"?idle:name.Contains("Idle")||name=="Ready"?ready:attack;state.speedParameter="ActionSpeed";state.speedParameterActive=state.motion!=idle&&state.motion!=move&&state.motion!=ready&&state.motion!=block;if(name=="Idle")machine.defaultState=state;}
   var go=UnityEngine.Object.Instantiate(model);go.name="Human pilgrim prototype";var animator=go.GetComponent<Animator>();animator.avatar=avatar;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
   // Retain the complete humanoid skeleton; only first-person mesh triangles are trimmed.
   foreach(var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
   {
     var mesh=UnityEngine.Object.Instantiate(renderer.sharedMesh);var weights=mesh.boneWeights;
     var right=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var left=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
     bool Arm(int bone)=>renderer.bones[bone]&&(renderer.bones[bone].IsChildOf(right)||renderer.bones[bone].IsChildOf(left));
     bool Keep(int vertex){var w=weights[vertex];return (Arm(w.boneIndex0)?w.weight0:0)+(Arm(w.boneIndex1)?w.weight1:0)+(Arm(w.boneIndex2)?w.weight2:0)+(Arm(w.boneIndex3)?w.weight3:0)>.7f;}
     for(int sub=0;sub<mesh.subMeshCount;sub++){var triangles=mesh.GetTriangles(sub);var kept=new System.Collections.Generic.List<int>();for(int i=0;i<triangles.Length;i+=3)if(Keep(triangles[i])&&Keep(triangles[i+1])&&Keep(triangles[i+2]))kept.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});mesh.SetTriangles(kept,sub);}
     string meshPath=Root+"/"+renderer.name+"_Arms.asset";var oldMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(oldMesh){EditorUtility.CopySerialized(mesh,oldMesh);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,meshPath);
   }
   foreach(var pair in new[]{(HumanBodyBones.RightHand,"handslot.r"),(HumanBodyBones.LeftHand,"handslot.l")}){var socket=new GameObject(pair.Item2).transform;socket.SetParent(animator.GetBoneTransform(pair.Item1),false);}
   PrefabUtility.SaveAsPrefabAsset(go,Root+"/HumanPlayer.prefab");UnityEngine.Object.DestroyImmediate(go);AssetDatabase.SaveAssets();return "Humanoid avatar, 10 clips, controller, arms meshes and hand sockets saved";
 }
}

