using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildWorldProgression
{
    public static string Run()
    {
        Directory.CreateDirectory("Assets/Resources/WorldProgression");
        AssetDatabase.Refresh();
        string npc=AssetDatabase.GUIDToAssetPath("7f442c3ff64cdb64e99f3ea609f5026a");
        if(!AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/WorldProgression/Watcher.prefab"))
            if(!AssetDatabase.CopyAsset(npc,"Assets/Resources/WorldProgression/Watcher.prefab"))throw new Exception("Watcher copy failed");
        string mutant="Assets/Resources/ExpeditionEnemies/MonsterMutant.prefab";
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(mutant))AssetDatabase.CopyAsset("Assets/01_Prefabs/Bosses/MonsterMutant.prefab",mutant);
        Style(mutant,false);
        Style("Assets/Resources/Presentation/Classes/Shinobi.prefab",true);
        AssetDatabase.SaveAssets();return "Watcher and MonsterMutant registered; Shinobi lacquer, cord, mask and helmet refined without changing animation bindings.";
    }
    static void Style(string path,bool shinobi)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    if(!materials[i])continue;
                    string key=(renderer.name+materials[i].name).ToLowerInvariant();
                    string filename=(shinobi?"Shinobi_":"Mutant_")+materials[i].name.Replace("/","_")+".mat";
                    string dest="Assets/Resources/WorldProgression/"+filename;
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(dest);
                    if(!mat){mat=new Material(materials[i]);AssetDatabase.CreateAsset(mat,dest);}
                    mat.shader=Shader.Find("Universal Render Pipeline/Lit");
                    bool cord=key.Contains("cord")||key.Contains("laton")||key.Contains("lazado");
                    bool cloth=key.Contains("tela")||key.Contains("cuero")||key.Contains("cloth");
                    mat.SetColor("_BaseColor",shinobi?(cord?new Color(.35f,.17f,.075f):cloth?new Color(.055f,.065f,.085f):new Color(.11f,.13f,.16f)):new Color(.42f,.32f,.29f));
                    mat.SetFloat("_Metallic",shinobi&&!cloth?.55f:0);mat.SetFloat("_Smoothness",shinobi&&!cloth?.32f:.18f);
                    materials[i]=mat;EditorUtility.SetDirty(mat);
                }
                renderer.sharedMaterials=materials;
            }
            if(shinobi)foreach(var t in root.GetComponentsInChildren<Transform>())
            {
                if(t.name.StartsWith("Identity - Cresta"))t.localScale=new Vector3(.82f,.74f,.9f);
                if(t.name=="Identity - Kabuto")t.localScale=new Vector3(.97f,.96f,.98f);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
