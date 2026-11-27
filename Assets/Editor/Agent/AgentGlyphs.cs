using System.Collections.Generic;using System.IO;using TMPro;using UnityEditor;using UnityEngine;using UnityEngine.TextCore;
// Builds the TMP sprite asset of gamepad button glyphs used by GameInput prompts: <sprite="CindersButtons" name="ps_cross">.
public static class AgentGlyphs{
 const string Tex="Assets/06_UI/Glyphs/CindersButtons.png",Meta="Assets/06_UI/Glyphs/CindersButtons.json",Out="Assets/Resources/Sprite Assets/CindersButtons.asset";
 [System.Serializable] class Cell{public string name;public int x,y,w,h;} [System.Serializable] class Wrap{public Cell[] items;}
 [MenuItem("Codice/UI/Build gamepad glyphs")]
 public static string Build(){
  AssetDatabase.Refresh();
  var imp=(TextureImporter)AssetImporter.GetAtPath(Tex);imp.textureType=TextureImporterType.Default;imp.alphaIsTransparency=true;imp.mipmapEnabled=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Trilinear;imp.SaveAndReimport();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Tex);
  var cells=JsonUtility.FromJson<Wrap>("{\"items\":"+File.ReadAllText(Meta)+"}").items;
  Directory.CreateDirectory(Path.GetDirectoryName(Out));AssetDatabase.DeleteAsset(Out);
  var asset=ScriptableObject.CreateInstance<TMP_SpriteAsset>();asset.name="CindersButtons";AssetDatabase.CreateAsset(asset,Out);
  asset.spriteSheet=tex;
  var mat=new Material(Shader.Find("TextMeshPro/Sprite"));mat.name="CindersButtons Material";mat.SetTexture(ShaderUtilities.ID_MainTex,tex);AssetDatabase.AddObjectToAsset(mat,asset);asset.material=mat;
  var so=new SerializedObject(asset);so.FindProperty("m_Version").stringValue="1.1.0";so.ApplyModifiedPropertiesWithoutUndo();
  uint i=0;foreach(var c in cells){var g=new TMP_SpriteGlyph(i,new GlyphMetrics(c.w,c.h,0,c.h*.80f,c.w*1.02f),new GlyphRect(c.x,c.y,c.w,c.h),1,0);asset.spriteGlyphTable.Add(g);var ch=new TMP_SpriteCharacter(0xFFFE,asset,g){name=c.name,scale=1.4f};asset.spriteCharacterTable.Add(ch);i++;}
  asset.UpdateLookupTables();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
  var back=Resources.Load<TMP_SpriteAsset>("Sprite Assets/CindersButtons");
  return $"glyphs={asset.spriteCharacterTable.Count} loadable={(back!=null)} ps_cross={(back!=null?back.GetSpriteIndexFromName("ps_cross"):-9)}";}
}
