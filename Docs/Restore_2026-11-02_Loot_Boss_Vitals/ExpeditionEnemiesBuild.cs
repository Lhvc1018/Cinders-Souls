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
        var hound = BuildVisual("Hellhound", "hellhound_3d_model", 1.12f, 90f);
        var inquisitor = BuildVisual("HorrorInquisitor", "horror_inquisitor_3d_model", 2.8f, 180f);
        var root = new GameObject("Hellhound");
        var agent = root.AddComponent<NavMeshAgent>(); agent.radius=.42f; agent.height=1.12f;
        var enemy = root.AddComponent<BasicEnemy>(); enemy.kind=EnemyKind.Swordsman; enemy.variantName="SABUESO INFERNAL";
        enemy.healthMultiplier=1.15f; enemy.speedMultiplier=.9f; enemy.damageMultiplier=1; enemy.useNativeAnimator=true;enemy.lungingHound=true;
        var capsule=root.GetComponent<CapsuleCollider>();capsule.center=new Vector3(0,.56f,0);capsule.height=1.12f;capsule.radius=.42f;
        hound.transform.SetParent(root.transform,false); enemy.animator=hound.GetComponent<Animator>();
        AddLight(root.transform,1.3f,.65f,2.8f,1.4f);
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
        Debug.Log("[Expedition] Built original Ashen Hound and Horror Inquisitor with baked skeletal clips.");
    }
    static GameObject BuildVisual(string name,string texturePrefix,float height,float rotation)
    {
        bool isHound=name=="Hellhound";
        string sourceName=isHound?"AshenHound":name;
        string folder="Assets/03_Models/Enemies/"+sourceName;
        string path=folder+"/"+sourceName+"_rigged.fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new Exception("FBX missing or not imported: "+path);
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;
        var clips=importer.defaultClipAnimations;
        if(clips.Length==0)throw new Exception("FBX has no animation takes: "+path);
        foreach(var clip in clips){clip.loopTime=clip.name.EndsWith("Idle",StringComparison.OrdinalIgnoreCase)||clip.name.EndsWith("Chase",StringComparison.OrdinalIgnoreCase)||clip.name.EndsWith("Walk",StringComparison.OrdinalIgnoreCase)||clip.name.EndsWith("Stun",StringComparison.OrdinalIgnoreCase);}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!model)throw new Exception("FBX model missing: "+path);
        var animations=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        string controllerPath=Out+"/"+name+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        foreach(var old in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(old.state);
        controller.parameters=Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        controller.AddParameter("LocomotionSpeed",AnimatorControllerParameterType.Float);
        var parameters=controller.parameters;foreach(var p in parameters)if(p.type==AnimatorControllerParameterType.Float)p.defaultFloat=1;controller.parameters=parameters;
        foreach(var state in isHound?new[]{"Idle","Chase","Patrol","Melee","Shoot","Slam","Jump","Hit","Stun","Death"}:new[]{"Idle","Chase","Patrol","Melee","Shoot","Slam","Hit","Stun","Death"})
        {
            string source=state=="Patrol"?(isHound?"Walk":"Chase"):state=="Shoot"?"Melee":state;
            var clip=animations.FirstOrDefault(c=>c.name.EndsWith(source,StringComparison.OrdinalIgnoreCase));
            if(!clip)throw new Exception("Missing "+source+" in "+path+"; available: "+string.Join(", ",animations.Select(c=>c.name)));
            var s=controller.layers[0].stateMachine.AddState(state);s.motion=clip;
            if(state=="Melee"||state=="Shoot"||state=="Slam"||state=="Hit"||state=="Jump") { s.speedParameter="ActionSpeed";s.speedParameterActive=true; }
            if(state=="Chase"||state=="Patrol"){s.speedParameter="LocomotionSpeed";s.speedParameterActive=true;}
            if(state=="Idle")controller.layers[0].stateMachine.defaultState=s;
        }
        var instance=Object.Instantiate(model);instance.name=name+" visual";
        var animator=instance.GetComponent<Animator>();if(!animator)animator=instance.AddComponent<Animator>();
        animator.avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        if(!animator.avatar || !animator.avatar.isValid)throw new Exception("Missing valid generic Avatar: "+path);
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
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
        if(isHound)
        {
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>HoundMaterial(m?m.name:"AshenSkin",folder)).ToArray();
            return instance;
        }
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
    static Material HoundMaterial(string source,string folder)
    {
        string name=new[]{"AshenSkin","WeatheredBone","DriedWound","BlackClaw","FeralEye","RaggedHide"}.FirstOrDefault(n=>source.Contains(n))??"AshenSkin";
        string path=Out+"/AshenHound_"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        var color=name=="WeatheredBone"?new Color(.93f,.76f,.43f):name=="DriedWound"?new Color(.52f,.045f,.028f):name=="BlackClaw"?new Color(.065f,.025f,.015f):name=="FeralEye"?new Color(1,.26f,.025f):name=="RaggedHide"?new Color(.34f,.11f,.035f):new Color(1,.43f,.17f);
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",name=="FeralEye"?.4f:.13f);material.SetFloat("_Metallic",0);
        material.SetTexture("_BaseMap",name=="AshenSkin"?AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/AshenHound_Skin.png"):null);
        if(name=="FeralEye"){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.85f,.12f,.01f));}else material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);return material;
    }
    static void AddLight(Transform parent,float y,float z,float range,float intensity)
    {
        var lamp=new GameObject("Enemy readability light").AddComponent<Light>();
        lamp.transform.SetParent(parent,false);lamp.transform.localPosition=new Vector3(0,y,z);
        lamp.type=LightType.Point;lamp.range=range;lamp.intensity=intensity;lamp.color=new Color(1f,.75f,.5f);lamp.shadows=LightShadows.None;
    }
}
