using UnityEditor;
using UnityEngine;
public static class BuildImportedVfx
{
    public static string Build()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources/CindersVfx");AssetDatabase.Refresh();
        string[] paths={"Assets/Hovl Studio/Procedural fire/Prefabs/Magic fire pro orange.prefab","Assets/FX_Kandol_Pack/FX_lightning_II/Prefabs/fx_lightning_01.prefab"};
        for(int i=0;i<2;i++){var source=AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);if(!source)throw new System.Exception(paths[i]);var go=(GameObject)PrefabUtility.InstantiatePrefab(source);foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/CindersVfx/"+(i==0?"Fire":"Lightning")+".prefab");Object.DestroyImmediate(go);}
        AssetDatabase.SaveAssets();return "Imported fire and lightning prefabs referenced without modifying originals";
    }
}
