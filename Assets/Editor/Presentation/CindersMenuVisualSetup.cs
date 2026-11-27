using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class CindersMenuVisualSetup
{
    const string Output = "Assets/Resources/CindersMenu";
    const string Adventurers = "Assets/03_Models/Characters/KayKit_Adventurers_2.0_FREE/";
    const string Doll = "Assets/03_Models/CreepyDoll/";

    [MenuItem("Cinders Souls/Build menu models and font")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        AssetDatabase.Refresh();
        BuildModel("Class_Warrior", Adventurers + "Characters/fbx/Knight.fbx", Adventurers + "Textures/knight_texture.png", 1.9f, true);
        BuildModel("Class_Archer", Adventurers + "Characters/fbx/Ranger.fbx", Adventurers + "Textures/ranger_texture.png", 1.9f, true);
        BuildModel("Class_Tank", Adventurers + "Characters/fbx/Barbarian.fbx", Adventurers + "Textures/barbarian_texture.png", 1.9f, true);
        BuildModel("Class_Mage", Adventurers + "Characters/fbx/Mage.fbx", Adventurers + "Textures/mage_texture.png", 1.9f, true);
        BuildModel("Class_Shinobi", Adventurers + "Characters/fbx/Rogue_Hooded.fbx", Adventurers + "Textures/rogue_texture.png", 1.9f, true);
        BuildModel("CreepyDoll_Fresh", Doll + "CreepyDoll.fbx", Doll + "Textures/CreepyDoll_Fresh.png", 2.1f, false);
        BuildModel("CreepyDoll_Rotten", Doll + "CreepyDoll.fbx", Doll + "Textures/CreepyDoll_Rotten.png", 2.1f, false);
        BuildFont();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CINDERS_MENU_VISUALS_READY");
    }

    static void BuildModel(string name, string modelPath, string texturePath, float height, bool animate)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (!source || !texture) throw new InvalidOperationException("Missing menu model or texture: " + modelPath + " / " + texturePath);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
        string materialPath = Output + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);

        var root = new GameObject(name);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(root.transform, false);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Model has no renderers: " + modelPath);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = height / Mathf.Max(.01f, bounds.size.y);
            model.transform.localScale = Vector3.one * scale;
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            if (animate)
            {
                var animator = model.GetComponent<Animator>();
                if (!animator) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/01_Prefabs/Characters/CharacterIdle.controller");
                animator.applyRootMotion = false;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void BuildFont()
    {
        string output = Output + "/Cinzel.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(output);
        if (existing && existing.atlasTexture) return;
        if (existing) AssetDatabase.DeleteAsset(output);
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Presentation/Fonts/Cinzel.ttf");
        if (!source) throw new InvalidOperationException("Cinzel.ttf was not imported.");
        var asset = TMP_FontAsset.CreateFontAsset(source);
        asset.name = "Cinzel";
        AssetDatabase.CreateAsset(asset, output);
        if (asset.atlasTexture && !AssetDatabase.Contains(asset.atlasTexture)) AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
        if (asset.material && !AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material, asset);
    }
}
