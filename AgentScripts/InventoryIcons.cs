using UnityEditor;
using UnityEngine;
using System.IO;
public static class InventoryIcons
{
 public static string Build()
 {
  string output="Assets/Resources/CindersIcons";Directory.CreateDirectory(output);
  foreach(string name in new[]{"EstusBottle","Passive0","Passive1","Passive2","Passive3","Passive4","RelicToken","ManaShard"})
  {
   var source=Resources.Load<GameObject>("CindersDungeon/"+name);var root=Object.Instantiate(source);root.transform.position=new Vector3(0,-1500,0);root.SetActive(true);
   foreach(var c in root.GetComponentsInChildren<Collider>())c.enabled=false;
   foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
   var renderers=root.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   var go=new GameObject("Icon camera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y)*.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<29;
   camera.transform.position=bounds.center+new Vector3(1,.5f,-3).normalized*5;camera.transform.LookAt(bounds.center);
   var lightGo=new GameObject("Icon light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<29;lightGo.transform.rotation=Quaternion.Euler(35,-30,0);
   var target=new RenderTexture(256,256,24);camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();string path=output+"/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;
   Object.DestroyImmediate(root);Object.DestroyImmediate(go);Object.DestroyImmediate(lightGo);Object.DestroyImmediate(texture);Object.DestroyImmediate(target);
   AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.SaveAndReimport();
  }
  return "8 inventory model icons rendered";
 }
}
