using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.Playables;using UnityEngine.Animations;
// Front / three-quarter / back portraits of posed characters. arg.txt: out.png then lines "prefabPath|controllerPath|state".
public static class AgentCoutureShot{public static string Run(){
 var lines=File.ReadAllLines(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Where(l=>l.Trim().Length>0).ToArray();string outName=lines[0];lines=lines.Skip(1).ToArray();
 const int W=440,H=620;var camGo=new GameObject("cam");var cam=camGo.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.07f,.07f,.09f);cam.cullingMask=1<<31;cam.fieldOfView=28;
 var key=new GameObject("key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.5f;key.color=new Color(1,.94f,.86f);key.cullingMask=1<<31;
 var rim=new GameObject("rim").AddComponent<Light>();rim.type=LightType.Directional;rim.intensity=.9f;rim.color=new Color(.5f,.6f,1f);rim.cullingMask=1<<31;
 var rt=new RenderTexture(W,H,24);cam.targetTexture=rt;var sheet=new Texture2D(W*3,H*lines.Length,TextureFormat.RGB24,false);string info="";
 try{for(int i=0;i<lines.Length;i++){var a=lines[i].Split('|');GameObject go;
   if(a[0]=="VIGIA"){go=new GameObject("vigia host");go.transform.position=new Vector3(8600+i*30,0,0);Codice.Presentation.VigiaLook.Dress(go);}
   else go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(a[0]),new Vector3(8600+i*30,0,0),Quaternion.identity);
   foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var l in go.GetComponentsInChildren<Light>(true))l.cullingMask=1<<31;
   var anim=go.GetComponentInChildren<Animator>();anim.enabled=true;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;var ctrl=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(a[1]);var clip=ctrl.animationClips.FirstOrDefault(c=>c.name==a[2])??ctrl.animationClips[0];anim.runtimeAnimatorController=null;anim.Rebind();
   var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var o=AnimationPlayableOutput.Create(graph,"b",anim);var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);o.SetSourcePlayable(p);graph.Play();p.SetTime(.2f);graph.Evaluate(0);
   foreach(var ribbon in go.GetComponentsInChildren<Codice.Presentation.ScarfRibbon>())for(int k=0;k<90;k++)ribbon.Step(.02f);var pole=go.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Farol de vigilia");var grip=go.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="B-hand.R");if(pole&&grip)pole.SetPositionAndRotation(grip.position,Quaternion.identity);
   var hips=go.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="B-hips");var c=(hips?new Vector3(hips.position.x,go.transform.position.y,hips.position.z):go.transform.position)+Vector3.up*1.02f;var dirs=new[]{new Vector3(.15f,.1f,1),new Vector3(.9f,.2f,.6f),new Vector3(-.35f,.15f,-1)};
   for(int v=0;v<3;v++){cam.transform.position=c+dirs[v].normalized*5.3f;cam.transform.LookAt(c);key.transform.rotation=Quaternion.LookRotation(-(dirs[v]+new Vector3(.6f,.9f,.2f)).normalized);rim.transform.rotation=Quaternion.LookRotation((dirs[v]*.8f+new Vector3(-.5f,-.3f,0)).normalized);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(W,H,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,W,H),0,0);tex.Apply();sheet.SetPixels(v*W,(lines.Length-1-i)*H,W,H,tex.GetPixels());Object.DestroyImmediate(tex);}
   info+=a[0]+" clip="+clip.name+" renderers="+go.GetComponentsInChildren<Renderer>().Length+"\n";graph.Destroy();Object.DestroyImmediate(go);}
  sheet.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../MapReview/agent/"+outName),sheet.EncodeToPNG());return info;}
 finally{RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(sheet);Object.DestroyImmediate(camGo);Object.DestroyImmediate(key.gameObject);Object.DestroyImmediate(rim.gameObject);}
}}
