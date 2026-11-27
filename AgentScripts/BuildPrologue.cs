using System;
using System.Linq;
using System.IO;
using Codice.Prologue;
using Codice.Mechanics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class BuildPrologue
{
    const string Folder = "Assets/07_Prologue";
    const string Pack = "Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    const string ScenePath = "Assets/00_Scenes/Maps/Prologo.unity";
    static Material atlas;
    public static string Build()
    {
        if(EditorApplication.isPlaying) throw new Exception("Exit Play Mode before building.");
        for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
        if(File.Exists(ScenePath)) throw new Exception("Prologue scene already exists. Update it directly.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        atlas = AssetDatabase.LoadAssetAtPath<Material>("Assets/00_Scenes/Maps/Materials/DungeonAtlas.mat");
        var floor=Model("floor_tile_large",new Vector3(4,.35f,4),true);
        var wall=Model("wall",new Vector3(4,4.4f,.5f),true);
        var gate=Model("wall_gated",new Vector3(4,4.4f,.5f),true);
        var torch=Model("torch_mounted",new Vector3(.3f,1,.35f),false);
        var barrel=Model("barrel_large_decorated",new Vector3(.9f,1.2f,.9f),true);
        var source=EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/Calabozo.unity",OpenSceneMode.Additive);
        var run=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RunScene>(true)).Single();
        var player=Object.Instantiate(run.player.gameObject).GetComponent<Codice.Characters.PlayerCharacter>();
        player.name="Prologue player";
        if(!player.view || !player.view.transform.IsChildOf(player.transform)) throw new Exception("Player camera is not self-contained.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player.gameObject,scene); SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(source,true);
        var director=new GameObject("Prologue director").AddComponent<PrologueDirector>();
        director.player=player; director.floorPrefab=floor;director.wallPrefab=wall;director.gatePrefab=gate;
        director.torchPrefab=torch;director.propPrefab=barrel;
        director.npcPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CindersMenu/Class_Mage.prefab");
        if(!director.npcPrefab) throw new Exception("Existing mage model missing.");
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        var light=new GameObject("Cold dungeon skylight").AddComponent<Light>();
        light.type=LightType.Directional; light.color=new Color(.45f,.55f,.7f);light.intensity=.65f;
        light.transform.rotation=Quaternion.Euler(55,-35,0);light.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.2f,.23f,.29f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.025f;
        RenderSettings.fogColor=new Color(.025f,.032f,.045f);RenderSettings.skybox=null;
        player.view.clearFlags=CameraClearFlags.SolidColor;player.view.backgroundColor=RenderSettings.fogColor;
        player.view.farClipPlane=160;player.view.cullingMask &= ~(1<<30);
        player.transform.position=new Vector3(0,.1f,-2.8f);
        EditorSceneManager.SaveScene(scene,ScenePath);
        if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath)) EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
        return "Prologue scene saved; existing dungeon assets and player connected; registered in build settings.";
    }
    static GameObject Model(string name,Vector3 size,bool solid)
    {
        string path=Folder+"/"+name+".prefab";
        if(File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+name+".fbx");
        if(!source)throw new Exception("Missing model "+name);
        var root=new GameObject(name);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
        var renderers=model.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        var scale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
        model.transform.localScale=scale;model.transform.localPosition=-Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
        foreach(var r in renderers)r.sharedMaterials=Enumerable.Repeat(atlas,r.sharedMaterials.Length).ToArray();
        foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        if(solid){var c=root.AddComponent<BoxCollider>();c.size=size;c.center=Vector3.up*size.y/2;}
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return prefab;
    }
    public static string CheckLayouts()
    {
        var signatures=new System.Collections.Generic.HashSet<string>();
        for(int seed=1;seed<=100;seed++)
        {
            var layout=new PrologueLayout(seed);
            if(!layout.IsConnected() || layout.Rooms.Count!=5)throw new Exception("Bad seed "+seed);
            signatures.Add(string.Join(";",layout.Rooms));
        }
        if(signatures.Count<40)throw new Exception("Insufficient layout variation");
        return "100 seeds connected; distinct layouts="+signatures.Count;
    }
}
