using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;
public static class VerifyNativeVisuals {
 public static string Run(){
 var report=new List<string>();var sheet=new Texture2D(1200,800,TextureFormat.RGB24,false);
 var names=new[]{"Duende Mutante","Lanza del Infierno","Gurdi del Abismo"};
 for(int n=0;n<3;n++){
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ExpeditionEnemies/"+names[n]+".prefab"));
  go.transform.position=new Vector3(5000,0,0);foreach(var b in go.GetComponents<MonoBehaviour>())b.enabled=false;
  var anim=go.GetComponentInChildren<Animator>();anim.enabled=false;
  var transforms=go.GetComponentsInChildren<Transform>(true);var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();Action reset=()=>{for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}};var clips=anim.runtimeAnimatorController.animationClips.Distinct().ToArray();
  foreach(var clip in clips){int missing=AnimationUtility.GetCurveBindings(clip).Count(b=>b.path!=""&&!anim.transform.Find(b.path));if(missing>0)throw new Exception(names[n]+" missing animation paths "+missing+" "+clip.name);
   for(int k=0;k<8;k++){reset();clip.SampleAnimation(anim.gameObject,clip.length*k/8);foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh);if(mesh.vertices.Any(v=>float.IsNaN(v.x)||v.magnitude>10000))throw new Exception("Invalid mesh "+names[n]);UnityEngine.Object.DestroyImmediate(mesh);}}
  }
  foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
  var lightGo=new GameObject("Preview key");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<31;lightGo.transform.rotation=Quaternion.Euler(35,-25,0);
  var cameraGo=new GameObject("Preview camera");var camera=cameraGo.AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.17f);camera.fieldOfView=42;camera.nearClipPlane=.05f;camera.farClipPlane=100;
  camera.transform.position=go.transform.position+new Vector3(4,2.8f,7);camera.transform.LookAt(go.transform.position+Vector3.up*1.7f);
  var rt=new RenderTexture(400,400,24);camera.targetTexture=rt;
  for(int row=0;row<2;row++){
   var clip=clips.First(c=>c.name==(row==0?"Idle":"Melee"));reset();clip.SampleAnimation(anim.gameObject,row==0?0:clip.length*.45f);camera.Render();RenderTexture.active=rt;
   var tex=new Texture2D(400,400,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,400,400),0,0);tex.Apply();sheet.SetPixels(n*400,(1-row)*400,400,400,tex.GetPixels());UnityEngine.Object.DestroyImmediate(tex);
  }
  RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);UnityEngine.Object.DestroyImmediate(go);
  report.Add(names[n]+": "+clips.Length+" clips; all transform paths matched; 8 samples per clip finite");
 }
 sheet.Apply();System.IO.File.WriteAllBytes("D:/67/CindersBackups/2026-11-07-native-muck/visuals.png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);return string.Join("\n",report);
 }
}

