using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildGothicArchitecture
{
    const string Out="Assets/Resources/GothicArchitecture/";
    const string Grave="Assets/03_Models/StructuresItemsInMap/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/";
    const string Dungeon="Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    const string Arena="Assets/03_Models/Map/MapBosses/kenney_mini-arena/Models/FBX format/";

    public static string Run()
    {
        Directory.CreateDirectory(Out); AssetDatabase.Refresh();
        Make("Arcade",Grave+"arch.fbx",4,5,.85f,true);
        Make("Facade",Grave+"crypt.fbx",4,5.8f,1.55f,true);
        Make("Tracery",Grave+"fence.fbx",4,2.8f,.4f,true);
        Make("Paving",Arena+"floor-detail.fbx",4,.35f,4,true,true);
        Make("Grating",Dungeon+"floor_tile_big_grate.fbx",4,.35f,4,true,true);
        Make("Monument",Arena+"statue.fbx",2,4.2f,2,false);
        Make("Ossuary",Grave+"coffin_decorated.fbx",1.25f,.9f,2.4f,false);
        Make("BareTree",Grave+"tree_dead_large.fbx",3,5.5f,3,false);
        Make("Shrine",Grave+"shrine_candles.fbx",2,3.4f,1.4f,false);
        Make("BrokenPillar",Arena+"column-damaged.fbx",1.5f,4,1.5f,false);
        return "10 normalized architecture prefabs";
    }
    public static string CemeteryExpansion(){
        Make("BurialWall",Dungeon+"wall_cracked.fbx",4,6,.7f,true);
        Make("BurialCrypt",Grave+"crypt.fbx",6,6,5,true);
        Make("BurialBench",Grave+"bench_decorated.fbx",2.4f,1.2f,1,false);
        Make("BurialLantern",Grave+"post_lantern.fbx",.7f,3.3f,.7f,false);
        Make("BurialCoffin",Grave+"coffin.fbx",1.1f,.65f,2.5f,true);
        Make("BurialTree",Grave+"tree_dead_medium.fbx",4,7,4,false);
        Make("BurialCross",Grave+"gravemarker_B.fbx",1.2f,2,1,false);
        Make("BurialPath",Grave+"path_A.fbx",4,.18f,4,true,true);
        Make("BurialGate",Grave+"arch_gate.fbx",4,5.5f,.6f,false);
        AssetDatabase.SaveAssets();return "Nine cemetery modules imported";
    }
    static void Make(string name,string source,float width,float height,float depth,bool solid,bool floor=false)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<GameObject>(source);
        if(!mesh)throw new Exception(source);
        var root=new GameObject(name);
        try{
            var model=UnityEngine.Object.Instantiate(mesh,root.transform,false);
            var renderers=model.GetComponentsInChildren<Renderer>();
            Bounds b=renderers.First().bounds;
            foreach(var r in renderers)b.Encapsulate(r.bounds);
            var scale=new Vector3(width/Mathf.Max(.01f,b.size.x),height/Mathf.Max(.01f,b.size.y),depth/Mathf.Max(.01f,b.size.z));
            model.transform.localPosition=Vector3.Scale(-new Vector3(b.center.x,b.min.y,b.center.z),scale);
            model.transform.localScale=Vector3.Scale(model.transform.localScale,scale);
            var mat=Resources.Load<Material>("CindersStone/"+(floor?"Floor":"Wall"));
            foreach(var r in renderers)r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
            foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            if(solid){var c=root.AddComponent<BoxCollider>();c.size=new Vector3(width,height,depth);c.center=Vector3.up*height*.5f;}
            PrefabUtility.SaveAsPrefabAsset(root,Out+name+".prefab");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    public static string BuildUpperLevels()
    {
        Make("TimberDeck",Dungeon+"floor_wood_large_dark.fbx",3.2f,.25f,4,true);
        Make("DockCargo",Dungeon+"crates_stacked.fbx",1.6f,1.3f,1.1f,false);
        Make("ScaffoldWindow",Dungeon+"wall_archedwindow_gated_scaffold.fbx",4,5.8f,1.1f,true);
        Make("WakeTable",Dungeon+"table_long_decorated_A.fbx",2.6f,1,1.1f,false);
        Make("VotiveShelf",Dungeon+"shelf_small_candles.fbx",1.8f,1.9f,.75f,false);
        Make("ChoirBanner",Dungeon+"banner_patternA_red.fbx",1.1f,2.2f,.2f,false);
        // These pieces use their original atlas instead of the dungeon stone override.
        var names=new[]{"TimberDeck","DockCargo","ScaffoldWindow","WakeTable","VotiveShelf","ChoirBanner"};
        var files=new[]{"floor_wood_large_dark","crates_stacked","wall_archedwindow_gated_scaffold","table_long_decorated_A","shelf_small_candles","banner_patternA_red"};
        for(int i=0;i<names.Length;i++){
            var path=Out+names[i]+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try{
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(Dungeon+files[i]+".fbx");
                var src=original.GetComponentsInChildren<Renderer>();
                var dst=root.GetComponentsInChildren<Renderer>();
                for(int j=0;j<dst.Length;j++){
                    var materials=src[j].sharedMaterials.Select(m=>{
                        var dest=Out+names[i]+"-"+m.name+".mat";
                        var result=AssetDatabase.LoadAssetAtPath<Material>(dest);
                        if(!result){result=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(result,dest);}
                        result.mainTexture=m.mainTexture;
                        var tint=names[i]=="ChoirBanner"?new Color(.45f,.105f,.13f):new Color(.42f,.34f,.27f);
                        result.color=tint;result.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(result);return result;
                    }).ToArray();
                    dst[j].sharedMaterials=materials;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return "6 gallery modules";
    }
}
