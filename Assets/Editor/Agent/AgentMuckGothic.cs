using System;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.Animations;using UnityEngine;
using Codice.Enemies;
// Repaints the native Muck rigs in the Cinders palette: desaturated bases, ash/bronze/crimson tints, ember or abyss-blue emission.
public static class AgentMuckGothic {
 const string Out="Assets/Resources/ExpeditionEnemies/MuckGothic/";
 const string Tex="Assets/Research/MuckNative/Texture2D/";
 struct Look{public string match;public Color tint;public float metal,smooth,sat;public string emission;public Color glow;public string normal;}
 static Look L(string m,float r,float g,float b,float metal=.05f,float smooth=.15f,float sat=.3f,string em=null,float er=0,float eg=0,float eb=0,string normal=null)=>new Look{match=m,tint=new Color(r,g,b),metal=metal,smooth=smooth,sat=sat,emission=em,glow=new Color(er,eg,eb),normal=normal};
 static readonly (string prefab,Look[] looks)[] Specs={
  ("Duende Mutante",new[]{L("Hogoblin",.4f,.39f,.33f,.05f,.2f,.25f,"HogoblinTexture_emission.png",2.4f,.8f,.12f),L("SWordGoblin",.55f,.52f,.55f,.75f,.45f,.15f),L("Default",.2f,.18f,.18f)}),
  ("Lanza del Infierno",new[]{L("Skin",.34f,.31f,.31f,.0f,.22f,.05f),L("Cloth",.24f,.045f,.04f,.0f,.1f,.6f),L("Gold",.48f,.34f,.17f,.8f,.35f,.5f),L("Feather",.12f,.11f,.12f,0,.1f,.2f),L("Eyes",.1f,.05f,.03f,0,.5f,0,null,3f,.9f,.15f),L("ChiefSpear",.5f,.46f,.44f,.65f,.4f,.2f)}),
  ("Gurdi del Abismo",new[]{L("Guardian",.3f,.29f,.36f,.25f,.28f,.0f,"Guardian_Emission.png",.35f,.75f,2.2f)}),
  ("MuckResidents/Salteador de la cripta",new[]{L("Goblin-Goblin",.38f,.4f,.33f,.0f,.18f,.2f,"GolbinTexture_emission.png",2.2f,.7f,.1f),L("No Name",.22f,.18f,.17f)}),
  ("MuckResidents/Lobo de hollin",new[]{L("Wolf",.15f,.14f,.15f,.0f,.12f,.1f,"WoldTexture_Emission.png",2.8f,.6f,.08f)}),
  ("MuckResidents/Golem funerario",new[]{L("StoneGolem",.22f,.22f,.26f,.1f,.22f,.1f,"GolemTexture_emissive.png",.35f,.7f,2.4f,"GolemTexture_normal.png")}),
 };
 public static string Build(){
  if(EditorApplication.isPlaying)return "stop play first";
  Directory.CreateDirectory(Out);AssetDatabase.Refresh();var sb=new StringBuilder();
  foreach(var (prefab,looks) in Specs)sb.AppendLine(Repaint("Assets/Resources/ExpeditionEnemies/"+prefab+".prefab",looks,Path.GetFileName(prefab)));
  sb.AppendLine(BuildSlinger());
  AssetDatabase.SaveAssets();return sb.ToString();
 }
 static string Repaint(string path,Look[] looks,string tag){
  var root=PrefabUtility.LoadPrefabContents(path);int n=0;
  try{
   foreach(var r in root.GetComponentsInChildren<Renderer>(true)){
    if(r is ParticleSystemRenderer||r is LineRenderer||r is TrailRenderer)continue;
    var mats=r.sharedMaterials;
    for(int i=0;i<mats.Length;i++){var m=mats[i];if(!m)continue;var look=looks.FirstOrDefault(l=>m.name.Contains(l.match));if(look.match==null)continue;mats[i]=Make(m,look,tag);n++;}
    r.sharedMaterials=mats;
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  return tag+": "+n+" material slots repainted";
 }
 static Material Make(Material src,Look look,string tag){
  string clean=src.name.Replace("Vigia_","").Replace(" (Instance)","");
  Material m;
  if(src.name.StartsWith("Gothic_")&&src.name.Contains(tag))m=src; // re-tune in place, keep the desaturated base
  else{
   var path=Out+"Gothic_"+tag+"_"+clean.Replace("Gothic_","")+".mat";m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   var baseTex=src.HasProperty("_BaseMap")?src.GetTexture("_BaseMap"):null;if(!baseTex&&src.HasProperty("_MainTex"))baseTex=src.GetTexture("_MainTex");
   if(baseTex)m.SetTexture("_BaseMap",src.name.StartsWith("Gothic_")?baseTex:Desaturate(baseTex as Texture2D,look.sat,tag+"_"+clean));
  }
  m.SetColor("_BaseColor",look.tint);m.SetFloat("_Metallic",look.metal);m.SetFloat("_Smoothness",look.smooth);
  if(look.normal!=null){var nt=AssetDatabase.LoadAssetAtPath<Texture2D>(Tex+look.normal);if(nt){m.SetTexture("_BumpMap",nt);m.EnableKeyword("_NORMALMAP");}}
  bool glow=look.glow.maxColorComponent>0;
  if(glow){var et=look.emission!=null?AssetDatabase.LoadAssetAtPath<Texture2D>(Tex+look.emission):null;if(et)m.SetTexture("_EmissionMap",et);m.SetColor("_EmissionColor",look.glow);m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
  else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
  EditorUtility.SetDirty(m);return m;
 }
 static Texture Desaturate(Texture2D t,float saturation,string name){
  if(!t)return null;var path=Out+"Tex_"+name+".png";
  var rt=RenderTexture.GetTemporary(t.width,t.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(t,rt);
  var prev=RenderTexture.active;RenderTexture.active=rt;var c=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false);c.ReadPixels(new Rect(0,0,t.width,t.height),0,0);RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);
  var px=c.GetPixels();for(int i=0;i<px.Length;i++){var p=px[i];float l=p.r*.3f+p.g*.59f+p.b*.11f;px[i]=new Color(Mathf.Lerp(l,p.r,saturation),Mathf.Lerp(l,p.g,saturation),Mathf.Lerp(l,p.b,saturation),p.a);}
  c.SetPixels(px);c.Apply();File.WriteAllBytes(path,c.EncodeToPNG());UnityEngine.Object.DestroyImmediate(c);AssetDatabase.ImportAsset(path);
  return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 // New ranged common enemy from the unused native GoblinRanged clip.
 static string BuildSlinger(){
  const string Dir="Assets/Resources/ExpeditionEnemies/MuckResidents/";
  string prefabPath=Dir+"Hondero de la cripta.prefab",ctrlPath=Dir+"CryptSlinger.controller",clipPath=Dir+"Goblin_GoblinRanged.anim";
  if(!AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath)){var src=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Research/MuckNative/AnimationClip/GoblinRanged.anim");var copy=UnityEngine.Object.Instantiate(src);AnimationUtility.SetAnimationEvents(copy,new AnimationEvent[0]);var st=AnimationUtility.GetAnimationClipSettings(copy);st.loopTime=false;AnimationUtility.SetAnimationClipSettings(copy,st);AssetDatabase.CreateAsset(copy,clipPath);}
  if(!AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath))AssetDatabase.CopyAsset(Dir+"Goblin.controller",ctrlPath);
  var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);ctrl.name="CryptSlinger";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
  var sm=ctrl.layers[0].stateMachine;var shoot=sm.states.FirstOrDefault(s=>s.state.name=="Shoot").state??sm.AddState("Shoot",new Vector3(300,300,0));
  shoot.motion=clip;shoot.speedParameterActive=true;shoot.speedParameter="ActionSpeed";EditorUtility.SetDirty(ctrl);
  if(!AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath))AssetDatabase.CopyAsset(Dir+"Salteador de la cripta.prefab",prefabPath);
  var root=PrefabUtility.LoadPrefabContents(prefabPath);
  try{
   root.name="Hondero de la cripta";var e=root.GetComponent<BasicEnemy>();e.variantName="Hondero de la cripta";e.kind=EnemyKind.Crossbowman;e.nativeWindup=.75f;e.healthMultiplier=.6f;e.damageMultiplier=.8f;e.speedMultiplier=1.05f;
   var anim=e.animator?e.animator:root.GetComponentInChildren<Animator>();anim.runtimeAnimatorController=ctrl;
   if(anim.transform!=root.transform)anim.transform.localScale*=.92f;
   var looks=new[]{L("Goblin-Goblin",.3f,.32f,.37f,.0f,.25f,.05f,"GolbinTexture_emission.png",2.6f,.2f,.1f),L("No Name",.36f,.07f,.05f,0,.12f,.5f)};
   foreach(var r in root.GetComponentsInChildren<Renderer>(true)){if(r is ParticleSystemRenderer)continue;var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var m=mats[i];if(!m)continue;var look=looks.FirstOrDefault(l=>m.name.Contains(l.match));if(look.match==null)continue;var name=m.name.Replace("Gothic_Salteador de la cripta_","");var orig=AssetDatabase.LoadAssetAtPath<Material>(Dir+name+".mat")??m;mats[i]=Make(orig,look,"Hondero");}r.sharedMaterials=mats;}
   PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  return "Hondero de la cripta: ranged goblin built (Shoot="+clip.name+" "+clip.length.ToString("F2")+"s)";
 }
}
