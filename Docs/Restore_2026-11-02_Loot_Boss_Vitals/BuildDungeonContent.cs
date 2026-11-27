using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Codice.Mechanics;
using Codice.Expedition;
public static class BuildDungeonContent
{
    const string Output="Assets/Resources/CindersDungeon";
    static Material Mat(string name,Color color,float metal=0,float smooth=.2f)
    {
        string path=Output+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static GameObject Part(Transform root,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;return g;
    }
    static void Save(GameObject root,string name){PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);}
    static void Fit(GameObject g,float size,bool height)
    {
        var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        float scale=size/(height?b.size.y:b.size.x);g.transform.localScale*=scale;g.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z)*scale;
    }
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode");
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();
        var bronze=Mat("Antique bronze",new Color(.48f,.28f,.09f),.65f);
        var iron=Mat("Trap iron",new Color(.24f,.28f,.31f),.7f,.4f);
        var stone=Mat("Shrine stone",new Color(.16f,.19f,.20f));
        var amber=Mat("Estus amber",new Color(1,.42f,.055f),.1f,.5f);amber.EnableKeyword("_EMISSION");amber.SetColor("_EmissionColor",new Color(.5f,.12f,.006f));
        var root=new GameObject("Reward chest");
        var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Models/Map/MapDungeon/kenney_mini-dungeon/Models/FBX format/chest.fbx"),root.transform);
        Fit(model,1.7f,false);
        foreach(var r in model.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterials=r.sharedMaterials.Select(source=>{
                var texture=source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null;
                var m=Mat("Chest palette",new Color(.85f,.62f,.38f),.15f,.25f);m.SetTexture("_BaseMap",texture);return m;
            }).ToArray();
        }
        var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.5f,0);collider.size=new Vector3(1.7f,1,1.2f);
        var chest=root.AddComponent<TreasureChest>();chest.price=0;chest.expeditionReward=true;
        Save(root,"RewardChest");
        root=new GameObject("Estus bottle");model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Models/Power Ups/BTM_Items_Gems/Prefabs/RedPotion.prefab"),root.transform);
        Fit(model,.22f,true);foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(amber,r.sharedMaterials.Length).ToArray();
        foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
        Save(root,"EstusBottle");
        root=new GameObject("Fuente de ceniza");root.AddComponent<EstusShrine>();
        Part(root.transform,"Pedestal",PrimitiveType.Cylinder,new Vector3(0,.35f,0),new Vector3(1.1f,.35f,1.1f),stone);
        Part(root.transform,"Bowl",PrimitiveType.Cylinder,new Vector3(0,.78f,0),new Vector3(1.45f,.10f,1.45f),bronze);
        var bottle=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/EstusBottle.prefab"),root.transform);bottle.transform.localPosition=Vector3.up*.9f;bottle.transform.localScale=Vector3.one*2;
        var sphere=root.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=.85f;sphere.center=Vector3.up*.7f;
        var light=new GameObject("Amber flame").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.transform.localPosition=Vector3.up*1.3f;light.type=LightType.Point;light.range=5;light.intensity=2;light.color=new Color(1,.48f,.12f);
        Save(root,"EstusShrine");
        root=new GameObject("Retracting iron spikes");root.layer=30;
        var trapPlate=Mat("Trap warning plate",new Color(.2f,.18f,.13f),.45f);trapPlate.EnableKeyword("_EMISSION");trapPlate.SetColor("_EmissionColor",Color.black);
        var plate=Part(root.transform,"Plate",PrimitiveType.Cube,Vector3.zero,new Vector3(2.8f,.06f,2.8f),trapPlate);plate.layer=30;UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
        var vertices=new List<Vector3>();var indices=new List<int>();
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
        {
            int start=vertices.Count;var c=new Vector3(x*.85f,.04f,z*.85f);
            vertices.Add(c+new Vector3(-.18f,0,-.18f));vertices.Add(c+new Vector3(-.18f,0,.18f));vertices.Add(c+new Vector3(.18f,0,.18f));vertices.Add(c+new Vector3(.18f,0,-.18f));vertices.Add(c+Vector3.up*.85f);
            for(int j=0;j<4;j++){indices.Add(start+j);indices.Add(start+(j+1)%4);indices.Add(start+4);}
        }
        string meshPath=Output+"/IronSpikes.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
        mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var spikes=new GameObject("Spikes",typeof(MeshFilter),typeof(MeshRenderer));spikes.transform.SetParent(root.transform,false);spikes.layer=30;spikes.GetComponent<MeshFilter>().sharedMesh=mesh;spikes.GetComponent<MeshRenderer>().sharedMaterial=iron;
        root.AddComponent<DungeonSpikeTrap>();Save(root,"SpikeTrap");
        ExpeditionEnemiesBuild.Build();AssetDatabase.SaveAssets();return "Built reward chest, Estus bottle, refill shrine, spike trap and colored hound materials.";
    }
}
