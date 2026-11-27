using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using UnityEngine.Playables;using UnityEngine.Animations;
public static class VerifyLordsRetarget {
 public static string Run()=>RunRole("Warrior");
 public static string All()=>string.Join("\n",new[]{"Warrior","Mage","Shinobi","Archer","Tank","Berserker"}.Select(RunRole));
 static string RunRole(string role){
  string root="Assets/Resources/Presentation/Lords/";var paths=Directory.GetFiles(root,"*.anim");
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Presentation/Classes/"+role+".prefab"));
  go.transform.position=new Vector3(5000,0,0);foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
  var animator=go.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.runtimeAnimatorController=null;
  var graph=PlayableGraph.Create("Lords retarget validation");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output=AnimationPlayableOutput.Create(graph,"body",animator);graph.Play();
  var cameraGo=new GameObject("Retarget preview camera");var cam=cameraGo.AddComponent<Camera>();cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.17f,.2f);cam.fieldOfView=40;cam.nearClipPlane=.05f;cam.farClipPlane=30;
  cam.transform.position=go.transform.position+new Vector3(2.8f,1.7f,4);cam.transform.LookAt(go.transform.position+Vector3.up*.9f);
  var lightGo=new GameObject("Retarget key");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(30,-30,0);
  var rt=new RenderTexture(360,360,24);cam.targetTexture=rt;var sheet=new Texture2D(1440,1080,TextureFormat.RGB24,false);
  var previews=new[]{"AS_SS1H_LightAttack01","AS_GENE_Run_Fnt","AS_GENE_DiveRoll_MediumBck_3m","AS_GENE_1H_Parry"};
  float minFeet=100,maxHeight=0;int sampled=0;string extra="";
  try{
   foreach(var path in paths){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip.isHumanMotion)throw new Exception("Not human "+path);
    var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);output.SetSourcePlayable(playable);
    for(int frame=0;frame<12;frame++){
     playable.SetTime(clip.length*frame/12f);graph.Evaluate(0);
     foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh);if(mesh.vertices.Any(v=>float.IsNaN(v.x)||float.IsInfinity(v.x)||v.magnitude>10))throw new Exception("Mesh invalid "+clip.name);UnityEngine.Object.DestroyImmediate(mesh);}
     var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);minFeet=Mathf.Min(minFeet,left.position.y,right.position.y);maxHeight=Mathf.Max(maxHeight,animator.GetBoneTransform(HumanBodyBones.Head).position.y);sampled++;
    }
    if(clip.name=="AS_GENE_Run_Fnt"){playable.SetTime(0);graph.Evaluate(0);using(var h=new HumanPoseHandler(animator.avatar,animator.transform)){var p=new HumanPose();h.GetHumanPose(ref p);extra=" LHand="+(animator.GetBoneTransform(HumanBodyBones.LeftHand).position-go.transform.position)+" Head="+(animator.GetBoneTransform(HumanBodyBones.Head).position-go.transform.position)+" Root="+p.bodyPosition+" Q="+p.bodyRotation+string.Join(";",p.muscles.Select((v,i)=>HumanTrait.MuscleName[i]+"="+v).Where(s=>s.Contains("Arm ")||s.Contains("Forearm")));}} int col=Array.IndexOf(previews,clip.name);if(col>=0)for(int row=0;row<3;row++){
     playable.SetTime(clip.length*(.15f+row*.3f));graph.Evaluate(0);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(360,360,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,360,360),0,0);tex.Apply();sheet.SetPixels(col*360,(2-row)*360,360,360,tex.GetPixels());UnityEngine.Object.DestroyImmediate(tex);
    }graph.DestroyPlayable(playable);
   }
   sheet.Apply();File.WriteAllBytes("D:/67/CindersBackups/2026-11-07-lords-retarget/poses-"+role+".png",sheet.EncodeToPNG());
   string report="PASS "+role+" "+paths.Length+" humanoid clips; "+sampled+" baked poses finite; minFoot="+minFeet+" maxHead="+maxHeight+extra;Debug.Log("[LordsRetarget] "+report);return report;
  }finally{graph.Destroy();RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);UnityEngine.Object.DestroyImmediate(go);}
 }
}



