using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Codice.Bosses;

public static class FitMuckWardens
{
    const string Art = "Assets/04_Materials/VigiasDelAbismo";
    const string Bosses = "Assets/Resources/ExpeditionEnemies/";
    static readonly string[] Names = {"Lanza del Infierno","Duende Mutante","Gurdi del Abismo"};

    // Apply native bind poses once. BakeMesh/TransformPoint double counts scale on this particular imported rig.
    static IEnumerable<Vector3> SkinVertices(SkinnedMeshRenderer skin)
    {
        var mesh = skin.sharedMesh;
        var poses = mesh.bindposes;
        var matrices = skin.bones.Select((bone,i)=>bone.localToWorldMatrix * poses[i]).ToArray();
        var vertices = mesh.vertices;
        var weights = mesh.boneWeights;
        for(int i=0;i<vertices.Length;i++)
        {
            var v=vertices[i]; var w=weights[i];
            yield return matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0
                + matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1
                + matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2
                + matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
        }
    }

    static void FitChief(GameObject root)
    {
        var pivot = root.GetComponent<BossCharacter>().visual;
        var animator = pivot.GetComponentInChildren<Animator>();
        var model = animator.transform.Find("newPlayer");
        var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
        var spear = model.GetComponentsInChildren<MeshFilter>().First(f=>f.name=="ChiefSpear");
        var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
        var idle = clips.First(c=>c.name=="Idle");
        var bind = EditorCurveBinding.FloatCurve("newPlayer",typeof(Transform),"m_LocalPosition.y");
        foreach(var c in clips) AnimationUtility.SetEditorCurve(c,bind,null);
        // Import class default from source. Otherwise a prior ground pass could accumulate an offset.
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Research/MuckNative/GameObject/Chief.prefab");
        model.localPosition=original.transform.Find("newPlayer").localPosition;
        idle.SampleAnimation(animator.gameObject,0);
        pivot.localPosition=Vector3.zero;
        pivot.localScale=Vector3.one;
        var verts=SkinVertices(skin).Select(root.transform.InverseTransformPoint).ToArray();
        float bottom=verts.Min(v=>v.y),top=verts.Max(v=>v.y);
        float scale=2.4f/(top-bottom);
        pivot.localScale=Vector3.one*scale;
        pivot.localPosition=new Vector3(-(verts.Max(v=>v.x)+verts.Min(v=>v.x))*.5f*scale,-bottom*scale+.005f,-(verts.Max(v=>v.z)+verts.Min(v=>v.z))*.5f*scale);
        var sourceMesh=original.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="ChiefSpear").sharedMesh;
        var mesh=UnityEngine.Object.Instantiate(sourceMesh);
        var grip=spear.transform.InverseTransformPoint(spear.transform.parent.position);
        mesh.vertices=sourceMesh.vertices.Select(v=>grip+(v-grip)*.66f).ToArray();
        mesh.RecalculateBounds();mesh.name="Asta infernal";
        string meshPath=Art+"/Asta infernal.asset";
        var stored=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(stored){EditorUtility.CopySerialized(mesh,stored);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,meshPath);stored=mesh;}
        spear.sharedMesh=stored;

        float restY=model.localPosition.y;
        // Per-clip vertical compensation preserves airborne frames and only lifts penetrations.
        foreach(var clip in clips)
        {
            float[] xyz = {model.localPosition.x,restY,model.localPosition.z};
            var curve=new AnimationCurve();
            int count=Mathf.CeilToInt(clip.length*60);
            for(int i=0;i<=count;i++){
                float t=clip.length*i/count;
                model.localPosition=new Vector3(xyz[0],restY,xyz[2]);
                clip.SampleAnimation(animator.gameObject,t);
                float body=SkinVertices(skin).Min(v=>root.transform.InverseTransformPoint(v).y);
                float floor=Mathf.Min(body,stored.vertices.Min(v=>root.transform.InverseTransformPoint(spear.transform.TransformPoint(v)).y));
                curve.AddKey(t,restY+Mathf.Max(0,.005f-floor)/scale);
            }
            AnimationUtility.SetEditorCurve(clip,bind,curve);
            EditorUtility.SetDirty(clip);
        }
        idle.SampleAnimation(animator.gameObject,0);
    }
    static Material WardMaterial(Material source)
    {
        string name=source.name.Replace("Vigia_","");
        string path=Art+"/Vigia_"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
        string hex;float metal=.05f,smooth=.33f;string glow=null;
        switch(name){
          case "Skin": hex="786A63";break;
          case "Feather1":hex="702D32";break;
          case "Feather2":hex="C1B39E";break;
          case "Cloth":hex="433843";break;
          case "Gold":hex="A17F53";metal=.72f;smooth=.48f;break;
          case "Eyes":hex="EAA271";glow="C45327";break;
          case "ChiefSpear":hex="B3AAA2";metal=.66f;smooth=.45f;break;
          case "Hogoblin":hex="AFA19A";glow="CA7542";break;
          case "Guardian_Pink":hex="747E92";metal=.38f;glow="8D8ACB";break;
          case "SWordGoblin":hex="AA9790";metal=.65f;break;
          default:hex="ABA3A1";break;
        }
        ColorUtility.TryParseHtmlString("#"+hex,out var color);
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);
        if(glow!=null){ColorUtility.TryParseHtmlString("#"+glow,out var e);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",e*.85f);}
        EditorUtility.SetDirty(mat);return mat;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode");
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        foreach(var name in Names)
        {
            var path=Bosses+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{
                if(name==Names[0])FitChief(root);
                foreach(var r in root.GetComponent<BossCharacter>().visual.GetComponentsInChildren<Renderer>(true)){
                    if(!(r is SkinnedMeshRenderer)&&!(r is MeshRenderer))continue;
                    r.sharedMaterials=r.sharedMaterials.Select(m=>m?WardMaterial(m):null).ToArray();
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return "Three warden palettes saved. Chief fitted to 2.4m, spear rebalanced, 6 native clips grounded.";
    }
}
