using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class ValidateDarkArt
{
 public static string Assets()
 {
  var shader=Shader.Find("Cinders/Weathered Stone");var errors=ShaderUtil.GetShaderMessages(shader).Where(x=>x.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
  if(errors.Length>0)throw new Exception(string.Join(" | ",errors.Select(x=>x.message)));
  foreach(var name in new[]{"IMFellEnglish","IMFellEnglishSC"}){var f=Resources.Load<TMP_FontAsset>("CindersFonts/"+name);if(!f||!f.HasCharacters("ÁÉÍÓÚáéíóúñÑ¿¡0123456789"))throw new Exception("Missing Spanish glyphs "+name);if(f.atlasPopulationMode!=AtlasPopulationMode.Static)throw new Exception("Font not baked");}
  return "Shader compiled; Spanish glyphs present; static font atlases";
 }
 public static async Task<string> Runtime()
 {
  for(int i=0;i<100&&!Codice.UI.RunHUD.Instance;i++)await Task.Delay(100);
  var hud=Codice.UI.RunHUD.Instance;if(!hud)throw new Exception("HUD missing");
  var floor=UnityEngine.Object.FindFirstObjectByType<Codice.Expedition.ExpeditionFloor>();if(!floor||floor.stoneMaterial.shader.name!="Cinders/Weathered Stone")throw new Exception("Stone not applied");
  int count=UnityEngine.Object.FindObjectsByType<Renderer>().Count(r=>r.sharedMaterials.Any(m=>m&&m.shader.name=="Cinders/Weathered Stone"));if(count<20)throw new Exception("Not enough stone renderers");
  var cam=hud.Player.view.GetUniversalAdditionalCameraData();if(!cam.renderPostProcessing)throw new Exception("Post processing disabled");
  var v=UnityEngine.Object.FindObjectsByType<Volume>().First(x=>x.name=="Ashen dungeon grade");if(!v.sharedProfile.TryGet<ColorAdjustments>(out var grade)||!grade.saturation.overrideState||grade.saturation.value!=-24)throw new Exception("Grade missing");
  hud.SetPaused(true);await Task.Delay(150);Canvas.ForceUpdateCanvases();
  var labels=hud.GetComponentsInChildren<TextMeshProUGUI>();var wrong=labels.Where(x=>x.isActiveAndEnabled&&!x.font.name.StartsWith("IMFell")).Select(x=>x.name).ToArray();
  if(wrong.Length>0)throw new Exception("Old fonts: "+string.Join(",",wrong));
  return "Photographic stone renderers="+count+"; grading active; inventory typography applied; overflow="+string.Join(",",labels.Where(x=>x.isActiveAndEnabled&&x.isTextOverflowing).Select(x=>x.name));
 }
}
