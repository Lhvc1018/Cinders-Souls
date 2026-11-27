using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public static class RepairFireMaterials
{
 public static string Run()
 {
  AssetDatabase.Refresh();var shader=Shader.Find("Cinders/Particles URP");if(!shader||!shader.isSupported)throw new System.Exception("URP shader unavailable");int count=0;
  foreach(string name in new[]{"Fire","Lightning"})
  {
   string path="Assets/Resources/CindersVfx/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);var cache=new Dictionary<Material,Material>();
   foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
   {
    var materials=renderer.sharedMaterials;
    for(int i=0;i<materials.Length;i++)
    {
     var source=materials[i];if(!source){materials[i]=new Material(shader);materials[i].SetColor("_Color",name=="Fire"?new Color(1,.4f,.05f):new Color(.6f,.3f,1));string fallback="Assets/Resources/CindersVfx/Materials/"+name+"_Fallback.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(fallback);if(existing){Object.DestroyImmediate(materials[i]);materials[i]=existing;}else AssetDatabase.CreateAsset(materials[i],fallback);continue;}
     if(!cache.TryGetValue(source,out var mat))
     {
      string dest="Assets/Resources/CindersVfx/Materials/"+name+"_"+source.name+".mat";
      mat=AssetDatabase.LoadAssetAtPath<Material>(dest);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,dest);}mat.shader=shader;
      Texture texture=source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):null;
      mat.SetTexture("_MainTex",texture);mat.SetColor("_Color",name=="Fire"?new Color(1,.42f,.09f,.8f):new Color(.55f,.3f,1,.8f));mat.SetFloat("_Intensity",1.4f);EditorUtility.SetDirty(mat);cache[source]=mat;count++;
     }
     materials[i]=mat;
    }
    renderer.sharedMaterials=materials;
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
  }
  AssetDatabase.SaveAssets();return count+" URP effect materials repaired; original asset packages preserved";
 }
}
