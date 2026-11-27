using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
public static class AgentLineup {
 public static string Run(){
  var paths=File.ReadAllLines(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/lineup.txt")).Where(l=>l.Trim().Length>0).ToArray();
  string outName=paths[0];paths=paths.Skip(1).ToArray();
  const int W=360,H=420;var camGo=new GameObject("lineup cam");var cam=camGo.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.09f,.09f,.11f);cam.cullingMask=1<<31;cam.fieldOfView=35;
  var key=new GameObject("key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.color=new Color(1,.93f,.85f);key.cullingMask=1<<31;key.transform.rotation=Quaternion.Euler(35,-40,0);
  var rim=new GameObject("rim").AddComponent<Light>();rim.type=LightType.Directional;rim.intensity=.8f;rim.color=new Color(.45f,.55f,1f);rim.cullingMask=1<<31;rim.transform.rotation=Quaternion.Euler(20,150,0);
  var rt=new RenderTexture(W,H,24);cam.targetTexture=rt;var sheet=new Texture2D(W*paths.Length,H*2,TextureFormat.RGB24,false);string info="";
  try{for(int i=0;i<paths.Length;i++){
   var go=(GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]),new Vector3(8000+i*50,0,0),Quaternion.identity);
   foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   var b=new Bounds(go.transform.position,Vector3.zero);bool f=true;foreach(var r in go.GetComponentsInChildren<Renderer>()){if(r is ParticleSystemRenderer)continue;if(f){b=r.bounds;f=false;}else b.Encapsulate(r.bounds);}
   float h=Mathf.Max(.5f,b.size.y,b.size.x*.8f,b.size.z*.8f);info+=Path.GetFileNameWithoutExtension(paths[i])+" h="+h.ToString("F2")+" mats="+string.Join(",",go.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct())+"\n";
   for(int v=0;v<2;v++){var dir=v==0?new Vector3(.5f,.25f,1):new Vector3(-.3f,.25f,-1);cam.transform.position=b.center+dir.normalized*h*2.6f;cam.transform.LookAt(b.center);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(W,H,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,W,H),0,0);tex.Apply();sheet.SetPixels(i*W,(1-v)*H,W,H,tex.GetPixels());Object.DestroyImmediate(tex);}
   Object.DestroyImmediate(go);}
   sheet.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../MapReview/agent/"+outName),sheet.EncodeToPNG());return info;
  }finally{RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(sheet);Object.DestroyImmediate(camGo);Object.DestroyImmediate(key.gameObject);Object.DestroyImmediate(rim.gameObject);}
 }
}
