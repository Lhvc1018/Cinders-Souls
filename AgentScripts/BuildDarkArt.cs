using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.Linq;
public static class BuildDarkArt
{
 public static string Run()
 {
  AssetDatabase.Refresh();
  foreach(var name in new[]{"IMFellEnglish","IMFellEnglishSC"})
  {
   string path="Assets/Resources/CindersFonts/"+name+".asset";
   if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path))continue;
   var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/IMFell/"+name+".ttf");
   if(!font)throw new System.Exception("Missing font "+name);
   var f=TMP_FontAsset.CreateFontAsset(font,80,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);f.name=name;
   string chars=new string(Enumerable.Range(32,224).Select(x=>(char)x).ToArray())+"–—‘’“”•…→←↑↓◆◇";
   f.TryAddCharacters(chars,out string missing);f.atlasPopulationMode=AtlasPopulationMode.Static;
   f.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{Resources.Load<TMP_FontAsset>("CindersFonts/CrimsonText")};
   AssetDatabase.CreateAsset(f,path);AssetDatabase.AddObjectToAsset(f.material,f);
   foreach(var tex in f.atlasTextures)AssetDatabase.AddObjectToAsset(tex,f);EditorUtility.SetDirty(f);
  }
  var shader=Shader.Find("Cinders/Weathered Stone");if(!shader||!shader.isSupported)throw new System.Exception("Stone shader unsupported");
  foreach(var name in new[]{"Wall","Floor"})
  {
   string root="Assets/Resources/CindersStone/",path=root+name+".mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;
   foreach(var map in new[]{"Diffuse","nor_gl","Rough"})
   {
    string file=System.IO.Directory.GetFiles(root,name+"_"+map+".*").First(x=>!x.EndsWith(".meta"));
    var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=2048;importer.anisoLevel=4;importer.mipmapEnabled=true;
    importer.sRGBTexture=map=="Diffuse";if(map=="nor_gl")importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();
    mat.SetTexture(map=="Diffuse"?"_BaseMap":map=="nor_gl"?"_NormalMap":"_RoughMap",AssetDatabase.LoadAssetAtPath<Texture2D>(file));
   }
   mat.SetColor("_BaseColor",name=="Wall"?new Color(.72f,.73f,.7f):new Color(.58f,.6f,.59f));mat.SetFloat("_Scale",name=="Wall"?.32f:.38f);mat.SetFloat("_NormalStrength",.65f);mat.SetFloat("_Damp",name=="Wall"?.25f:.4f);EditorUtility.SetDirty(mat);
  }
  var urp=UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;if(!urp)throw new System.Exception("URP missing");urp.supportsHDR=true;EditorUtility.SetDirty(urp);
  AssetDatabase.SaveAssets();return "Two static Spanish font atlases and two photographic triplanar stone materials ready";
 }
}
