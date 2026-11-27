using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;
public static class BuildSwordCombo
{
 public static string Run(){var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Presentation/Human/HumanPlayer.controller");var sm=controller.layers[0].stateMachine;
 for(int i=1;i<=3;i++){string name="SwordCombo"+i;string source="Assets/03_Models/Mixamo/SwordAndShield/"+(i==1?"sword and shield attack.fbx":i==2?"sword and shield slash (2).fbx":"sword and shield slash (3).fbx");var clip=Object.Instantiate(AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__")));clip.name=name;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);string path="Assets/Resources/Presentation/Human/"+name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(old){EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);clip=old;}else AssetDatabase.CreateAsset(clip,path);var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??sm.AddState(name);state.motion=clip;state.speedParameter="ActionSpeed";state.speedParameterActive=true;}
 EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "Three distinct sword clips installed";}
}
