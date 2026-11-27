using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
// Copies the WALLCOEUR impact pack into Resources with URP materials (the pack ships legacy built-in particle shaders).
public static class AgentImpactVfx {
 const string Src="Assets/06_Effects/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/";
 const string Dst="Assets/Resources/CindersVfx/Impacts/";
 static readonly string[] Picks={"0_Classic/VFX_Classic_01","0_Classic/VFX_Classic_03","0_Classic/VFX_Blood_01","0_Classic/VFX_Blood_02","0_Classic/VFX_Poison_01","0_Classic/VFX_Arcade_02","1_Critical/VFX_Critical_01","1_Critical/VFX_Critical_02","2_Impact_1.1.0/VFX_ImpactClassic01_1.1.0","2_Impact_1.1.0/VFX_ImpactCross_1.1.0","2_Impact_1.1.0/VFX_ImpactCritical_1.1.0_Pink","2_Impact_1.1.0/VFX_ImpactToon_1.1.0"};
 public static string Build(){
  if(EditorApplication.isPlaying)return "stop play first";
  Directory.CreateDirectory(Dst+"Materials");AssetDatabase.Refresh();
  var add=Shader.Find("Cinders/Particles URP");var alpha=Shader.Find("Cinders/Particles URP Alpha");
  if(!add||!alpha)return "shaders missing";
  var sb=new StringBuilder();
  foreach(var pick in Picks){
   var src=AssetDatabase.LoadAssetAtPath<GameObject>(Src+pick+".prefab");if(!src){sb.AppendLine("missing "+pick);continue;}
   var go=(GameObject)PrefabUtility.InstantiatePrefab(src);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   float duration=0;int systems=0;
   foreach(var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true)){
    var mats=r.sharedMaterials.Select(m=>Convert(m,add,alpha)).ToArray();r.sharedMaterials=mats;
    var ps=r.GetComponent<ParticleSystem>();var main=ps.main;duration=Mathf.Max(duration,main.duration+main.startLifetime.constantMax);main.playOnAwake=true;main.loop=false;systems++;
   }
   var b=new Bounds(go.transform.position,Vector3.zero);
   string name=Path.GetFileName(pick).Replace("_1.1.0","");go.name=name;
   PrefabUtility.SaveAsPrefabAsset(go,Dst+name+".prefab");Object.DestroyImmediate(go);
   sb.AppendLine($"{name}: systems={systems} duration={duration:F2}");
  }
  AssetDatabase.SaveAssets();return sb.ToString();
 }
 static Material Convert(Material m,Shader add,Shader alpha){
  if(!m)return null;var path=Dst+"Materials/"+m.name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
  bool additive=m.shader.name.Contains("Add")||m.shader.name.Contains("Additive");
  var c=new Material(additive?add:alpha);c.name=m.name;
  var tex=m.HasProperty("_MainTex")?m.GetTexture("_MainTex"):null;if(tex)c.SetTexture("_MainTex",tex);
  if(m.HasProperty("_MainTex")){c.SetTextureScale("_MainTex",m.GetTextureScale("_MainTex"));c.SetTextureOffset("_MainTex",m.GetTextureOffset("_MainTex"));}
  var tint=m.HasProperty("_TintColor")?m.GetColor("_TintColor"):m.HasProperty("_Color")?m.GetColor("_Color"):Color.white;
  c.SetColor("_Color",tint);c.SetFloat("_Intensity",m.HasProperty("_TintColor")?2:1);c.renderQueue=3000;
  AssetDatabase.CreateAsset(c,path);Debug.Log("[Agent] "+m.name+" "+m.shader.name+" -> "+c.shader.name);return c;
 }
}
