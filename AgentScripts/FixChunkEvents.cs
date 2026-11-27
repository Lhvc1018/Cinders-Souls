using UnityEngine;
using UnityEditor;
public static class FixChunkEvents {public static string Run(){string path="Assets/Resources/ExpeditionEnemies/Big Chunk.prefab";var p=PrefabUtility.LoadPrefabContents(path);p.GetComponentInChildren<Animator>(true).fireEvents=false;PrefabUtility.SaveAsPrefabAsset(p,path);PrefabUtility.UnloadPrefabContents(p);AssetDatabase.SaveAssets();return "Native events disabled; combat timeline owns damage";}}
