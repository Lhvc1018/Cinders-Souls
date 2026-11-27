using System.Collections.Generic;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
// World 3 residents: (1) real procedural motion for the unrigged spiders, (2) two authored variants per resident
// with their own palette, accessories, proportions and stats (Resources/ExpeditionEnemies/Cemetery/Variants).
public static class AgentCemeteryVariants{
 const string Dir="Assets/Resources/ExpeditionEnemies/Cemetery/";const string Out=Dir+"Variants/";
 // ------------------------------------------------------------------ spider motion
 class Track{public string path;public Vector3 rotRest,posRest,scaleRest;public List<Keyframe>[] r=new List<Keyframe>[3],p=new List<Keyframe>[3];public Track(){for(int i=0;i<3;i++){r[i]=new List<Keyframe>();p[i]=new List<Keyframe>();}}}
 delegate void Pose(float t,Dictionary<string,(Vector3 rot,Vector3 pos)> o);
 static AnimationClip Clip(string asset,Transform model,string[] parts,float length,bool loop,Pose pose,int samples=24){
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,asset);}
  var keep=AnimationUtility.GetCurveBindings(clip).Where(b=>!(b.type==typeof(Transform))).ToArray();clip.ClearCurves();
  var tracks=new Dictionary<string,Track>();
  foreach(var path in parts){var t=path==""?model:model.Find(path);if(!t)continue;tracks[path]=new Track{path=path,rotRest=t.localEulerAngles,posRest=t.localPosition,scaleRest=t.localScale};}
  for(int i=0;i<=samples;i++){float time=length*i/samples;var o=new Dictionary<string,(Vector3,Vector3)>();pose(time/length,o);
   foreach(var kv in tracks){o.TryGetValue(kv.Key,out var d);var rot=kv.Value.rotRest+d.Item1;var pos=kv.Value.posRest+d.Item2;
    for(int a=0;a<3;a++){kv.Value.r[a].Add(new Keyframe(time,rot[a]));kv.Value.p[a].Add(new Keyframe(time,pos[a]));}}}
  string[] axes={"x","y","z"};
  foreach(var kv in tracks)for(int a=0;a<3;a++){
   var rc=new AnimationCurve(kv.Value.r[a].ToArray());var pc=new AnimationCurve(kv.Value.p[a].ToArray());for(int k=0;k<rc.length;k++){rc.SmoothTangents(k,0);pc.SmoothTangents(k,0);}
   AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(kv.Key,typeof(Transform),"localEulerAnglesRaw."+axes[a]),rc);
   AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(kv.Key,typeof(Transform),"m_LocalPosition."+axes[a]),pc);}
  var s=AnimationUtility.GetAnimationClipSettings(clip);s.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,s);EditorUtility.SetDirty(clip);return clip;}
 static float S(float t,float cycles=1)=>Mathf.Sin(t*Mathf.PI*2*cycles);
 static float Bell(float t,float a,float b)=>t<a||t>b?0:Mathf.Sin(Mathf.InverseLerp(a,b,t)*Mathf.PI);
 static float Ease(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
 static string SpiderMotion(string name,bool legged){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+name+".prefab");var inst=(GameObject)Object.Instantiate(prefab);var anim=inst.GetComponentInChildren<Animator>();var model=anim.transform;
  string[] parts=legged?new[]{"","FrontLegs","Head","Bug","LeftEye","RightEye"}:new[]{"","HuntsmanSpider","Eyes"};
  string body=legged?"Head":"HuntsmanSpider";float u=legged?1:1.4f;// amplitude scale
  var clips=new Dictionary<string,AnimationClip>();
  void Add(string state,float len,bool loop,Pose p)=>clips[state]=Clip(Dir+name+"_State_"+state+".anim",model,parts,len,loop,p);
  Add("Idle",2.2f,true,(t,o)=>{o[""]=(new Vector3(S(t)*2*u,0,0),new Vector3(0,.012f*S(t,2),0));o[body]=(new Vector3(S(t,1)*3,S(t,.5f)*4,0),Vector3.zero);if(legged)o["FrontLegs"]=(new Vector3(S(t,2)*4,0,0),Vector3.zero);});
  Pose scuttle(float speed)=> (t,o)=>{o[""]=(new Vector3(S(t,2)*3*u,S(t)*4*u,S(t,2)*5*u),new Vector3(0,.035f*Mathf.Abs(S(t,2))*speed,0));o[body]=(new Vector3(S(t,2)*5,S(t)*6,0),Vector3.zero);if(legged)o["FrontLegs"]=(new Vector3(S(t,2)*20*speed,S(t)*8,0),Vector3.zero);};
  Add("Patrol",.7f,true,scuttle(.7f));Add("Chase",.42f,true,scuttle(1));Add("Run",.36f,true,scuttle(1.2f));
  Pose lunge(float rear,float reach)=> (t,o)=>{float up=Ease(t,0,.32f)*(1-Ease(t,.36f,.46f));float strike=Bell(t,.36f,.62f);float rec=1-Ease(t,.62f,1);
   o[""]=(new Vector3(-rear*up+18*strike*rec,0,0),new Vector3(0,.08f*up,-.12f*up+reach*strike*rec));o[body]=(new Vector3(-12*up+22*strike,0,0),Vector3.zero);if(legged)o["FrontLegs"]=(new Vector3(-35*up+25*strike,0,0),Vector3.zero);};
  Add("Melee",.95f,false,lunge(22,.45f));Add("Slam",1.2f,false,lunge(34,.25f));
  Pose spit=(t,o)=>{float rise=Ease(t,0,.35f)*(1-Ease(t,.75f,1));float shake=Bell(t,.35f,.7f)*S(t,9)*5;o[""]=(new Vector3(-30*rise,0,shake),new Vector3(0,.1f*rise,-.05f*rise));o[body]=(new Vector3(-18*rise+shake,0,0),Vector3.zero);if(legged)o["FrontLegs"]=(new Vector3(-40*rise,0,0),Vector3.zero);};
  Add("Shoot",1.05f,false,spit);Add("Summon",1.3f,false,spit);
  Add("Hit",.38f,false,(t,o)=>{float k=Bell(t,0,1)*(1-t);o[""]=(new Vector3(-16*k,S(t,3)*6*k,S(t,4)*8*k),new Vector3(0,.03f*k,-.14f*k));});
  Add("Stun",1f,true,(t,o)=>{o[""]=(new Vector3(8,0,S(t,6)*7),new Vector3(0,-.03f,0));o[body]=(new Vector3(14+S(t,3)*3,S(t,2)*5,0),Vector3.zero);if(legged)o["FrontLegs"]=(new Vector3(25,0,0),Vector3.zero);});
  // wire states
  var ctrl=anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;var sm=ctrl.layers[0].stateMachine;int wired=0;
  foreach(var kv in clips){var st=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==kv.Key)??sm.AddState(kv.Key);st.motion=kv.Value;wired++;}
  EditorUtility.SetDirty(ctrl);Object.DestroyImmediate(inst);return name+" clips="+wired;}
 // ------------------------------------------------------------------ variants
 static Material Tinted(Material src,string id,Color tint,Color? glow){Directory.CreateDirectory(Out+"Materials");var path=Out+"Materials/"+id+" · "+src.name.Replace("/","_")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(src);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(src);
  var c=src.HasProperty("_BaseColor")?src.GetColor("_BaseColor"):src.HasProperty("_Color")?src.GetColor("_Color"):Color.white;var t=new Color(c.r*tint.r,c.g*tint.g,c.b*tint.b,c.a);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",t);if(m.HasProperty("_Color"))m.SetColor("_Color",t);
  bool eye=src.name.IndexOf("eye",System.StringComparison.OrdinalIgnoreCase)>=0;if(!eye&&m.HasProperty("_EmissionColor")){m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
  if(eye&&glow.HasValue&&m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",glow.Value);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}EditorUtility.SetDirty(m);return m;}
 static Transform Find(GameObject go,params string[] names){foreach(var n in names){var t=go.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name==n||x.name.EndsWith(":"+n));if(t)return t;}return go.GetComponentsInChildren<Renderer>(true).OrderByDescending(r=>r.bounds.size.sqrMagnitude).First().transform;}
 static GameObject Prop(Transform parent,string asset,Vector3 worldOffset,Vector3 euler,float worldSize,Material mat=null){var src=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(!src)return null;var g=(GameObject)Object.Instantiate(src);g.name="Variante · "+src.name;
  foreach(var c in g.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);var b=new Bounds(g.transform.position,Vector3.zero);foreach(var r in g.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);float k=worldSize/Mathf.Max(.01f,b.size.magnitude);g.transform.localScale*=k;
  g.transform.position=parent.position+worldOffset;g.transform.rotation=Quaternion.Euler(euler);g.transform.SetParent(parent,true);if(mat)foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();return g;}
 static GameObject Shard(Transform parent,Vector3 worldOffset,Vector3 euler,float size,Material mat){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/ExpeditionEnemies/Terremotos_Esquirla.asset");var g=new GameObject("Variante · púa");g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.transform.position=parent.position+worldOffset;g.transform.rotation=Quaternion.Euler(euler);g.transform.localScale=Vector3.one*size/Mathf.Max(.01f,mesh?mesh.bounds.size.magnitude:1);g.transform.SetParent(parent,true);return g;}
 static void Candle(Transform parent,Vector3 worldOffset,float size,Color light){Prop(parent,"Assets/Resources/WorldThree/Kit/candle_triple.prefab",worldOffset,Vector3.zero,size);var l=new GameObject("Variante · llama").AddComponent<Light>();l.type=LightType.Point;l.range=3.5f;l.intensity=1.6f;l.color=light;l.shadows=LightShadows.None;l.transform.position=parent.position+worldOffset+Vector3.up*size*.7f;l.transform.SetParent(parent,true);}
 static Material Flat(string id,Color c,Color? glow,float metal=0,float smooth=.3f){Directory.CreateDirectory(Out+"Materials");var path=Out+"Materials/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);if(glow.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",glow.Value);}EditorUtility.SetDirty(m);return m;}
 delegate void Dress(GameObject go);
 static string Variant(string baseName,string id,string title,Color tint,Color? glow,float scale,float hp,float spd,float dmg,Dress dress){
  var src=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+baseName+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(src);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name=title;
  foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>m?Tinted(m,id,tint,glow):m).ToArray();
  var model=go.GetComponentInChildren<Animator>().transform;model.localScale*=scale;var cap=go.GetComponent<CapsuleCollider>();if(cap){cap.height*=scale;cap.radius*=scale;cap.center*=scale;}
  var e=go.GetComponent<Codice.Enemies.BasicEnemy>();e.variantName=title;e.healthMultiplier*=hp;e.speedMultiplier*=spd;e.damageMultiplier*=dmg;
  dress?.Invoke(go);
  var path=Out+id+".prefab";PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);return $"{id} \"{title}\" hp x{hp} spd x{spd} dmg x{dmg} scale x{scale}";}
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";Directory.CreateDirectory(Out);AssetDatabase.Refresh();var sb=new StringBuilder();
  sb.AppendLine(SpiderMotion("HeadSpider",true));sb.AppendLine(SpiderMotion("Huntsman",false));
  var ember=Flat("Brasa de cripta",new Color(.9f,.3f,.08f),new Color(3.2f,.9f,.15f));var bone=Flat("Hueso viejo",new Color(.78f,.74f,.64f),null,0,.2f);var iron=Flat("Hierro de horca",new Color(.18f,.16f,.15f),null,.8f,.4f);var soul=Flat("Fuego fatuo",new Color(.4f,1f,.7f),new Color(.6f,2.6f,1.4f));
  sb.AppendLine(Variant("HeadSpider","HeadSpider_Osario","Cría del osario",new Color(1.25f,1.18f,1.05f),new Color(.2f,.9f,.45f),.82f,.75f,1.22f,.9f,go=>{var h=Find(go,"Head");for(int i=0;i<4;i++)Shard(h,new Vector3((i-1.5f)*.07f,.13f,-.05f),new Vector3(-20,0,(i-1.5f)*18),.16f,bone);}));
  sb.AppendLine(Variant("HeadSpider","HeadSpider_Brasa","Araña de brasa",new Color(.42f,.3f,.27f),new Color(1.6f,.45f,.08f),1.18f,1.35f,.95f,1.2f,go=>{var h=Find(go,"Head");for(int i=0;i<3;i++)Shard(h,new Vector3((i-1)*.12f,.2f,-.12f-.04f*i),new Vector3(-35,0,(i-1)*22),.32f,ember);}));
  sb.AppendLine(Variant("Huntsman","Huntsman_Ceniza","Tejedora de ceniza",new Color(.62f,.6f,.66f),new Color(.6f,.25f,1.4f),1.05f,1.25f,1f,1f,go=>{var b=Find(go,"HuntsmanSpider");Candle(b,new Vector3(0,.42f,-.15f),.42f,new Color(1f,.6f,.3f));}));
  sb.AppendLine(Variant("Huntsman","Huntsman_Carmesi","Cazadora carmesí",new Color(.95f,.35f,.33f),new Color(1.8f,.1f,.1f),1.22f,1.1f,1.12f,1.25f,go=>{var b=Find(go,"HuntsmanSpider");for(int i=0;i<5;i++)Shard(b,new Vector3((i-2)*.13f,.32f,-.1f),new Vector3(-10,0,(i-2)*16),.3f,iron);}));
  sb.AppendLine(Variant("Executioner","Executioner_Campana","Verdugo de la campana",new Color(.55f,.55f,.6f),null,1.08f,1.35f,.92f,1.1f,go=>{var hips=Find(go,"Hips");Prop(hips,"Assets/Resources/WorldThree/Kit/keyring_hanging.prefab",new Vector3(.22f,.05f,.05f),new Vector3(0,90,0),.5f);var head=Find(go,"Head");foreach(int s in new[]{-1,1})Shard(head,new Vector3(s*.1f,.18f,0),new Vector3(0,0,s*-25),.28f,iron);}));
  sb.AppendLine(Variant("Executioner","Executioner_Sangre","Verdugo carmesí",new Color(1.1f,.42f,.4f),null,1f,1f,1.08f,1.3f,go=>{var spine=Find(go,"Spine2","Spine1");Prop(spine,"Assets/Resources/WorldThree/Kit/banner_thin_red.prefab",new Vector3(0,-.1f,-.22f),new Vector3(0,180,0),1.1f);}));
  sb.AppendLine(Variant("StrawReaper","StrawReaper_Vela","Segador de la vela",new Color(.7f,.62f,.5f),null,1f,1.1f,1f,1.1f,go=>{var head=Find(go,"Head");Candle(head,new Vector3(0,.25f,0),.38f,new Color(1f,.7f,.35f));}));
  sb.AppendLine(Variant("StrawReaper","StrawReaper_Podrido","Espantapájaros podrido",new Color(.5f,.68f,.45f),null,1.12f,1.45f,.88f,1f,go=>{var spine=Find(go,"Spine2","Spine1");for(int i=0;i<3;i++)Shard(spine,new Vector3((i-1)*.15f,.15f,-.2f),new Vector3(-30,0,(i-1)*25),.4f,bone);}));
  sb.AppendLine(Variant("Watcher","Watcher_Fatuo","Ojo de la cripta",new Color(.85f,.95f,.88f),new Color(.1f,.5f,.25f),1f,1.15f,1.05f,1.1f,go=>{var head=Find(go,"Head");var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.DestroyImmediate(orb.GetComponent<Collider>());orb.name="Variante · ojo fatuo";orb.GetComponent<Renderer>().sharedMaterial=soul;orb.transform.position=head.position+new Vector3(0,.32f,.05f);orb.transform.localScale=Vector3.one*.16f;orb.transform.SetParent(head,true);var l=orb.AddComponent<Light>();l.color=new Color(.45f,1,.7f);l.range=4;l.intensity=1.8f;l.shadows=LightShadows.None;}));
  sb.AppendLine(Variant("Watcher","Watcher_Velado","Vigía velado",new Color(.35f,.33f,.36f),null,1.1f,1.3f,.95f,1.15f,go=>{var head=Find(go,"Head");Prop(head,"Assets/Resources/WorldThree/Kit/banner_patternC_brown.prefab",new Vector3(0,-.15f,-.12f),new Vector3(0,180,0),1f);}));
  AssetDatabase.SaveAssets();return sb.ToString();}
}
