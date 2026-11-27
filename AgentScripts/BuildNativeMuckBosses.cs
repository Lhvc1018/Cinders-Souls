using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Codice.Bosses;
using Codice.Mechanics;
public static class BuildNativeMuckBosses {
 const string Dir="Assets/Resources/ExpeditionEnemies/";
 static AnimationClip Clip(string source,string name,bool loop){
  var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(source);
  if(!original||AnimationUtility.GetCurveBindings(original).Length==0)throw new Exception("Missing curves: "+source);
  string path=Dir+"MuckNative/"+name+".anim";
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
  if(!clip){clip=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(clip,path);}else EditorUtility.CopySerialized(original,clip);
  clip.name=name.Substring(name.IndexOf('_')+1);clip.legacy=false;
  AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
  EditorUtility.SetDirty(clip);return clip;
 }
 static void ConvertMaterials(GameObject model){
  foreach(var m in model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct()){
   if(m.shader&&m.shader.name=="Universal Render Pipeline/Lit")continue;
   var so=new SerializedObject(m);Color color=Color.white;Texture tex=null;
   var colors=so.FindProperty("m_SavedProperties.m_Colors");
   for(int i=0;i<colors.arraySize;i++){var e=colors.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("first").stringValue=="_Color")color=e.FindPropertyRelative("second").colorValue;}
   var textures=so.FindProperty("m_SavedProperties.m_TexEnvs");
   for(int i=0;i<textures.arraySize;i++){var e=textures.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("first").stringValue=="_MainTex")tex=e.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;}
   m.shader=Shader.Find("Universal Render Pipeline/Lit");m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",tex);m.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(m);
  }
 }
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Stop play mode");
  AssetDatabase.Refresh();System.IO.Directory.CreateDirectory(Dir+"MuckNative");AssetDatabase.Refresh();
  string[] sources={"Gronk","Chief","Guardian"};
  string[] old={"Custodio de las dos hojas","Lancero de ceniza","Guardian de obsidiana"};
  string[] names={"Duende Mutante","Lanza del Infierno","Gurdi del Abismo"};
  string[][] clips={
   new[]{"Idle_hogoblin","Run_hogoblin","Attack1_hogblin","Ranged_Hogoblin","Ranged_Hogoblin1"},
   new[]{"Chief_Idle","Run_Chief","Chief_Melee","Chief_Ranged","Chief_JumpSlam","Chief_MeleeSpin"},
   new[]{"GuardianIdle","GuardianWalk","GuardianMelee","GuardianLaser","GuardianRanged2"}
  };
  var report=new List<string>();
  for(int n=0;n<3;n++){
   string path=Dir+names[n]+".prefab";string previous=Dir+old[n]+".prefab";
   if(AssetDatabase.LoadAssetAtPath<GameObject>(previous)){var error=AssetDatabase.MoveAsset(previous,path);if(error!="")throw new Exception(error);}
   var root=PrefabUtility.LoadPrefabContents(path);
   try{
    root.name=names[n];root.transform.localScale=Vector3.one;
    foreach(Transform child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
    var pivot=new GameObject("Native visual");pivot.transform.SetParent(root.transform,false);
    var native=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Research/MuckNative/GameObject/"+sources[n]+".prefab"),pivot.transform);
    native.name=sources[n];native.transform.localPosition=Vector3.zero;native.transform.localRotation=Quaternion.identity;native.transform.localScale=Vector3.one;
    ConvertMaterials(native);
    string controllerPath=Dir+"MuckNative/"+sources[n]+".controller";
    var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
    if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
    foreach(var state in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(state.state);
    controller.parameters=new[]{new AnimatorControllerParameter{name="ActionSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1},new AnimatorControllerParameter{name="LocomotionSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1}};
    string[] states={"Idle","Chase","Melee","Shoot","Slam","Spin"};
    AnimationClip idle=null;
    for(int c=0;c<clips[n].Length;c++){
     var clip=Clip("Assets/Research/MuckAnimations/"+sources[n]+"/"+clips[n][c]+".anim",sources[n]+"_"+states[c],c<2);
     if(c==0)idle=clip;
     var s=controller.layers[0].stateMachine.AddState(states[c]);s.motion=clip;
     if(c>0){s.speedParameterActive=true;s.speedParameter=c==1?"LocomotionSpeed":"ActionSpeed";}
     if(c==0)controller.layers[0].stateMachine.defaultState=s;
    }
    foreach(string state in new[]{"Run","Hit","Stun","Death"}){
     var s=controller.layers[0].stateMachine.AddState(state);
     s.motion=state=="Run"?controller.layers[0].stateMachine.states.First(x=>x.state.name=="Chase").state.motion:idle;
     if(state=="Run"){s.speedParameterActive=true;s.speedParameter="LocomotionSpeed";}else s.speed=0;
    }
    var animator=native.GetComponentInChildren<Animator>(true);animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    idle.SampleAnimation(animator.gameObject,0);
    var skins=native.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    Bounds bounds=new Bounds();bool first=true;
    foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);foreach(var v in mesh.vertices){var w=skin.transform.TransformPoint(v);if(first){bounds=new Bounds(w,Vector3.zero);first=false;}else bounds.Encapsulate(w);}UnityEngine.Object.DestroyImmediate(mesh);skin.updateWhenOffscreen=true;}
    if(first||bounds.size.y<.1f)throw new Exception("Invalid native mesh "+sources[n]);
    float desired=n==2?3.8f:n==1?2.4f:3.2f;float scale=desired/bounds.size.y;pivot.transform.localScale=Vector3.one*scale;
    pivot.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
    var boss=root.GetComponent<BossCharacter>();boss.visual=pivot.transform;boss.useNativeAnimator=true;boss.meleeWeapon=null;boss.bowWeapon=null;boss.baseHealth=n==2?1700:1400;boss.summons=new Codice.Enemies.BasicEnemy[0];boss.restrictToMelee=false;boss.bloodInquisitor=false;
    var capsule=root.GetComponent<CapsuleCollider>();capsule.height=desired;capsule.radius=n==2?.85f:n==1?.5f:.65f;capsule.center=Vector3.up*(desired*.5f);
    var agent=root.GetComponent<NavMeshAgent>();agent.height=desired;agent.radius=capsule.radius;agent.baseOffset=0;agent.stoppingDistance=2.8f;agent.acceleration=22;
    PrefabUtility.SaveAsPrefabAsset(root,path);
    report.Add(names[n]+" native="+sources[n]+" height="+desired+"m clips="+clips[n].Length+" scale="+scale);
   } finally {PrefabUtility.UnloadPrefabContents(root);}
  }
  foreach(var lab in UnityEngine.Object.FindObjectsByType<CombatTestRoom>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   lab.enemyPrefabs=AssetDatabase.FindAssets("t:Prefab",new[]{Dir.TrimEnd('/')}).Select(g=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).Where(p=>p&&(p.GetComponent<BossCharacter>()||p.GetComponent<Codice.Enemies.BasicEnemy>())).ToArray();EditorUtility.SetDirty(lab);
  }
  EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();return string.Join("\n",report);
 }
}

