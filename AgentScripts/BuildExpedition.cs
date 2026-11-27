using System;
using System.IO;
using System.Linq;
using Codice.Expedition;
using Codice.Mechanics;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;
public static class BuildExpedition
{
    public static string Inspect()
    {
        var scene=EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/Calabozo.unity",OpenSceneMode.Additive);
        string result=string.Join("\n",scene.GetRootGameObjects().Select(g=>g.name+" ["+string.Join(",",g.GetComponents<Component>().Select(c=>c?c.GetType().Name:"MISSING"))+"]"));
        EditorSceneManager.CloseScene(scene,true);return result;
    }
    public static string Fonts()
    {
        Directory.CreateDirectory("Assets/Resources/CindersFonts");AssetDatabase.Refresh();
        Font("UncialAntiqua","Assets/Fonts/UncialAntiqua/UncialAntiqua-Regular.ttf");
        Font("CrimsonText","Assets/Fonts/CrimsonText/CrimsonText-SemiBold.ttf");
        AssetDatabase.SaveAssets();return "Two static SDF fonts created with Latin and Spanish glyphs and bundled OFL licenses.";
    }
    static void Font(string name,string sourcePath)
    {
        string path="Assets/Resources/CindersFonts/"+name+".asset";
        if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path))return;
        var source=AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(sourcePath);
        if(!source)throw new Exception("Missing font "+sourcePath);
        var asset=TMP_FontAsset.CreateFontAsset(source,90,9,GlyphRenderMode.SDF,2048,2048,AtlasPopulationMode.Dynamic);
        asset.name=name;
        string characters=new string(Enumerable.Range(32,95).Concat(Enumerable.Range(160,96)).Select(i=>(char)i).ToArray())+"—–‘’“”…•→←↑↓◆◇";
        asset.TryAddCharacters(characters,out string missing);
        if(!asset.HasCharacters("ÁÉÍÓÚÜÑáéíóúüñ¿¡",out System.Collections.Generic.List<char> absent))throw new Exception("Missing Spanish glyphs: "+string.Join("",absent));
        var fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        asset.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
        if(fallback)asset.fallbackFontAssetTable.Add(fallback);
        asset.atlasPopulationMode=AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(asset,path);
        foreach(var texture in asset.atlasTextures){texture.name=name+" atlas";AssetDatabase.AddObjectToAsset(texture,asset);}
        AssetDatabase.AddObjectToAsset(asset.material,asset);EditorUtility.SetDirty(asset);
    }
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        if(File.Exists("Assets/00_Scenes/Maps/Descenso.unity"))throw new Exception("Scene already exists");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
        var scene=EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/Calabozo.unity");
        EditorSceneManager.SaveScene(scene,"Assets/00_Scenes/Maps/Descenso.unity");
        var context=Object.FindAnyObjectByType<RunScene>();
        var player=context.player;var dungeon=context.dungeon;var arena=context.arena;
        // Keep only live gameplay references; remove the copied map and its baked navigation.
        var keep=new System.Collections.Generic.HashSet<GameObject>{player.transform.root.gameObject,context.transform.root.gameObject,dungeon.transform.root.gameObject,arena.transform.root.gameObject};
        foreach(var root in scene.GetRootGameObjects())
            if(!keep.Contains(root) && !root.GetComponent<UnityEngine.EventSystems.EventSystem>())Object.DestroyImmediate(root);
        foreach(var nav in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)){nav.RemoveData();Object.DestroyImmediate(nav);}
        if(context.portal){Object.DestroyImmediate(context.portal.gameObject);context.portal=null;}
        if(dungeon.merchant){Object.DestroyImmediate(dungeon.merchant.gameObject);dungeon.merchant=null;}
        context.lootMarkers=Array.Empty<Transform>();dungeon.spawnPoints=Array.Empty<Transform>();
        dungeon.startOnPlay=false;arena.startOnPlay=false;arena.managedByRun=true;
        if(arena.goblinPrefab)arena.bossPrefabs=new[]{arena.goblinPrefab};
        var floor=context.gameObject.AddComponent<ExpeditionFloor>();floor.context=context;
        floor.floorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/07_Prologue/floor_tile_large.prefab");
        floor.wallPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/07_Prologue/wall.prefab");
        floor.gatePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/07_Prologue/wall_gated.prefab");
        floor.torchPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/07_Prologue/torch_mounted.prefab");
        floor.propPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/07_Prologue/barrel_large_decorated.prefab");
        floor.pillarPrefab=BuildPillar();
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/00_Scenes/Maps/Materials/DungeonAtlas.mat");
        var mat=new Material(source){name="Ashen dungeon stone"};mat.SetColor("_BaseColor",new Color(.48f,.47f,.48f));
        AssetDatabase.CreateAsset(mat,"Assets/07_Prologue/AshenStone.mat");floor.stoneMaterial=mat;
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene("Assets/00_Scenes/Maps/Descenso.unity",true)}).ToArray();
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
        return "Descenso scene created with original Calabozo enemies, player, boss and seeded rooms.";
    }
    static GameObject BuildPillar()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/pillar_decorated.fbx");
        var root=new GameObject("Carved dungeon pillar");var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
        var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        var size=new Vector3(.85f,5.4f,.85f);var scale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
        model.transform.localScale=scale;model.transform.localPosition=-Vector3.Scale(new Vector3(b.center.x,b.min.y,b.center.z),scale);
        foreach(var r in rs)r.sharedMaterials=Enumerable.Repeat(AssetDatabase.LoadAssetAtPath<Material>("Assets/00_Scenes/Maps/Materials/DungeonAtlas.mat"),r.sharedMaterials.Length).ToArray();
        var col=root.AddComponent<BoxCollider>();col.size=size;col.center=Vector3.up*size.y/2;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/07_Prologue/pillar_decorated.prefab");Object.DestroyImmediate(root);return prefab;
    }
    public static string Layouts()
    {
        var unique=new System.Collections.Generic.HashSet<string>();
        for(int i=1;i<=100;i++){var l=new ExpeditionLayout(i);if(!l.Connected())throw new Exception("Disconnected "+i);unique.Add(string.Join(";",l.Rooms));}
        return "100 connected layouts; distinct="+unique.Count;
    }
}
