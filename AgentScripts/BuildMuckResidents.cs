using System;
using System.IO;
using System.Linq;
using Codice.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public static class BuildMuckResidents
{
    const string Source = "Assets/Research/MuckNative/";
    const string Folder = "Assets/Resources/ExpeditionEnemies/MuckResidents/";
    static Material Patina(Material original, string species, Color tint)
    {
        string path = Folder + species + "-" + (original ? original.name : "surface") + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        var props = original ? new SerializedObject(original).FindProperty("m_SavedProperties.m_TexEnvs") : null;
        if (props != null)
            for (int i = 0; i < props.arraySize; ++i)
            {
                var entry = props.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == "_MainTex")
                    m.SetTexture("_BaseMap", entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture);
            }
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", species == "StoneGolem" ? .32f : .16f);
        m.SetFloat("_Metallic", species == "StoneGolem" ? .35f : .06f);
        EditorUtility.SetDirty(m);
        return m;
    }
    static AnimationClip LoadClip(string species, string sourceName, bool loop)
    {
        string src = Source + "AnimationClip/" + sourceName + ".anim";
        string path = Folder + species + "_" + sourceName + ".anim";
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(src);
        if (!source || AnimationUtility.GetCurveBindings(source).Length == 0) throw new Exception("No curves: " + src);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (!clip)
        {
            clip = UnityEngine.Object.Instantiate(source);
            AssetDatabase.CreateAsset(clip, path);
        }
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }
    static void Build(string species, string title, string idleName, string walkName, string strikeName, float height, float speed, float damage, EnemyKind kind, Color tint)
    {
        var root = new GameObject(title);
        try
        {
            var pivot = new GameObject("Visual").transform;
            pivot.SetParent(root.transform, false);
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "GameObject/" + species + ".prefab"),pivot);
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            var animator = model.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            string path = Folder + species + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            foreach (string param in new[]{"ActionSpeed","LocomotionSpeed"})
                controller.AddParameter(new AnimatorControllerParameter{name=param,type=AnimatorControllerParameterType.Float,defaultFloat=1});
            var idle = LoadClip(species, idleName, true);
            var walk = LoadClip(species, walkName, true);
            var attack = LoadClip(species, strikeName, false);
            foreach (var stateName in new[]{"Idle","BlockIdle","Chase","Patrol","Melee","HeavyAttack","Stun","Hit","Death"})
            {
                var state = machine.AddState(stateName);
                bool move = stateName == "Chase" || stateName == "Patrol";
                bool strike = stateName == "Melee" || stateName == "HeavyAttack";
                state.motion = move ? walk : strike ? attack : idle;
                if (move || strike) {state.speedParameterActive = true;state.speedParameter = move ? "LocomotionSpeed" : "ActionSpeed";}
                if (stateName == "Idle") machine.defaultState = state;
                if (stateName == "Death" || stateName == "Hit" || stateName == "Stun") state.speed = 0;
            }
            animator.runtimeAnimatorController = controller;
            idle.SampleAnimation(animator.gameObject, 0);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Patina(m,species,tint)).ToArray();
            var mesh = new Mesh();
            var bounds = new Bounds();
            bool first = true;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.BakeMesh(mesh);
                foreach(var vertex in mesh.vertices)
                {
                    Vector3 p = skin.transform.TransformPoint(vertex);
                    if(first) {bounds = new Bounds(p,Vector3.zero);first=false;} else bounds.Encapsulate(p);
                }
                skin.updateWhenOffscreen = true;
            }
            UnityEngine.Object.DestroyImmediate(mesh);
            if(first || bounds.size.y < .01f) throw new Exception("Invalid pose bounds " + species);
            float scale = height / bounds.size.y;
            pivot.localScale = Vector3.one * scale;
            pivot.localPosition = -scale * new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            var enemy = root.AddComponent<BasicEnemy>();
            enemy.animator = animator;
            enemy.useNativeAnimator = true;
            enemy.kind = kind;
            enemy.healthMultiplier = species == "Goblin" ? .72f : species == "Wolf" ? .9f : .85f;
            enemy.speedMultiplier = speed;
            enemy.damageMultiplier = damage;
            enemy.variantName = title;
            enemy.nativeWindup = Mathf.Clamp(attack.length * .55f,.3f,1.05f);
            enemy.locomotionMetersPerSecond = (kind == EnemyKind.Heavy ? 1.8f : 4.2f) * speed;
            var collider = root.GetComponent<CapsuleCollider>();
            collider.height = height;
            collider.radius = Mathf.Min(height*.45f, species=="StoneGolem"?.8f:.43f);
            collider.center = Vector3.up * height*.5f;
            var agent = root.GetComponent<NavMeshAgent>();
            agent.height = height; agent.radius = collider.radius; agent.baseOffset = 0;
            agent.acceleration = 14; agent.angularSpeed = 420;
            PrefabUtility.SaveAsPrefabAsset(root, Folder + title + ".prefab");
        }
        finally {UnityEngine.Object.DestroyImmediate(root);}
    }
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Requires edit mode");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Build("Wolf","Lobo de hollin","Wolf_Idle","WolfRun","WolfAttack",1.12f,1.25f,.84f,EnemyKind.Swordsman,new Color(.48f,.46f,.53f));
        Build("Goblin","Salteador de la cripta","Idle_0","Walk_0","GoblinMelee",1.48f,.96f,1.08f,EnemyKind.Swordsman,new Color(.57f,.51f,.38f));
        Build("StoneGolem","Golem funerario","Idle_3","Walk_1","Attack",2.36f,.83f,.98f,EnemyKind.Heavy,new Color(.39f,.48f,.55f));
        AssetDatabase.SaveAssets();
        return "Built Wolf, Goblin, StoneGolem as ordinary enemies with nine native clips.";
    }
    public static string RegisterLab()
    {
        var room = UnityEngine.Object.FindFirstObjectByType<Codice.Mechanics.CombatTestRoom>();
        if (Application.isPlaying || !room || room.gameObject.scene.name != "LaboratorioContenido")
            throw new Exception("Open LaboratorioContenido in edit mode");
        var imports = AssetDatabase.FindAssets("t:Prefab",new[]{Folder.TrimEnd('/')})
            .Select(id => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id)))
            .Where(p => p && p.GetComponent<BasicEnemy>()).ToArray();
        if(imports.Length!=3)throw new Exception("Expected three ordinary enemies");
        room.enemyPrefabs = room.enemyPrefabs.Concat(imports).Distinct().ToArray();
        EditorUtility.SetDirty(room);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
        if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(room.gameObject.scene))throw new Exception("Could not save lab");
        return "Registered " + string.Join(", ",imports.Select(p=>p.name)) + " in laboratory.";
    }
}
