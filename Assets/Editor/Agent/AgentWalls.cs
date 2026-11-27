using System.Linq;
using UnityEditor;
using UnityEngine;
/// Cracked-wall audit: the cracked mesh must only mean "breakable secret wall".
public static class AgentWalls
{
    const string Kit="Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    public static string Fix()
    {
        var sb=new System.Text.StringBuilder();
        const string burial="Assets/Resources/GothicArchitecture/BurialWall.prefab",secret="Assets/Resources/GothicArchitecture/SecretCrackedWall.prefab";
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(secret)){AssetDatabase.CopyAsset(burial,secret);sb.AppendLine("copied cracked wall -> SecretCrackedWall");}
        var intact=AssetDatabase.LoadAllAssetsAtPath(Kit+"wall.fbx").OfType<Mesh>().FirstOrDefault();
        if(!intact)return "wall.fbx mesh not found";
        var root=PrefabUtility.LoadPrefabContents(burial);
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true)){sb.AppendLine($"BurialWall {mf.name}: {mf.sharedMesh?.name} -> {intact.name}");mf.sharedMesh=intact;}
        foreach(var mc in root.GetComponentsInChildren<MeshCollider>(true))mc.sharedMesh=intact;
        PrefabUtility.SaveAsPrefabAsset(root,burial);PrefabUtility.UnloadPrefabContents(root);
        var floor=Object.FindAnyObjectByType<Codice.Expedition.ExpeditionFloor>(FindObjectsInactive.Include);
        if(floor&&floor.wallPrefab)foreach(var mf in floor.wallPrefab.GetComponentsInChildren<MeshFilter>(true))sb.AppendLine($"wallPrefab {mf.name}: {mf.sharedMesh?.name} bounds={mf.sharedMesh?.bounds.size}");
        sb.AppendLine("intact bounds="+intact.bounds.size);
        AssetDatabase.SaveAssets();return sb.ToString();
    }
}
