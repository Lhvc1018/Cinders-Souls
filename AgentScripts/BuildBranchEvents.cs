using System.Linq;
using UnityEngine;
using UnityEditor;
using Codice.Characters;
using Codice.Expedition;
public static class BuildBranchEvents
{
    public static string Build()
    {
        const string dir="Assets/Resources/CindersDungeon/";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Merchant.prefab");
        var merchant=(GameObject)PrefabUtility.InstantiatePrefab(source);PrefabUtility.SaveAsPrefabAsset(merchant,dir+"Merchant.prefab");Object.DestroyImmediate(merchant);
        var root=new GameObject("Statue · oath of ash");var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Models/Map/MapBosses/kenney_mini-arena/Models/FBX format/statue.fbx"),root.transform);
        var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        model.transform.localScale*=1.6f/bounds.size.y;bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        var mat=AssetDatabase.LoadAssetAtPath<Material>(dir+"OathStone.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,dir+"OathStone.mat");}
        mat.color=new Color(.25f,.22f,.19f);mat.SetFloat("_Smoothness",.2f);foreach(var r in renderers)r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
        foreach(var c in root.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        var box=root.AddComponent<BoxCollider>();box.size=new Vector3(.9f,1.6f,.9f);box.center=Vector3.up*.8f;root.AddComponent<BlessingStatue>();
        PrefabUtility.SaveAsPrefabAsset(root,dir+"BlessingStatue.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();return "Merchant and 1.6 m stone statue saved.";
    }
}
