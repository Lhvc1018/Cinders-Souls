using TMPro;
using Text = TMPro.TextMeshProUGUI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using DG.Tweening;
using TMPEffects.Components;
namespace Codice.UI
{
    public sealed partial class MainMenuUI : MonoBehaviour
    {
        RectTransform home,options,classes;
        MenuModelShowcase classShowcase;
        Text className,classDetails,classLore,classOrdinal;
        readonly Button[] classButtons=new Button[5];
        string[] classNames,classStats;
        Codice.Characters.PlayerClass[] classKinds;
        int selectedClass;
        int lastPreviewedClass=-1;
        Text values,displayMode;
        static readonly Color Gold=new Color(.88f,.71f,.43f), Cream=new Color(.95f,.92f,.85f), Muted=new Color(.57f,.61f,.66f);
        void Start()
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(PlayerPrefs.HasKey("Codice.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("Codice.Fullscreen")==1;
            AudioListener.volume=PlayerPrefs.GetFloat("Codice.Volume",1);
            var root=new GameObject("Main menu canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            UIWidgets.ConfigureCanvas(root.GetComponent<Canvas>());
            if(!FindFirstObjectByType<EventSystem>())new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            var bg=UIWidgets.Image(root.transform,"Background",0,0,1920,1080,new Color(.016f,.02f,.027f)).rectTransform;bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;bg.offsetMin=bg.offsetMax=Vector2.zero;
            var backdrop=UIWidgets.Rect(root.transform,"Cathedral backdrop",0,0,1920,1080).gameObject.AddComponent<RawImage>();
            backdrop.texture=Resources.Load<Texture2D>("CindersMenu/Cathedral");backdrop.raycastTarget=false;
            backdrop.rectTransform.anchorMin=Vector2.zero;backdrop.rectTransform.anchorMax=Vector2.one;backdrop.rectTransform.offsetMin=backdrop.rectTransform.offsetMax=Vector2.zero;
            backdrop.color=new Color(.72f,.72f,.72f,1);
            var atmosphere=UIWidgets.Rect(root.transform,"Ash and cathedral",0,0,1920,1080);
            atmosphere.anchorMin=Vector2.zero;atmosphere.anchorMax=Vector2.one;atmosphere.offsetMin=atmosphere.offsetMax=Vector2.zero;
            var emberLayer=Centered(root.transform,"Candle embers",1920,1080);emberLayer.gameObject.AddComponent<MenuEmbers>();
            home=Centered(root.transform,"Main Menu",1680,900);
            options=Centered(root.transform,"Opciones",760,900);options.gameObject.SetActive(false);
            Label(home,"Edition",90,70,700,32,18,Gold,"C I N D E R S   S O U L S     /     EL ÚLTIMO CICLO");
            Line(home,90,125,540,new Color(.5f,.4f,.28f,.6f));
            var title=Label(home,"Title",82,175,920,285,88,Cream,"CINDERS\n<color=#AD8570>SOULS</color>");
            title.characterSpacing=10;
            var titleEffect=title.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();
            titleEffect.shadowMode=Coffee.UIEffects.ShadowMode.Shadow;titleEffect.shadowDistance=new Vector2(2,-3);titleEffect.shadowColor=Color.black;
            Label(home,"Subtitle",90,460,680,90,25,Muted,"Toda llama deja una deuda.\nTodo descenso exige un nombre.");
            var first=Button(home,"INICIAR DESCENSO",90,580,560,70,()=>ShowClasses(true),true);
            Button(home,"COOPERATIVO LAN",90,665,560,62,Codice.Multiplayer.LanSession.Open);
            Button(home,"OPCIONES",90,742,270,56,()=>ShowOptions(true));
            Button(home,"SALIR",380,742,270,56,RunNavigation.Quit);
            Button(home,"TRUCOS",1330,805,250,48,()=>ShowCheats(true));
            BuildCheats(root.transform);
            Label(home,"World caption",1110,620,490,80,25,Gold,"LA VIGILIA NO TERMINA");
            Label(home,"World subtitle",1110,680,450,95,20,Muted,"Un reino bajo cenizas.\nCinco legados. Una última oportunidad.");
            Line(home,90,855,1510,new Color(.3f,.28f,.23f));
            Label(home,"Footer",90,875,1100,25,16,Muted,"ROGUELIKE  /  HORROR GÓTICO                           ↑ ↓ ELEGIR   ·   ENTER CONFIRMAR");
            Label(options,"Eyebrow",40,30,680,30,18,Gold,"CINDERS SOULS    /    AJUSTES");
            Label(options,"Heading",35,100,690,85,64,Cream,"A tu manera.");
            Label(options,"Description",40,195,680,38,21,Muted,"Prepara tu próximo descenso.");
            Line(options,40,262,680,new Color(.3f,.28f,.23f));
            values=Label(options,"Values",40,303,680,140,25,Cream,"");
            Button(options,"VOLUMEN −",40,480,325,60,()=>Volume(-.1f));Button(options,"VOLUMEN +",395,480,325,60,()=>Volume(.1f));
            Button(options,"SENSIBILIDAD −",40,562,325,60,()=>Sensitivity(-.01f));Button(options,"SENSIBILIDAD +",395,562,325,60,()=>Sensitivity(.01f));
            var display=Button(options,"CAMBIAR PANTALLA COMPLETA",40,644,680,60,()=>{bool full=!Screen.fullScreen;Screen.fullScreen=full;PlayerPrefs.SetInt("Codice.Fullscreen",full?1:0);PlayerPrefs.Save();});
            displayMode=display.GetComponentInChildren<Text>();
            Button(options,"VOLVER",40,757,680,68,()=>ShowOptions(false),true);
            Label(options,"Saved",40,854,680,30,17,Muted,"Los cambios se guardan automáticamente.  •  Esc para volver");
            classes=Centered(root.transform,"Elegir clase",1680,900);
            Label(classes,"Chapter",80,35,1520,35,18,Gold,"CINDERS SOULS  /  ELEGIR LEGADO");
            UIWidgets.Image(classes,"Class reading panel",1150,270,470,530,new Color(.025f,.025f,.03f,.92f));
            UIWidgets.Image(classes,"Class panel spine",1169,302,2,365,new Color(.58f,.44f,.29f,.7f));
            UIWidgets.Image(classes,"Class panel seal",1162,280,16,16,Gold,"diamond");
            Label(classes,"Title",80,135,1110,90,64,Cream,"Elige tu legado.");
            Label(classes,"Hint",80,230,1520,45,21,Muted,"Cada clase empieza con su propia arma. Encontrarás más equipo durante el descenso.");
            classNames=new[]{"CABALLERO","ARQUERO","TANQUE","MAGO","SHINOBI"};
            classStats=new[]{"120 VIDA   /   VELOCIDAD 6.5\n\nEspada y escudo. Daño equilibrado.\nClic derecho: bloquear.\nQ: gran tajo.","100 VIDA   /   VELOCIDAD 8\n\nArco y flechas. Combate a distancia.\nRecoge munición durante la partida.\nQ: lluvia de flechas.","180 VIDA   /   VELOCIDAD 4.8\n\nMazo y resistencia. Ataques en área.\nMenos movilidad, más vida.\nQ: impacto contra el suelo.","100 VIDA   /   VELOCIDAD 6.5\n\nBastón y hechizos. 100 de maná.\nEl maná se regenera con el tiempo.\nQ: descarga arcana.","120 VIDA   /   VELOCIDAD 6.5\n\nKatana y shuriken. Defensa precisa.\nClic derecho: parry y postura.\nQ: tajos de sangre."};
            classKinds=new[]{Codice.Characters.PlayerClass.Warrior,Codice.Characters.PlayerClass.Archer,Codice.Characters.PlayerClass.Tank,Codice.Characters.PlayerClass.Mage,Codice.Characters.PlayerClass.Shinobi};
            var portrait=UIWidgets.Rect(classes,"Retrato de clase",520,270,620,540).gameObject.AddComponent<RawImage>();
            classShowcase=portrait.gameObject.AddComponent<MenuModelShowcase>();
            classShowcase.Initialize(portrait,"Class_Warrior");
            classOrdinal=Label(classes,"Legacy number",1190,278,390,25,16,Muted,"");
            className=Label(classes,"Class name",1190,310,390,65,34,Gold,"");
            var nameShadow=className.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();
            nameShadow.shadowMode=Coffee.UIEffects.ShadowMode.Shadow;nameShadow.shadowDistance=new Vector2(1,-2);nameShadow.shadowColor=Color.black;
            classLore=Label(classes,"Class lore",1190,365,390,40,17,Muted,"");
            classDetails=Label(classes,"Class details",1190,415,390,285,24,Cream,"");
            Line(classes,1190,395,390,new Color(.42f,.31f,.24f));
            for(int i=0;i<5;i++)
            {
                int index=i;
                var choice=Button(classes,classNames[i],80,310+i*90,400,72,()=>PreviewClass(index),false);
                classButtons[i]=choice;
                var preview=choice.gameObject.AddComponent<MenuClassPreviewTrigger>();preview.menu=this;preview.index=i;
            }
            PreviewClass(0);
            Button(classes,"VOLVER",80,810,260,55,()=>ShowClasses(false));
            Button(classes,"DESCENDER",1190,715,390,65,()=>RunNavigation.StartGame(classKinds[selectedClass],CheatRound),true);
            Label(classes,"Controls",385,827,1200,50,18,Muted,"Selecciona una clase   •   Confirma para comenzar   •   Esc para volver");
            classes.gameObject.SetActive(false);
            first.Select();RefreshOptions();
            FadeIn(home);
        }
        static RectTransform Centered(Transform parent,string name,float w,float h)
        {var r=UIWidgets.Rect(parent,name,0,0,w,h);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;}
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size,Color color,string text)
        {var t=UIWidgets.Label(parent,name,x,y,w,h,size,color);t.text=text;if(size>=30){var font=Resources.Load<TMP_FontAsset>("CindersFonts/UncialAntiqua");if(font)t.font=font;}return t;}
        static void MetalPlate(Transform parent,float x,float y,float w,float h)
        {var texture=Resources.Load<Texture2D>("CindersMenu/MetalBar");if(!texture)return;var plate=UIWidgets.Rect(parent,"Forged title plate",x,y,w,h).gameObject.AddComponent<RawImage>();plate.texture=texture;plate.raycastTarget=false;}
        static void Line(Transform parent,float x,float y,float w,Color color)=>UIWidgets.Image(parent,"Divider",x,y,w,1,color);
        static Button Button(Transform parent,string name,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,bool primary=false)
        {
            var rect=UIWidgets.Rect(parent,name,x,y,w,h);
            var face=rect.gameObject.AddComponent<RelicButtonFace>();face.primary=primary;face.raycastTarget=true;
            var b=rect.gameObject.AddComponent<RelicMenuButton>();b.targetGraphic=face;b.onClick.AddListener(action);
            b.transition=Selectable.Transition.None;
            var label=Label(rect,"Label",66,0,w-112,h,w<350?20:24,Cream,name);label.alignment=TextAlignmentOptions.MidlineLeft;label.fontStyle=FontStyles.Bold;
            var font=Resources.Load<TMP_FontAsset>("CindersFonts/UncialAntiqua");if(font)label.font=font;
            return b;
        }
        void Update()
        {
            if(cheats&&cheats.gameObject.activeSelf&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){ShowCheats(false);return;}
            if(classes && classes.gameObject.activeSelf && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)ShowClasses(false);
            if(options && options.gameObject.activeSelf && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)ShowOptions(false);
            if(displayMode)displayMode.text=Screen.fullScreen?"PANTALLA COMPLETA  •  CAMBIAR":"MODO VENTANA  •  CAMBIAR";
        }
        static void FadeIn(RectTransform panel){var group=panel.GetComponent<CanvasGroup>();if(!group)group=panel.gameObject.AddComponent<CanvasGroup>();group.DOKill();group.alpha=0;group.DOFade(1,.45f).SetUpdate(true).SetLink(panel.gameObject);}
        void ShowClasses(bool show){home.gameObject.SetActive(!show);classes.gameObject.SetActive(show);FadeIn(show?classes:home);(show?classes:home).GetComponentInChildren<Button>().Select();}
        public void PreviewClass(int index)
        {
            if(classNames==null||index<0||index>=classNames.Length)return;
            selectedClass=index;className.text=classNames[index];classDetails.text=classStats[index];
            classLore.text=new[]{"Un juramento entre ruinas.","La noche nunca te ve llegar.","La última muralla del reino.","El conocimiento también consume.","Una sombra bajo la ceniza."}[index];
            classOrdinal.text=$"LEGADO  {index+1:00} / {classNames.Length:00}";
            if(lastPreviewedClass!=index)
            {
                lastPreviewedClass=index;
                FadeClassLabel(className);FadeClassLabel(classLore);FadeClassLabel(classDetails);FadeClassLabel(classOrdinal);
            }
            for(int i=0;i<classButtons.Length;i++)if(classButtons[i]){var face=classButtons[i].GetComponent<RelicButtonFace>();face.primary=i==index;face.SetVerticesDirty();}

            classShowcase.SetModel(new[]{"Class_Warrior","Class_Archer","Class_Tank","Class_Mage","Class_Shinobi"}[index]);
        }
        static void FadeClassLabel(Text label)
        {
            var group=label.GetComponent<CanvasGroup>();if(!group)group=label.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;group.DOKill();group.alpha=.55f;group.DOFade(1,.25f).SetUpdate(true).SetLink(label.gameObject);
        }
        void ShowOptions(bool show){home.gameObject.SetActive(!show);options.gameObject.SetActive(show);FadeIn(show?options:home);(show?options:home).GetComponentInChildren<Button>().Select();RefreshOptions();}
        void Volume(float delta){AudioListener.volume=Mathf.Clamp01(AudioListener.volume+delta);PlayerPrefs.SetFloat("Codice.Volume",AudioListener.volume);PlayerPrefs.Save();RefreshOptions();}
        void Sensitivity(float delta){PlayerPrefs.SetFloat("Codice.Sensitivity",Mathf.Clamp(PlayerPrefs.GetFloat("Codice.Sensitivity",.09f)+delta,.02f,.3f));PlayerPrefs.Save();RefreshOptions();}
        void RefreshOptions(){values.text=$"VOLUMEN GENERAL                         {AudioListener.volume:P0}\n\nSENSIBILIDAD DEL RATÓN                 {PlayerPrefs.GetFloat("Codice.Sensitivity",.09f):0.00}";}
    }
}
