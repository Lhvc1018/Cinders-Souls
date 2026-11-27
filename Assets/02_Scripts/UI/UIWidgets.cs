using TMPro;
using Text = TMPro.TextMeshProUGUI;
using Codice.Characters;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Codice.UI
{
    public static class UIWidgets
    {
        static TMP_FontAsset font;
        public static void ConfigureCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (!scaler) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // Keep the entire reference layout visible on 4:3, 16:10 and ultrawide displays.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        public static Image Image(Transform parent,string name,float x,float y,float w,float h,Color color,string shape="solid")
        {var i=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=ProceduralUISprites.Shape(shape);i.color=color;if(shape=="frame")i.type=UnityEngine.UI.Image.Type.Sliced;i.raycastTarget=false;return i;}
        public static Text Label(Transform parent,string name,float x,float y,float w,float h,int size=20,Color? color=null)
        {
            if (!font) font = Resources.Load<TMP_FontAsset>("CindersFonts/IMFellEnglish") ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var t = Rect(parent,name,x,y,w,Mathf.Max(h,size*1.65f)).gameObject.AddComponent<Text>();
            var displayFont = size >= 30 ? Resources.Load<TMP_FontAsset>("CindersFonts/IMFellEnglishSC") : null;
            t.font = displayFont ? displayFont : font; t.fontSize = size; t.color = color ?? new Color(.95f,.94f,.91f);
            t.characterSpacing = size >= 30 ? 1.8f : .2f; t.raycastTarget = false; t.richText = true; t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }
        public static Button Button(Transform parent,string text,float x,float y,float w,float h,UnityAction action)
        {
            var rect=Rect(parent,text,x,y,w,h);
            var face=rect.gameObject.AddComponent<RelicButtonFace>();face.raycastTarget=true;
            var button=rect.gameObject.AddComponent<RelicMenuButton>();button.targetGraphic=face;
            button.transition=Selectable.Transition.None;button.onClick.AddListener(action);
            var label=Label(rect,"Label",48,0,w-78,h,18);label.text=text;label.alignment=TextAlignmentOptions.MidlineLeft;
            return button;
        }
        public static RectTransform Surface(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=Image(parent,name,x,y,w,h,new Color(.042f,.047f,.044f,.94f)).rectTransform;
            Image(r,"Upper gilt",0,0,w,1,new Color(.48f,.41f,.27f,.65f));
            Image(r,"Lower gilt",0,h-1,w,1,new Color(.48f,.41f,.27f,.45f));
            return r;
        }

        public static void Ornament(Transform parent,float x,float y,float width)
        {
            var gold=new Color(.58f,.52f,.37f,.65f);
            Image(parent,"Etched rule",x,y,width,1,gold);
            Image(parent,"Left finial",x,y-3,7,7,gold,"diamond");
            Image(parent,"Right finial",x+width-7,y-3,7,7,gold,"diamond");
            Image(parent,"Central seal",x+width*.5f-5,y-5,10,10,gold,"diamond");
        }

        public static Image Filled(Transform parent,string name,float x,float y,float w,float h,Color color,bool radial=false)
        {var i=Image(parent,name,x,y,w,h,color,radial?"ring":"solid");i.type=UnityEngine.UI.Image.Type.Filled;i.fillMethod=radial?UnityEngine.UI.Image.FillMethod.Radial360:UnityEngine.UI.Image.FillMethod.Horizontal;i.fillOrigin=0;return i;}
        public static string WeaponName(WeaponInstance w)=>w.katana?"Katana":w.shuriken?"Shuriken":WeaponName(w.kind);
        public static string WeaponName(WeaponKind k)=>new[]{"Espada","Hacha","Arco","Daga","Martillo","Lanza","Guadaña","Escudo","Báculo","Mosquete"}[(int)k];
        public static string Special(WeaponKind k)=>new[]{"Gran Tajo","Hendidura Sísmica","Lluvia de Flechas","Ráfaga de Dagas","Impacto Sísmico","Empalamiento Imperial","Siega Circular","Bastión Inmortal","Torrente Arcano","Descarga de Pólvora"}[(int)k];
        public static string Passive(PassiveKind k)=>new[]{"Frenesí","Corazón Vampírico","Sobrecarga","Botas de Mercurio","Coraza de Espinas"}[(int)k];
        public static string Clock(float seconds)=>$"{(int)seconds/60:00}:{(int)seconds%60:00}.{(int)(seconds*100)%100:00}";
    }
    public sealed class GhostBar : MonoBehaviour
    {
        [field: SerializeField] public Image Fill {get;private set;}
        [field: SerializeField] public Image Ghost {get;private set;}
        [field: SerializeField] public Text Caption {get;private set;}
        float previous=-1,delay;
        public static GhostBar Create(Transform parent,string name,float x,float y,float width,float height)
        {
            var frame=UIWidgets.Image(parent,name,x,y,width,height,new Color(.36f,.31f,.21f));var bar=frame.gameObject.AddComponent<GhostBar>();
            UIWidgets.Image(frame.transform,"Well",2,2,width-4,height-4,ProceduralUISprites.Abyss);
            bar.Ghost=UIWidgets.Filled(frame.transform,"Rally",2,2,width-4,height-4,ProceduralUISprites.Gold);
            bar.Fill=UIWidgets.Filled(frame.transform,"Health",2,2,width-4,height-4,ProceduralUISprites.Crimson);
            bar.Caption=UIWidgets.Label(frame.transform,"Value",9,3,width-18,height-3,Mathf.Min(20,(int)height-6));bar.Caption.alignment=TextAlignmentOptions.Center;return bar;
        }
        public void Set(float health,float maximum,string caption=null)
        {
            float value=maximum>0?Mathf.Clamp01(health/maximum):0;
            if(previous<0||value>previous) Ghost.fillAmount=value;
            else if(value<previous)delay=Time.unscaledTime+.35f;
            Fill.fillAmount=value;previous=value;Caption.text=caption??$"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maximum)}";
        }
        void Update(){if(Time.unscaledTime>=delay)Ghost.fillAmount=Mathf.Lerp(Ghost.fillAmount,Fill.fillAmount,1-Mathf.Exp(-7*Time.unscaledDeltaTime));}
    }
}
