using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;
public static class BuildWalkMotion{
 public static string Run(){
 const string root="Assets/Resources/Presentation/Human/";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"HumanPlayer.controller");
 var tree=new BlendTree{name="Walk eight directions",blendType=BlendTreeType.FreeformDirectional2D,blendParameter="MoveX",blendParameterY="MoveY",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);
 foreach(var spec in new[]{("Forward",new Vector2(0,1)),("Backward",new Vector2(0,-1)),("Left",new Vector2(-1,0)),("Right",new Vector2(1,0)),("ForwardLeft",new Vector2(-.707f,.707f)),("ForwardRight",new Vector2(.707f,.707f)),("BackwardLeft",new Vector2(-.707f,-.707f)),("BackwardRight",new Vector2(.707f,-.707f))}){
 var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_"+spec.Item1+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var clip=Object.Instantiate(source);clip.name="Walk"+spec.Item1;
 var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=true;settings.loopBlendOrientation=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
 foreach(var b in AnimationUtility.GetCurveBindings(clip))if(b.propertyName=="RootT.x"||b.propertyName=="RootT.z"){var c=AnimationUtility.GetEditorCurve(clip,b);AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,c.Evaluate(0)));}
 var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(root+clip.name+".anim");if(old){EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);clip=old;}else AssetDatabase.CreateAsset(clip,root+clip.name+".anim");tree.AddChild(clip,spec.Item2);
 }
 var sm=controller.layers[0].stateMachine;var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Walk")??sm.AddState("Walk");state.motion=tree;state.speedParameter="LocomotionRate";state.speedParameterActive=true;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return "PASS eight directional walking clips and Walk state";
 }
}
