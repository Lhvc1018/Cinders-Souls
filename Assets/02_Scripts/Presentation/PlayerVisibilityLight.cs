using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Codice.Presentation
{
    public sealed class PlayerVisibilityLight : MonoBehaviour
    {
        Light fill;
        void Start()
        {
            var player = GetComponent<Codice.Characters.PlayerCharacter>();
            if (!player || !player.view || Codice.Multiplayer.LanAvatar.IsRemote(player)) return;
            var obj = new GameObject("Camera visibility fill");
            obj.transform.SetParent(player.view.transform, false);
            obj.transform.localPosition = new Vector3(0, .12f, .15f);
            fill = obj.AddComponent<Light>();
            fill.type = LightType.Point; fill.range = 8.5f; fill.intensity = 1.2f;
            fill.color = new Color(.72f, .78f, .87f);
            fill.shadows = LightShadows.Soft; fill.shadowStrength = .5f;
            fill.shadowBias = .025f; fill.shadowNormalBias = .15f;
            fill.renderMode = LightRenderMode.ForcePixel;
            player.view.allowHDR = true;
            player.view.GetUniversalAdditionalCameraData().renderPostProcessing = true; player.view.GetUniversalAdditionalCameraData().stopNaN = true;
        }
        public static void ConfigureAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.32f, .35f, .4f);
            RenderSettings.ambientEquatorColor = new Color(.22f, .25f, .29f);
            RenderSettings.ambientGroundColor = new Color(.14f, .15f, .18f);
            RenderSettings.ambientIntensity = 1;
        }
        void OnDestroy() { if (fill) Destroy(fill.gameObject); }
    }
}
