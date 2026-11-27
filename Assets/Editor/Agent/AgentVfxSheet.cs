using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
public static class AgentVfxSheet {
 public static string Run(){
  const int S=220;var paths=Directory.GetFiles("Assets/Resources/CindersVfx/Impacts","*.prefab").OrderBy(p=>p).ToArray();float[] times={.08f,.2f,.45f};
  var camGo=new GameObject("vfx cam");var cam=camGo.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.11f,.12f);cam.cullingMask=1<<31;cam.fieldOfView=50;
  var rt=new RenderTexture(S,S,24);cam.targetTexture=rt;var sheet=new Texture2D(S*paths.Length,S*times.Length,TextureFormat.RGB24,false);string info="";
  try{
   for(int i=0;i<paths.Length;i++){
    var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]),new Vector3(7000,0,0),Quaternion.identity);foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
    cam.transform.position=go.transform.position+new Vector3(0,.6f,-3.2f);cam.transform.LookAt(go.transform.position);
    var systems=go.GetComponentsInChildren<ParticleSystem>();
    for(int k=0;k<times.Length;k++){foreach(var ps in systems.Where(p=>p.transform.parent==null||p.transform.parent.GetComponentInParent<ParticleSystem>()==null))ps.Simulate(times[k],true,true);
     cam.Render();RenderTexture.active=rt;var tex=new Texture2D(S,S,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,S,S),0,0);tex.Apply();sheet.SetPixels(i*S,(times.Length-1-k)*S,S,S,tex.GetPixels());Object.DestroyImmediate(tex);}
    var b=new Bounds(go.transform.position,Vector3.zero);foreach(var r in go.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);info+=Path.GetFileNameWithoutExtension(paths[i])+" size="+b.size.ToString("F2")+"\n";
    Object.DestroyImmediate(go);
   }
   sheet.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../MapReview/agent/impacts.png"),sheet.EncodeToPNG());return info;
  }finally{RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(sheet);Object.DestroyImmediate(camGo);}
 }
}
