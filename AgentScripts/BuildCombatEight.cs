using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;
using Codice.Bosses;
using Codice.Presentation;
public static class BuildCombatEight {
 const string Dir="Assets/Resources/ExpeditionEnemies/";
 static Bounds BoundsOf(GameObject go){var b=new Bounds();bool first=true;var mesh=new Mesh();foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.BakeMesh(mesh);foreach(var v in mesh.vertices){var w=r.transform.TransformPoint(v);if(first){b=new Bounds(w,Vector3.zero);first=false;}else b.Encapsulate(w);}}UnityEngine.Object.DestroyImmediate(mesh);return b;}
 static void Materialize(GameObject go,string prefix){foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.sharedMaterials=r.sharedMaterials.Select((m,i)=>{if(!m)return m;string path=Dir+prefix+"_"+m.name.Replace("/","_")+".mat";var copy=AssetDatabase.LoadAssetAtPath<Material>(path);if(!copy){copy=new Material(m);copy.shader=Shader.Find("Universal Render Pipeline/Lit");copy.SetColor("_BaseColor",new Color(.29f,.31f,.32f));copy.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(copy,path);}return copy;}).ToArray();}}
 static void Death(Animator a,string label){
  var controller=a.runtimeAnimatorController as AnimatorController;if(!controller)return;
  var states=controller.layers[0].stateMachine;var idle=states.states.FirstOrDefault(s=>s.state.name=="Idle").state?.motion as AnimationClip;
  if(idle)idle.SampleAnimation(a.gameObject,0);
  string path=Dir+"Death_"+label+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
  if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.ClearCurves();clip.name="Death";
  var hips=a.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.IndexOf("hips",StringComparison.OrdinalIgnoreCase)>=0)??a.transform;
  string bone=AnimationUtility.CalculateTransformPath(hips,a.transform);var e=hips.localEulerAngles;var pos=hips.localPosition;
  clip.SetCurve(bone,typeof(Transform),"localEulerAnglesRaw.x",new AnimationCurve(new Keyframe(0,e.x),new Keyframe(.3f,e.x+12),new Keyframe(1.15f,e.x+82),new Keyframe(3.8f,e.x+82)));
  clip.SetCurve(bone,typeof(Transform),"m_LocalPosition.y",new AnimationCurve(new Keyframe(0,pos.y),new Keyframe(.5f,pos.y*.7f),new Keyframe(1.15f,pos.y*.18f),new Keyframe(3.8f,pos.y*.18f)));
  foreach(var t in hips.GetComponentsInChildren<Transform>().Where(t=>t.name.IndexOf("knee",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("elbow",StringComparison.OrdinalIgnoreCase)>=0)){var angles=t.localEulerAngles;clip.SetCurve(AnimationUtility.CalculateTransformPath(t,a.transform),typeof(Transform),"localEulerAnglesRaw.x",AnimationCurve.EaseInOut(0,angles.x,1.1f,angles.x+35));}
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
  var state=states.states.FirstOrDefault(s=>s.state.name=="Death").state??states.AddState("Death");state.motion=clip;state.speed=1;state.speedParameterActive=false;EditorUtility.SetDirty(controller);
 }
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Stop play");AssetDatabase.Refresh();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Research/MuckNative/GameObject/StoneTitan.prefab");if(!source)throw new Exception("Missing extracted StoneTitan");
  var root=new GameObject("Big Chunk");
  try{
   var pivot=new GameObject("Titan visual").transform;pivot.SetParent(root.transform,false);var model=UnityEngine.Object.Instantiate(source,pivot);model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);model.transform.localScale=Vector3.one;
   var a=model.GetComponentInChildren<Animator>(true);a.fireEvents=false;var original=(AnimatorController)a.runtimeAnimatorController;
   string path=Dir+"BigChunk.controller";var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path)??AnimatorController.CreateAnimatorControllerAtPath(path);
   foreach(var s in c.layers[0].stateMachine.states)c.layers[0].stateMachine.RemoveState(s.state);
   c.parameters=new[]{new AnimatorControllerParameter{name="ActionSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1},new AnimatorControllerParameter{name="LocomotionSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1}};
   string[] names={"Idle","Chase","Run","Melee","Slam","Shoot","Hit","Stun"},originals={"Idle","Walk","Walk","AttackSwipe","AttackSlam","AttackRanged","Idle","Idle"};
   for(int i=0;i<names.Length;i++){var clip=original.layers[0].stateMachine.states.First(s=>s.state.name==originals[i]).state.motion as AnimationClip;if(!clip||AnimationUtility.GetCurveBindings(clip).Length==0)throw new Exception("Titan missing curves "+originals[i]);var s=c.layers[0].stateMachine.AddState(names[i]);s.motion=clip;s.speedParameterActive=i>0;s.speedParameter=i<3?"LocomotionSpeed":"ActionSpeed";if(i==0)c.layers[0].stateMachine.defaultState=s;}
   a.runtimeAnimatorController=c;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   ((AnimationClip)c.layers[0].stateMachine.defaultState.motion).SampleAnimation(a.gameObject,0);var bounds=BoundsOf(model);float scale=8/bounds.size.y;pivot.localScale=Vector3.one*scale;pivot.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
   Materialize(model,"BigChunk");var agent=root.AddComponent<NavMeshAgent>();agent.height=8;agent.radius=2;agent.stoppingDistance=5;agent.acceleration=12;
   var col=root.AddComponent<CapsuleCollider>();col.height=8;col.radius=2;col.center=Vector3.up*4;
   var boss=root.AddComponent<BossCharacter>();boss.kind=BossKind.BigChunk;boss.visual=pivot;boss.useNativeAnimator=true;boss.baseHealth=2400;boss.goldReward=500;Death(a,"BigChunk");
   PrefabUtility.SaveAsPrefabAsset(root,Dir+"Big Chunk.prefab");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Dir.TrimEnd('/')})){
   string path=AssetDatabase.GUIDToAssetPath(guid);var p=PrefabUtility.LoadPrefabContents(path);
   try{
    var a=p.GetComponentInChildren<Animator>(true);if(!a)continue;
    bool muck=a.runtimeAnimatorController&&(AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("Muck")||p.name=="Big Chunk");
    if(muck)Death(a,p.name);
    if(p.name=="Golem funerario"){var v=p.transform.Find("Visual");var b=BoundsOf(p);float s=3.8f/b.size.y;v.localScale*=s;v.localPosition*=s;var col=p.GetComponent<CapsuleCollider>();col.height=3.8f;col.radius=.95f;col.center=Vector3.up*1.9f;var ag=p.GetComponent<NavMeshAgent>();ag.height=3.8f;ag.radius=.95f;}
    if(p.name=="AngelKnight"){
     a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
     foreach(var r in p.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.enabled=true;r.updateWhenOffscreen=true;r.localBounds=new Bounds(Vector3.up*1.5f,Vector3.one*12);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
     var boss=p.GetComponent<BossCharacter>();boss.visual.localPosition=Vector3.up*.35f;
     foreach(var ps in p.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.playOnAwake=false;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
    PrefabUtility.SaveAsPrefabAsset(p,path);
   }finally{PrefabUtility.UnloadPrefabContents(p);}
  }
  BuildSword();AssetDatabase.SaveAssets();return "Built Big Chunk 8m, golem 3.8m, Muck death clips, Angel bounds and knight sword.";
 }
 static void BuildSword(){
  var catalog=Resources.Load<WeaponVisualCatalog>("WeaponVisualCatalog");var root=new GameObject("Espada de acero del peregrino");
  try{var steel=new Material(Shader.Find("Universal Render Pipeline/Lit"));steel.SetColor("_BaseColor",new Color(.24f,.27f,.3f));steel.SetFloat("_Metallic",.85f);steel.SetFloat("_Smoothness",.5f);string matPath="Assets/Resources/KnightSteel.mat";var stored=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!stored){AssetDatabase.CreateAsset(steel,matPath);stored=steel;}else UnityEngine.Object.DestroyImmediate(steel);
   var mesh=new Mesh();mesh.name="Tapered steel blade";mesh.vertices=new[]{new Vector3(-.035f,.16f,0),new Vector3(0,.16f,.012f),new Vector3(.035f,.16f,0),new Vector3(0,.16f,-.012f),new Vector3(-.025f,.9f,0),new Vector3(0,.9f,.009f),new Vector3(.025f,.9f,0),new Vector3(0,.9f,-.009f),new Vector3(0,1.06f,0)};mesh.triangles=new[]{0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0,4,8,5,5,8,6,6,8,7,7,8,4};mesh.RecalculateNormals();mesh.RecalculateBounds();string mp="Assets/Resources/KnightBlade.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(mp);if(!saved){AssetDatabase.CreateAsset(mesh,mp);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}
   var blade=new GameObject("Forged blade",typeof(MeshFilter),typeof(MeshRenderer));blade.transform.SetParent(root.transform,false);blade.GetComponent<MeshFilter>().sharedMesh=saved;blade.GetComponent<Renderer>().sharedMaterial=stored;
   Part(root,"Crossguard",new Vector3(0,.14f,0),new Vector3(.22f,.035f,.035f),stored);
   Part(root,"Grip",new Vector3(0,.03f,0),new Vector3(.032f,.19f,.035f),stored);
   Part(root,"Pommel",new Vector3(0,-.08f,0),new Vector3(.055f,.05f,.05f),stored);
   catalog.tiers[0]=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/KnightStartingSword.prefab");EditorUtility.SetDirty(catalog);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static void Part(GameObject root,string name,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform,false);g.transform.localPosition=p;g.transform.localScale=s;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;}
}
