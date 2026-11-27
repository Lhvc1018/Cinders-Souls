using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using Codice.Enemies;
using Object=UnityEngine.Object;
public static class BuildCryptResidents
{
    const string Root="Assets/Resources/ExpeditionEnemies";
    const string Skeleton="Assets/03_Models/DungeonCharacters/Skeletons_demo/";
    static AnimationClip Source(string path,string suffix=null)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview")&&(suffix==null||c.name.EndsWith(suffix)));
    static AnimationClip SaveClip(AnimationClip clip,string name,bool loop)
    {
        clip.name=name;AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        string path=Root+"/CryptClips/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing){EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    static Material Material(string name,string texture,Color tint,float metallic)
    {
        string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(m);return m;
    }
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play mode");
        Directory.CreateDirectory(Root+"/CryptClips");AssetDatabase.Refresh();
        Build(true);Build(false);AssetDatabase.SaveAssets();return "Creepy Doll and Dungeon Skeleton integrated with native rigs, custom materials, complete combat states and measured scale.";
    }
    static void Build(bool doll)
    {
        string name=doll?"CreepyDoll":"CryptSkeleton";string modelPath=doll?"Assets/03_Models/CreepyDoll/CreepyDoll.fbx":Skeleton+"models/DungeonSkeleton_demo.FBX";
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();
        var root=new GameObject(name);var visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),root.transform);visual.name="Visual";
        var animator=visual.GetComponent<Animator>()??visual.AddComponent<Animator>();animator.avatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();animator.enabled=false;
        var idle=Source(doll?modelPath:Skeleton+"animation/DS_onehand_idle_A.FBX",doll?"Idle":null);
        var walk=Source(doll?modelPath:Skeleton+"animation/DS_onehand_walk.FBX",doll?"Walk":null);
        idle.SampleAnimation(visual,0);
        Bounds bounds=new Bounds();bool first=true;
        foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=new Mesh();r.BakeMesh(mesh,false);var matrix=Matrix4x4.TRS(r.transform.position,r.transform.rotation,Vector3.one);
            foreach(var v in mesh.vertices){var world=matrix.MultiplyPoint3x4(v);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}
            Object.DestroyImmediate(mesh);
        }
        float height=doll?1.45f:2.15f;float scale=height/bounds.size.y;visual.transform.localScale*=scale;visual.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
        foreach(var r in visual.GetComponentsInChildren<Renderer>())
        {
            if(doll)r.sharedMaterials=Enumerable.Repeat(Material("Doll_BurgundyPorcelain","Assets/03_Models/CreepyDoll/Textures/CreepyDoll_Rotten.png",new Color(.82f,.58f,.52f),.03f),r.sharedMaterials.Length).ToArray();
            else r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.Contains("Equipment")?Material("Crypt_RustedEquipment",Skeleton+"models/Materials/DemoEquipment.tga",new Color(.63f,.29f,.14f),.55f):Material("Crypt_AgedIvory",Skeleton+"models/Materials/DemoSkeleton.png",new Color(.83f,.72f,.45f),.08f)).ToArray();
        }
        var states=new Dictionary<string,AnimationClip>();
        states["Idle"]=SaveClip(Object.Instantiate(idle),name+"Idle",true);states["Chase"]=SaveClip(Object.Instantiate(walk),name+"Chase",true);states["Patrol"]=states["Chase"];
        foreach(string action in new[]{"Melee","Hit","Stun","Death"})states[action]=!doll&&action=="Melee"?SaveClip(Object.Instantiate(Source(Skeleton+"animation/DS_onehand_attack_A.FBX")),name+action,false):Author(visual,idle,name,action,doll);
        states["Shoot"]=states["Melee"];states["Slam"]=states["Melee"];
        string controllerPath=Root+"/"+name+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        foreach(var s in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(s.state);
        controller.parameters=Array.Empty<AnimatorControllerParameter>();
        foreach(string parameter in new[]{"Speed","ActionSpeed","LocomotionSpeed"})controller.AddParameter(new AnimatorControllerParameter{name=parameter,type=AnimatorControllerParameterType.Float,defaultFloat=1});
        foreach(var pair in states)
        {
            var s=controller.layers[0].stateMachine.AddState(pair.Key);s.motion=pair.Value;
            if(pair.Key=="Chase"||pair.Key=="Patrol"){s.speedParameter="LocomotionSpeed";s.speedParameterActive=true;}
            if(pair.Key=="Melee"||pair.Key=="Hit"||pair.Key=="Slam"||pair.Key=="Shoot"){s.speedParameter="ActionSpeed";s.speedParameterActive=true;}
            if(pair.Key=="Idle")controller.layers[0].stateMachine.defaultState=s;
        }
        idle.SampleAnimation(visual,0);animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;
        var enemy=root.AddComponent<BasicEnemy>();enemy.kind=EnemyKind.Swordsman;enemy.animator=animator;enemy.useNativeAnimator=true;enemy.lungingHound=false;enemy.miniboss=false;
        enemy.variantName=doll?"MUÑECA DEL LUTO":"CUSTODIO OSARIO";enemy.healthMultiplier=doll?.8f:1.4f;enemy.speedMultiplier=doll?.62f:.53f;enemy.damageMultiplier=doll?1.1f:1.45f;
        enemy.nativeWindup=doll?.65f:.8f;enemy.locomotionMetersPerSecond=doll?1.2f:1.4f;
        var agent=root.GetComponent<NavMeshAgent>();agent.height=height;agent.radius=doll?.3f:.4f;
        var capsule=root.GetComponent<CapsuleCollider>();capsule.height=height;capsule.radius=agent.radius;capsule.center=Vector3.up*height*.5f;
        var light=new GameObject("Warm readability").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.transform.localPosition=new Vector3(0,height*.7f,.65f);light.type=LightType.Point;light.intensity=.45f;light.range=2.5f;light.color=new Color(.9f,.8f,.65f);
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+name+".prefab");Object.DestroyImmediate(root);
    }
    static AnimationClip Author(GameObject visual,AnimationClip idle,string name,string action,bool doll)
    {
        idle.SampleAnimation(visual,0);var bones=visual.GetComponentsInChildren<Transform>().Where(t=>t!=visual.transform).ToArray();
        var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
        var curves=new Dictionary<string,AnimationCurve>();var clip=new AnimationClip{frameRate=30};
        float duration=action=="Melee"?.975f:action=="Death"?1.2f:action=="Hit"?.3f:.9f;int count=Mathf.RoundToInt(duration*30);
        Transform Find(string n)=>bones.First(t=>t.name==n);
        void Rotate(string n,Vector3 euler){var t=Find(n);t.rotation=visual.transform.rotation*Quaternion.Euler(euler)*Quaternion.Inverse(visual.transform.rotation)*t.rotation;}
        for(int frame=0;frame<=count;frame++)
        {
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
            float t=frame/(float)count;string chest=doll?"Bone.001":"Bip001 Spine1",head=doll?"Bone.002":"Bip001 Head";
            if(action=="Melee")
            {
                float wind=Mathf.Sin(Mathf.Clamp01(t/.42f)*Mathf.PI/2),strike=t<.42f?0:Mathf.Sin(Mathf.Clamp01((t-.42f)/.58f)*Mathf.PI);
                Rotate(chest,new Vector3(-10*wind+25*strike,0,12*strike));Rotate(head,new Vector3(-12*strike,0,-10*wind));
                Rotate("Bone.001_L.001",new Vector3(-45*wind+70*strike,-10*strike,-20*wind));Rotate("Bone.001_R.001",new Vector3(-35*wind+75*strike,15*strike,20*wind));
                Rotate("Bone.001_L.002",new Vector3(-25*wind+30*strike,0,0));Rotate("Bone.001_R.002",new Vector3(-20*wind+25*strike,0,0));
            }
            else if(action=="Hit"){float f=Mathf.Sin(t*Mathf.PI);Rotate(chest,new Vector3(-18*f,0,7*f));Rotate(head,new Vector3(-12*f,0,-8*f));}
            else if(action=="Stun"){Rotate(chest,new Vector3(12,0,2*Mathf.Sin(t*Mathf.PI*2)));Rotate(head,new Vector3(18,0,7*Mathf.Sin(t*Mathf.PI*2)));}
            else
            {
                float f=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.75f));string root=doll?"Armature":"Bip001";
                Rotate(root,doll?new Vector3(86*f,0,12*f):new Vector3(8*f,0,-92*f));Rotate(chest,new Vector3(15*f,0,0));Rotate(head,new Vector3(-15*f,0,15*f));
                if(!doll)
                {
                    // Let the limbs and held weapon settle sideways instead of propping up the corpse.
                    void Flatten(Transform joint,Vector3 direction)
                    {
                        var flat=Vector3.ProjectOnPlane(direction,Vector3.up);
                        if(flat.sqrMagnitude>.0001f)joint.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(direction,flat),f)*joint.rotation;
                    }
                    Flatten(Find(root),Find(head).position-Find("Bip001 Pelvis").position);
                    foreach(string side in new[]{"L","R"})
                    {
                        foreach(var segment in new[]{("Thigh","Calf"),("Calf","Foot"),("UpperArm","Forearm"),("Forearm","Hand")})
                        {var joint=Find("Bip001 "+side+" "+segment.Item1);Flatten(joint,Find("Bip001 "+side+" "+segment.Item2).position-joint.position);}
                        var hand=Find("Bip001 "+side+" Hand");Vector3 tip=Vector3.zero;
                        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            int bone=Array.IndexOf(skin.bones,hand);if(bone<0)continue;
                            var mesh=new Mesh();skin.BakeMesh(mesh,false);var vertices=mesh.vertices;var weights=skin.sharedMesh.boneWeights;
                            var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                            for(int v=0;v<vertices.Length;v++)if(weights[v].boneIndex0==bone&&weights[v].weight0>.6f)
                            {var delta=matrix.MultiplyPoint3x4(vertices[v])-hand.position;if(delta.sqrMagnitude>tip.sqrMagnitude)tip=delta;}
                            Object.DestroyImmediate(mesh);
                        }
                        Flatten(hand,tip);
                    }
                }
                var r=Find(root);r.position+=visual.transform.up*(-.05f*f);
                // Clamp the sampled deformed body above ground, preserving the death pose.
                float min=float.MaxValue;
                foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh,false);var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                    foreach(var v in mesh.vertices)min=Mathf.Min(min,matrix.MultiplyPoint3x4(v).y);Object.DestroyImmediate(mesh);
                }
                r.position+=Vector3.up*(.005f-min)*(min<.005f?1:f);
            }
            for(int i=0;i<bones.Length;i++)
            {
                string path=AnimationUtility.CalculateTransformPath(bones[i],visual.transform);var p=bones[i].localPosition;var q=bones[i].localRotation;
                float[] values={p.x,p.y,p.z,q.x,q.y,q.z,q.w};string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
                for(int j=0;j<7;j++){string key=path+"|"+props[j];if(!curves.TryGetValue(key,out var curve)){curve=new AnimationCurve();curves.Add(key,curve);}curve.AddKey(duration*t,values[j]);}
            }
        }
        foreach(var pair in curves){int cut=pair.Key.LastIndexOf('|');clip.SetCurve(pair.Key.Substring(0,cut),typeof(Transform),pair.Key.Substring(cut+1),pair.Value);}
        clip.EnsureQuaternionContinuity();return SaveClip(clip,name+action,action=="Stun");
    }
}
