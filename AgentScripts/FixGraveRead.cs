using UnityEngine;using UnityEditor;
public static class FixGraveRead{public static string Run(){foreach(var n in new[]{"grave_A","grave_A_destroyed"}){var i=(ModelImporter)AssetImporter.GetAtPath("Assets/03_Models/StructuresItemsInMap/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/"+n+".fbx");i.isReadable=true;i.SaveAndReimport();}return "Grave meshes readable for runtime NavMesh";}}
