using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
// Secret bosses for hidden vaults: spectral echoes of earlier guardians (desaturated, violet-lit, a bit weaker).
public static class AgentSecretBosses{
 const string D="Assets/Resources/Secrets/Bosses/";
 public static string Run(){if(EditorApplication.isPlaying)return "stop play";Directory.CreateDirectory(D+"Materials");AssetDatabase.Refresh();var sb=new StringBuilder();
  (string src,string title,float hp,Color tint)[] list={("Assets/Resources/ExpeditionEnemies/AngelKnight.prefab","Eco del Ángel Caído",.6f,new Color(.55f,.55f,.75f)),("Assets/Resources/ExpeditionEnemies/HorrorInquisitor.prefab","Inquisidor Olvidado",.75f,new Color(.5f,.45f,.6f)),
   ("Assets/Resources/ExpeditionEnemies/Necromancer.prefab","Sepulturero sin Nombre",.7f,new Color(.45f,.6f,.55f)),("Assets/Resources/ExpeditionEnemies/Gurdi del Abismo.prefab","Custodio Hueco",.75f,new Color(.5f,.5f,.6f)),("Assets/Resources/WorldFive/Bosses/Ogro del Foso.prefab","Matriarca del Foso",.8f,new Color(.6f,.45f,.6f))};
  int k=0;foreach(var e in list){var go=PrefabUtility.LoadPrefabContents(e.src);try{go.name=e.title;var b=go.GetComponent<Codice.Bosses.BossCharacter>();b.baseHealth*=e.hp;b.goldReward=Mathf.RoundToInt(b.goldReward*.6f);int i=0;
   foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m)return m;var p=D+"Materials/"+k+"-"+(i++)+".mat";var c=AssetDatabase.LoadAssetAtPath<Material>(p);if(!c){c=new Material(m);AssetDatabase.CreateAsset(c,p);}else c.CopyPropertiesFromMaterial(m);
    var col=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white;if(c.HasProperty("_BaseColor"))c.SetColor("_BaseColor",new Color(col.r*e.tint.r,col.g*e.tint.g,col.b*e.tint.b,col.a));if(c.HasProperty("_EmissionColor")){c.EnableKeyword("_EMISSION");c.SetColor("_EmissionColor",new Color(.09f,.05f,.22f));}EditorUtility.SetDirty(c);return c;}).ToArray();
   var aura=new GameObject("Aura espectral").AddComponent<Light>();aura.transform.SetParent(go.transform,false);aura.transform.localPosition=Vector3.up*2;aura.type=LightType.Point;aura.color=new Color(.55f,.35f,1f);aura.range=8;aura.intensity=2.2f;aura.shadows=LightShadows.None;
   PrefabUtility.SaveAsPrefabAsset(go,D+e.title+".prefab");sb.AppendLine(e.title+" hp="+b.baseHealth);}finally{PrefabUtility.UnloadPrefabContents(go);}k++;}
  AssetDatabase.SaveAssets();return sb.ToString();}
 public static string Audit(){var sb=new StringBuilder();int vaults=0,bossVaults=0,bad=0;for(int floor=2;floor<=15;floor++)for(int s=0;s<150;s++){var l=new Codice.Expedition.ExpeditionLayout(unchecked(1000+s+(floor-1)*104729),floor);if(!l.Connected())bad++;if(l.VaultRoom>=0){vaults++;if(Codice.Expedition.ExpeditionLayout.VaultBossFloor(floor))bossVaults++;}if(floor>=7&&l.Levels.Count!=l.Cells.Count)bad++;}
  sb.Append($"layouts=2100 vaults={vaults} bossVaults={bossVaults} invalid={bad}");return sb.ToString();}
}
