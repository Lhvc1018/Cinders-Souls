using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.Playables;using UnityEngine.Animations;
public static class AgentPoseSheet {
 public static string Request="";// "prefabPath|controllerPath|clip1,clip2|t1,t2|out.png|katana"
 public static string Run(){
  var a=File.ReadAllText(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/sheet.txt")).Trim().Split('|');
  return Render(a[0],a[1],a[2].Split(','),a[3].Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray(),a[4],a.Length>5?a[5]:"");
 }
 public static string Render(string prefab,string controller,string[] clips,float[] times,string output,string weapon){
  const int S=300;
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));go.transform.position=new Vector3(6000,0,0);
  var animator=go.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.enabled=true;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
  if(weapon=="dual"){var it=new Codice.Characters.WeaponInstance(Codice.Characters.WeaponKind.Sword);Codice.Presentation.HumanoidWeaponGrip.Attach(animator,it,false);Codice.Presentation.HumanoidWeaponGrip.Attach(animator,it,true);}
  if(weapon.StartsWith("katana")){var item=new Codice.Characters.WeaponInstance(Codice.Characters.WeaponKind.Sword);item.katana=true;Codice.Presentation.HumanoidWeaponGrip.Attach(animator,item,false);}
  foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
  var b=new Bounds(go.transform.position,Vector3.zero);foreach(var r in go.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);float h=1.9f;
  var ctrl=controller.Length>0?AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controller):null;
  var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output2=AnimationPlayableOutput.Create(graph,"b",animator);graph.Play();
  var camGo=new GameObject("sheet cam");var cam=camGo.AddComponent<Camera>();cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.17f,.2f);cam.fieldOfView=40;
  var camDir=(weapon.EndsWith("back")?new Vector3(-.9f,.25f,-1.5f):new Vector3(.9f,.25f,1.5f)).normalized*4.2f;cam.transform.position=go.transform.position+camDir;cam.transform.LookAt(go.transform.position+Vector3.up*h*.45f);
  var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.layer=31;floor.transform.position=go.transform.position;floor.transform.localScale=Vector3.one*h;
  var lightGo=new GameObject("sheet light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(40,-30,0);
  var rt=new RenderTexture(S,S,24);cam.targetTexture=rt;var sheet=new Texture2D(S*times.Length,S*clips.Length,TextureFormat.RGB24,false);
  try{
   for(int row=0;row<clips.Length;row++){
    var clip=clips[row].EndsWith(".anim")?AssetDatabase.LoadAssetAtPath<AnimationClip>(clips[row]):ctrl.animationClips.FirstOrDefault(c=>c.name==clips[row]);if(!clip)continue;
    var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);output2.SetSourcePlayable(p);
    for(int col=0;col<times.Length;col++){p.SetTime(clip.length*times[col]);graph.Evaluate(0);foreach(var ribbon in go.GetComponentsInChildren<Codice.Presentation.ScarfRibbon>())for(int k=0;k<40;k++)ribbon.Step(.02f);var hp=animator.GetBoneTransform(HumanBodyBones.Hips).position;cam.transform.position=new Vector3(hp.x,go.transform.position.y+1.3f,hp.z)+camDir;cam.transform.LookAt(new Vector3(hp.x,go.transform.position.y+.95f,hp.z));cam.Render();RenderTexture.active=rt;var tex=new Texture2D(S,S,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,S,S),0,0);tex.Apply();sheet.SetPixels(col*S,(clips.Length-1-row)*S,S,S,tex.GetPixels());UnityEngine.Object.DestroyImmediate(tex);}
    graph.DestroyPlayable(p);
   }
   sheet.Apply();var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../MapReview/agent/"+output));File.WriteAllBytes(path,sheet.EncodeToPNG());return "wrote "+path;
  }finally{graph.Destroy();RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(camGo);UnityEngine.Object.DestroyImmediate(lightGo);UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(go);}
 }
}
