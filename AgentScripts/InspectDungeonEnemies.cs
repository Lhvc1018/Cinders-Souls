using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectDungeonEnemies
{
    public static string Run()
    {
        var report=new System.Text.StringBuilder();
        foreach(string path in new[]{"Assets/03_Models/CreepyDoll/CreepyDoll.fbx","Assets/03_Models/DungeonCharacters/Skeletons_demo/models/DungeonSkeleton_demo.FBX","Assets/03_Models/DungeonCharacters/Skeletons_demo/animation/DS_onehand_walk.FBX","Assets/03_Models/DungeonCharacters/Skeletons_demo/animation/DS_onehand_attack_A.FBX"})
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            report.AppendLine(path+" rig="+importer.animationType+" takes="+string.Join(",",importer.defaultClipAnimations.Select(c=>c.name)));
            report.AppendLine("Clips: "+string.Join(";",AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Select(c=>c.name+" "+c.length)));
            if(!model)continue;
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())report.AppendLine("Mesh "+r.name+" bones="+r.bones.Length+" bounds="+r.sharedMesh.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"none")));
            report.AppendLine("Hierarchy: "+string.Join(";",model.GetComponentsInChildren<Transform>().Select(t=>AnimationUtility.CalculateTransformPath(t,model.transform))));
        }
        System.IO.File.WriteAllText("Docs/DungeonEnemyInventory.txt",report.ToString());return report.ToString();
    }
}
