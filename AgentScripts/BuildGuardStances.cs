using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class BuildGuardStances{
 const string Root="Assets/Resources/Presentation/Human/";
 public static string Run(){
 var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"HumanPlayer.controller");
 string path="Assets/07_Animacion/DoubleL/FBX Unity/One Hand Up/Sheild/Idle/1Hand_Up_Shield_Block_Idle_1.fbx";
 var importer=(ModelImporter)AssetImporter.GetAtPath(path);if(importer.animationType!=ModelImporterAnimationType.Human){importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();}
 var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(x=>!x.name.StartsWith("__"));if(!source.isHumanMotion)throw new Exception("Shield guard not humanoid");
 var clip=UnityEngine.Object.Instantiate(source);clip.name="BlockIdle";var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"BlockIdle.anim");EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(old);var block=c.layers[0].stateMachine.states.First(s=>s.state.name=="BlockIdle").state;block.motion=old;block.speedParameterActive=false;EditorUtility.SetDirty(block);
 if(!c.layers.Any(l=>l.name=="Weapon carry"))c.AddLayer("Weapon carry");var layers=c.layers;int index=Array.FindIndex(layers,l=>l.name=="Weapon carry");var layer=layers[index];
 var mask=layer.avatarMask;if(!mask){mask=new AvatarMask{name="Native carry arms"};AssetDatabase.AddObjectToAsset(mask,c);}for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);foreach(var part in new[]{AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);EditorUtility.SetDirty(mask);layer.avatarMask=mask;layer.defaultWeight=0;
 foreach(string name in new[]{"SwordIdle","KatanaIdle","SpearIdle","BowIdle","StaffIdle","DualIdle","BlockIdle"}){var s=layer.stateMachine.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??layer.stateMachine.AddState(name);s.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+name+".anim");s.speed=1;s.speedParameterActive=false;EditorUtility.SetDirty(s);}
 layers[index]=layer;c.layers=layers;EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();return "PASS authored shield guard and Human Melee arms-only carry layer";
 }
}
