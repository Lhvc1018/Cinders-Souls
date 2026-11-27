using System;
using System.IO;
using System.Linq;
using Codice.Bosses;
using Codice.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Explicit editor command. The original imported models and existing encounters stay intact.
public static class ExpeditionEnemiesBuild
{
    const string Out = "Assets/Resources/ExpeditionEnemies";
    [MenuItem("Tools/Cinders/Build Expedition Enemies")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode before building enemy assets.");
        Directory.CreateDirectory(Out); AssetDatabase.Refresh();
        var hound = BuildVisual("Hellhound", "hellhound_3d_model", 1.4f, -90f);
        var inquisitor = BuildVisual("HorrorInquisitor", "horror_inquisitor_3d_model", 2.8f, 180f);
        var root = new GameObject("Hellhound");
        var agent = root.AddComponent<NavMeshAgent>(); agent.radius=.48f; agent.height=1.35f;
        var enemy = root.AddComponent<BasicEnemy>(); enemy.kind=EnemyKind.Swordsman; enemy.variantName="SABUESO INFERNAL";
        enemy.healthMultiplier=1.15f; enemy.speedMultiplier=1.15f; enemy.damageMultiplier=1; enemy.useNativeAnimator=true;
        var capsule=root.GetComponent<CapsuleCollider>();capsule.center=new Vector3(0,.7f,0);capsule.height=1.4f;capsule.radius=.45f;
        hound.transform.SetParent(root.transform,false); enemy.animator=hound.GetComponent<Animator>();
        AddLight(root.transform,1f,-.8f,5f,2.5f);
        PrefabUtility.SaveAsPrefabAsset(root,Out+"/Hellhound.prefab");Object.DestroyImmediate(root);

        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Bosses/GoblinKing.prefab");
        if(!source)throw new Exception("Original boss prefab missing");
        root=Object.Instantiate(source);root.name="HorrorInquisitor";
        var boss=root.GetComponent<BossCharacter>();
        if(!boss)throw new Exception("BossCharacter missing");
        if(boss.meleeWeapon)Object.DestroyImmediate(boss.meleeWeapon);
        if(boss.bowWeapon)Object.DestroyImmediate(boss.bowWeapon);
        if(boss.visual)Object.DestroyImmediate(boss.visual.gameObject);
        boss.meleeWeapon=null;boss.bowWeapon=null;boss.summons=Array.Empty<BasicEnemy>();
        boss.kind=BossKind.DemonLord;boss.baseHealth=1200;boss.useNativeAnimator=true;boss.restrictToMelee=true;
        inquisitor.transform.SetParent(root.transform,false);boss.visual=inquisitor.transform;
        var bossCollider=root.GetComponent<CapsuleCollider>();bossCollider.center=new Vector3(0,1.35f,0);bossCollider.height=2.7f;bossCollider.radius=.55f;
        AddLight(root.transform,2f,-1f,6f,2f);
        PrefabUtility.SaveAsPrefabAsset(root,Out+"/HorrorInquisitor.prefab");Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("[Expedition] Built Hellhound and Horror Inquisitor prefabs with provisional skeleton animations.");
    }
    static GameObject BuildVisual(string name,string texturePrefix,float height,float rotation)
    {
        string folder="Assets/03_Models/Enemies/"+name;
        string path=folder+"/"+name+"_rigged.fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new Exception("FBX missing or not imported: "+path);
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;
        var clips=importer.defaultClipAnimations;
        if(clips.Length==0)throw new Exception("FBX has no animation takes: "+path);
        foreach(var clip in clips){clip.loopTime=clip.name.EndsWith("Idle",StringComparison.OrdinalIgnoreCase)||clip.name.EndsWith("Chase",StringComparison.OrdinalIgnoreCase);}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!model)throw new Exception("FBX model missing: "+path);
        var animations=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        string controllerPath=Out+"/"+name+".controller";
        AssetDatabase.DeleteAsset(controllerPath);
        var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        foreach(var state in new[]{"Idle","Chase","Patrol","Melee","Shoot","Slam","Hit","Death"})
        {
            string source=state=="Patrol"?"Chase":state=="Shoot"||state=="Slam"?"Melee":state;
            var clip=animations.FirstOrDefault(c=>c.name.EndsWith(source,StringComparison.OrdinalIgnoreCase));
            if(!clip)throw new Exception("Missing "+source+" in "+path+"; available: "+string.Join(", ",animations.Select(c=>c.name)));
            var s=controller.layers[0].stateMachine.AddState(state);s.motion=clip;
            if(state=="Melee"||state=="Shoot"||state=="Slam"||state=="Hit") { s.speedParameter="ActionSpeed";s.speedParameterActive=true; }
            if(state=="Idle")controller.layers[0].stateMachine.defaultState=s;
        }
        var instance=Object.Instantiate(model);instance.name=name+" visual";
        var animator=instance.GetComponent<Animator>();if(!animator)animator=instance.AddComponent<Animator>();
        animator.avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        if(!animator.avatar || !animator.avatar.isValid)throw new Exception("Missing valid generic Avatar: "+path);
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        var renderers=instance.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0)throw new Exception("Model contains no renderers: "+path);
        // Measure posed vertices, not conservative animation bounds.
        Bounds bounds=new Bounds();bool hasBounds=false;
        foreach(var renderer in renderers)
        {
            if(renderer is SkinnedMeshRenderer skin)
            {
                foreach(var vertex in skin.sharedMesh.vertices){var point=skin.transform.TransformPoint(vertex);if(!hasBounds){bounds=new Bounds(point,Vector3.zero);hasBounds=true;}else bounds.Encapsulate(point);}
            }
            else {if(!hasBounds){bounds=renderer.bounds;hasBounds=true;}else bounds.Encapsulate(renderer.bounds);}
        }
        if(bounds.size.y<.0001f)throw new Exception("Model has zero height: "+path);
        // Imported skinned bounds include the unposed armature. Adjust visible mesh size in the dungeon.
        float scale=height/bounds.size.y;instance.transform.localScale*=scale;
        instance.transform.localPosition=Vector3.up*(-bounds.min.y*scale);
        instance.transform.localRotation=Quaternion.Euler(0,rotation,0);
        string matPath=Out+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+texturePrefix+"_basecolor.jpg");
        if(tex)mat.SetTexture("_BaseMap",tex);
        mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.25f);mat.SetFloat("_Metallic",.15f);
        string normalPath=folder+"/"+texturePrefix+"_normal."+(name=="Hellhound"?"jpg":"png");
        var normalImporter=AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if(normalImporter){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mat.SetFloat("_BumpScale",.7f);mat.EnableKeyword("_NORMALMAP");}
        mat.DisableKeyword("_EMISSION");
        foreach(var renderer in renderers)renderer.sharedMaterials=Enumerable.Repeat(mat,renderer.sharedMaterials.Length).ToArray();
        return instance;
    }
    static void AddLight(Transform parent,float y,float z,float range,float intensity)
    {
        var lamp=new GameObject("Enemy readability light").AddComponent<Light>();
        lamp.transform.SetParent(parent,false);lamp.transform.localPosition=new Vector3(0,y,z);
        lamp.type=LightType.Point;lamp.range=range;lamp.intensity=intensity;lamp.color=new Color(1f,.75f,.5f);lamp.shadows=LightShadows.None;
    }
}
