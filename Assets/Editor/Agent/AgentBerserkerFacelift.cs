using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.AI;
// Final-boss facelift for «Berserker Orco»: drops the stuck-on horn props, restores a darker bronze-blood palette,
// scales him up as the closing boss and gives smouldering eyes.
public static class AgentBerserkerFacelift{public static string Run(){
 const string P="Assets/Resources/WorldFive/Bosses/Berserker Orco.prefab";var go=PrefabUtility.LoadPrefabContents(P);int removed=0,tinted=0;
 try{foreach(var t in go.GetComponentsInChildren<Transform>(true).Where(t=>t&&t.name.StartsWith("Horda ·")).ToArray()){Object.DestroyImmediate(t.gameObject);removed++;}
  var b=go.GetComponent<Codice.Bosses.BossCharacter>();b.baseHealth=5200;b.goldReward=1300;
  var vis=b.visual?b.visual:go.transform.GetChild(0);if(!vis.name.Contains("·final")){vis.localScale*=1.22f;vis.name+=" ·final";var cap=go.GetComponent<CapsuleCollider>();cap.height*=1.22f;cap.radius*=1.22f;cap.center*=1.22f;var ag=go.GetComponent<NavMeshAgent>();ag.radius*=1.15f;ag.height*=1.22f;}
  var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Bosses/OrkBerserker.prefab").GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).ToArray();
  foreach(var r in go.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials){if(!m||!AssetDatabase.GetAssetPath(m).Contains("WorldFive/Materials"))continue;var orig=src.FirstOrDefault(o=>m.name.Contains(o.name))??src.FirstOrDefault(o=>o.shader==m.shader);
   var c=orig&&orig.HasProperty("_BaseColor")?orig.GetColor("_BaseColor"):Color.white;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",new Color(c.r*.82f,c.g*.7f,c.b*.66f,c.a));
   bool eye=m.name.IndexOf("eye",System.StringComparison.OrdinalIgnoreCase)>=0;if(m.HasProperty("_EmissionColor")){if(eye){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(3f,.6f,.1f));}else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}}EditorUtility.SetDirty(m);tinted++;}
  PrefabUtility.SaveAsPrefabAsset(go,P);}finally{PrefabUtility.UnloadPrefabContents(go);}AssetDatabase.SaveAssets();return "removed="+removed+" tinted="+tinted;}}
