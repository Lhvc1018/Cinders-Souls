using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Codice.Multiplayer;
using Codice.UI;
using TMPro;
namespace Codice.Presentation
{
    // Clone the pipeline: quality choices must never modify the imported project asset.
    public sealed class RuntimePerformance : MonoBehaviour
    {
        static RuntimePerformance instance;
        UniversalRenderPipelineAsset original, runtime;
        RenderPipelineAsset previousQuality;
        TextMeshProUGUI label;
        float elapsed, worst;
        int frames, height;
        public static bool NativeResolution=>PlayerPrefs.GetInt("Codice.NativeResolution",0)==1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(!instance)new GameObject("Performance and network display").AddComponent<RuntimePerformance>();}
        void Awake()
        {
            instance=this;DontDestroyOnLoad(gameObject);
            original=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(original){previousQuality=QualitySettings.renderPipeline;runtime=Instantiate(original);runtime.name="Cinders runtime quality";QualitySettings.renderPipeline=runtime;ApplyResolution();}
            var canvas=new GameObject("Performance overlay",typeof(Canvas));canvas.transform.SetParent(transform,false);
            UIWidgets.ConfigureCanvas(canvas.GetComponent<Canvas>());
            canvas.GetComponent<Canvas>().sortingOrder=1000;
            label=UIWidgets.Label(canvas.transform,"FPS and latency",-260,5,520,32,20);
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,1);
            label.alignment=TextAlignmentOptions.Center;
        }
        public static void ToggleResolution(){PlayerPrefs.SetInt("Codice.NativeResolution",NativeResolution?0:1);PlayerPrefs.Save();if(instance)instance.ApplyResolution();}
        void ApplyResolution(){height=Screen.height;if(runtime)runtime.renderScale=NativeResolution?1:Mathf.Clamp(1440f/Mathf.Max(1,height),.5f,1);}
        void Update()
        {
            if(height!=Screen.height)ApplyResolution();
            float dt=Time.unscaledDeltaTime;elapsed+=dt;worst=Mathf.Max(worst,dt);frames++;
            if(elapsed<.5f)return;
            string network=!LanSession.Active?"PING: SIN CONEXIÓN":LanSession.Host?(LanSession.Instance.HostLinkInfo is string link?"INVITADO: "+link:"PING: ANFITRIÓN"):LanSession.Client?(LanSession.Instance.PingMilliseconds<0?"PING: MIDIENDO…":"PING: "+LanSession.ColoredPing(LanSession.Instance.PingMilliseconds)):"PING: CONECTANDO…";
            label.text=(frames/elapsed).ToString("0")+" FPS · "+(elapsed*1000/frames).ToString("0.0")+" ms · PICO "+(worst*1000).ToString("0")+" ms   |   "+network;
            elapsed=worst=0;frames=0;
        }
        void OnDestroy(){if(instance!=this)return;instance=null;if(runtime){if(QualitySettings.renderPipeline==runtime)QualitySettings.renderPipeline=previousQuality;Destroy(runtime);}}
    }
}
