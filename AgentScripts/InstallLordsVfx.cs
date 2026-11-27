using System;
using UnityEngine;
using UnityEditor;
public static class InstallLordsVfx
{
    const string Root="Assets/Resources/Presentation/LordsVfx/";
    public static string Run()
    {
        AssetDatabase.Refresh();var shader=Shader.Find("Cinders/Lords Attack Mask");if(!shader)throw new Exception("Missing Lords attack shader");
        foreach(var item in new[]{("Slash","SlashMask"),("Impact","AttackSpark"),("Core","SparkCore")})
        {
            string texturePath=Root+item.Item2+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
            string path=Root+item.Item1+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;material.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_Tint",Color.white);EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();return "Lords masks installed: slash, impact spark, cast core. Unity emitters; 32 effects maximum; no gameplay colliders or lights.";
    }
}
