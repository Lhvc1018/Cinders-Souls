using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
public static class NereoShot{public static string Run(){
 var host=new GameObject("vigia host");host.transform.position=new Vector3(8200,0,0);var body=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("WorldProgression/Nereo"),host.transform,false).transform;if(!body){Object.DestroyImmediate(host);return "dress failed";}
 foreach(var t in host.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var l in host.GetComponentsInChildren<Light>())l.cullingMask=1<<31;
 var anim=body.GetComponentInChildren<Animator>();var clip=anim.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name=="StaffIdle")??anim.runtimeAnimatorController.animationClips[0];
 const int W=520,H=640;var camGo=new GameObject("cam");var cam=camGo.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.05f,.06f);cam.cullingMask=1<<31;cam.fieldOfView=32;
 var key=new GameObject("key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.9f;key.cullingMask=1<<31;key.transform.rotation=Quaternion.Euler(35,-40,0);
 var rt=new RenderTexture(W,H,24);cam.targetTexture=rt;var sheet=new Texture2D(W*2,H,TextureFormat.RGB24,false);string info="clip="+clip.name+" human="+anim.isHuman;
 try{for(int v=0;v<2;v++){anim.Rebind();clip.SampleAnimation(anim.gameObject,0);
   // re-seat accessories is unnecessary: they are bone children
   var c=host.transform.position+Vector3.up*1.05f;var dir=v==0?new Vector3(.45f,.15f,1):new Vector3(-.5f,.15f,-1);cam.transform.position=c+dir.normalized*4.6f;cam.transform.LookAt(c);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(W,H,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,W,H),0,0);tex.Apply();sheet.SetPixels(v*W,0,W,H,tex.GetPixels());Object.DestroyImmediate(tex);}
  sheet.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../MapReview/agent/nereo.png"),sheet.EncodeToPNG());return info;}
 finally{RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(sheet);Object.DestroyImmediate(camGo);Object.DestroyImmediate(key.gameObject);Object.DestroyImmediate(host);}
}}
