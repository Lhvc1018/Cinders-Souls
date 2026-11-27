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
        int lastPreviewedClass=-1;bool choosingEndless;
        Text values,displayMode;
        static readonly Color Gold=new Color(.74f,.65f,.44f), Cream=new Color(.91f,.88f,.77f), Muted=new Color(.65f,.63f,.55f);
        void Start()
        {
            if(Application.isPlaying){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(PlayerPrefs.HasKey("Codice.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("Codice.Fullscreen")==1;
            AudioListener.volume=PlayerPrefs.GetFloat("Codice.Volume",1);
            var root=new GameObject("Main menu canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            UIWidgets.ConfigureCanvas(root.GetComponent<Canvas>());
            if(!FindFirstObjectByType<EventSystem>())new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            var bg=UIWidgets.Image(root.transform,"Background",0,0,1920,1080,new Color(.016f,.02f,.027f)).rectTransform;bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;bg.offsetMin=bg.offsetMax=Vector2.zero;
            var backdrop=UIWidgets.Rect(root.transform,"Cathedral backdrop",0,0,1920,1080).gameObject.AddComponent<RawImage>();
            backdrop.texture=Resources.Load<Texture2D>("CindersMenu/Cathedral");backdrop.raycastTarget=false;
            backdrop.rectTransform.anchorMin=Vector2.zero;backdrop.rectTransform.anchorMax=Vector2.one;backdrop.rectTransform.offsetMin=backdrop.rectTransform.offsetMax=Vector2.zero;
            backdrop.color=new Color(.32f,.32f,.29f,1);
            var atmosphere=UIWidgets.Rect(root.transform,"Ash and cathedral",0,0,1920,1080);
            atmosphere.anchorMin=Vector2.zero;atmosphere.anchorMax=Vector2.one;atmosphere.offsetMin=atmosphere.offsetMax=Vector2.zero;
            var emberLayer=Centered(root.transform,"Candle embers",1920,1080);emberLayer.gameObject.AddComponent<MenuEmbers>();
            home=Centered(root.transform,"Main Menu",1680,900);
            options=Centered(root.transform,"Opciones",760,900);options.gameObject.SetActive(false);
            var sigil=UIWidgets.Rect(home,"Ashen vow",740,22,200,200).gameObject.AddComponent<AshenSigil>();sigil.color=new Color(.65f,.53f,.32f,.66f);sigil.raycastTarget=false;
            var edition=Label(home,"Edition",440,225,800,35,17,Gold,"U N   R E I N O   B A J O   C E N I Z A S");edition.alignment=TextAlignmentOptions.Center;
            var title=Label(home,"Title",260,280,1160,120,88,Cream,"CINDERS SOULS");title.characterSpacing=8;title.alignment=TextAlignmentOptions.Center;
            var titleEffect=title.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();titleEffect.shadowMode=Coffee.UIEffects.ShadowMode.Shadow;titleEffect.shadowDistance=new Vector2(1,-3);titleEffect.shadowColor=Color.black;
            Line(home,510,433,660,new Color(.5f,.43f,.29f,.6f));
            var motto=Label(home,"Subtitle",390,451,900,45,24,Muted,"Toda llama deja una deuda.");motto.alignment=TextAlignmentOptions.Center;
            var first=Button(home,"INICIAR DESCENSO",560,545,560,57,()=>{choosingEndless=false;ShowClasses(true);},true);
            Button(home,"COLISEO INFINITO",560,608,560,51,()=>{choosingEndless=true;ShowClasses(true);});
            Button(home,"COOPERATIVO",560,669,560,51,Codice.Multiplayer.LanSession.Open);
            Button(home,"OPCIONES",560,730,560,51,()=>ShowOptions(true));
            Button(home,"SALIR",560,791,560,51,RunNavigation.Quit);
            Button(home,"TRUCOS",1420,838,190,40,()=>ShowCheats(true));BuildCheats(root.transform);
            var foot=footer=Label(home,"Footer",350,851,980,30,17,Muted,"↑ ↓  Seleccionar      Enter  Confirmar      Esc  Volver");foot.alignment=TextAlignmentOptions.Center;
            Label(options,"Eyebrow",40,30,680,30,18,Gold,"CINDERS SOULS    /    AJUSTES");
            Label(options,"Heading",35,100,690,85,64,Cream,"Ajustes");
            Label(options,"Description",40,195,680,38,21,Muted,"Prepara tu próximo descenso.");
            Line(options,40,262,680,new Color(.3f,.28f,.23f));
            values=Label(options,"Values",40,303,680,140,25,Cream,"");
            Button(options,"VOLUMEN −",40,480,325,60,()=>Volume(-.1f));Button(options,"VOLUMEN +",395,480,325,60,()=>Volume(.1f));
            Button(options,"SENSIBILIDAD −",40,562,325,60,()=>Sensitivity(-.01f));Button(options,"SENSIBILIDAD +",395,562,325,60,()=>Sensitivity(.01f));
            var display=Button(options,"CAMBIAR PANTALLA COMPLETA",40,644,680,60,()=>{bool full=!Screen.fullScreen;Screen.fullScreen=full;PlayerPrefs.SetInt("Codice.Fullscreen",full?1:0);PlayerPrefs.Save();});
            displayMode=display.GetComponentInChildren<Text>();
            var quality=Button(options,"RESOLUCION 3D",40,710,680,38,()=>{});
            var qualityText=quality.GetComponentInChildren<Text>();
            System.Action refreshQuality=()=>qualityText.text=Codice.Presentation.RuntimePerformance.NativeResolution?"RESOLUCIÓN 3D: NATIVA":"RESOLUCIÓN 3D: FLUIDA (MÁX. 1440p)";
            quality.onClick.AddListener(()=>{Codice.Presentation.RuntimePerformance.ToggleResolution();refreshQuality();});refreshQuality();
            Button(options,"VOLVER",40,757,680,68,()=>ShowOptions(false),true);
            Label(options,"Saved",40,854,680,30,17,Muted,"Los cambios se guardan automáticamente.  •  Esc para volver");
            classes=Centered(root.transform,"Elegir clase",1680,900);
            Label(classes,"Chapter",80,35,1520,35,18,Gold,"CINDERS SOULS  /  ELEGIR LEGADO");
            UIWidgets.Image(classes,"Class reading panel",80,270,450,500,new Color(.035f,.034f,.028f,.86f));
            UIWidgets.Image(classes,"Class panel spine",99,302,1,365,new Color(.58f,.44f,.29f,.7f));
            UIWidgets.Image(classes,"Class panel seal",94,280,10,10,Gold,"diamond");
            Label(classes,"Title",80,116,1480,80,48,Cream,"Elige tu legado");
            Label(classes,"Hint",80,211,1520,45,21,Muted,"Cada clase empieza con su propia arma. Encontrarás más equipo durante el descenso.");
            classNames=new[]{"CABALLERO","ARQUERO","TANQUE","MAGO","SHINOBI"};
            classStats=new[]{"120 VIDA   /   VELOCIDAD 6.5\n\nEspada y escudo. Daño equilibrado.\nClic derecho: bloquear.\nQ: gran tajo.","100 VIDA   /   VELOCIDAD 7.4\n\nArco y flechas. Combate a distancia.\nRecoge munición durante la partida.\nQ: lluvia de flechas.","180 VIDA   /   VELOCIDAD 4.8\n\nMazo y resistencia. Ataques en área.\nMenos movilidad, más vida.\nQ: impacto contra el suelo.","100 VIDA   /   VELOCIDAD 6.5\n\nBastón y hechizos. 100 de maná.\nEl maná se regenera con el tiempo.\nQ: descarga arcana.","120 VIDA   /   VELOCIDAD 7.8\n\nKatana y shuriken. Defensa precisa.\nClic derecho: parry y postura.\nQ: tajos de sangre."};
            classKinds=new[]{Codice.Characters.PlayerClass.Warrior,Codice.Characters.PlayerClass.Archer,Codice.Characters.PlayerClass.Tank,Codice.Characters.PlayerClass.Mage,Codice.Characters.PlayerClass.Shinobi};
            var portrait=UIWidgets.Rect(classes,"Retrato de clase",540,260,620,560).gameObject.AddComponent<RawImage>();
            classShowcase=portrait.gameObject.AddComponent<MenuModelShowcase>();
            if(Application.isPlaying)classShowcase.Initialize(portrait,"Class_Warrior");
            classOrdinal=Label(classes,"Legacy number",115,290,390,28,16,Muted,"");
            className=Label(classes,"Class name",115,336,390,65,32,Gold,"");
            var nameShadow=className.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();
            nameShadow.shadowMode=Coffee.UIEffects.ShadowMode.Shadow;nameShadow.shadowDistance=new Vector2(1,-2);nameShadow.shadowColor=Color.black;
            classLore=Label(classes,"Class lore",115,403,390,40,17,Muted,"");
            classDetails=Label(classes,"Class details",115,468,375,280,23,Cream,"");
            Line(classes,115,451,375,new Color(.42f,.31f,.24f));
            for(int i=0;i<classNames.Length;i++)
            {
                int index=i;
                var choice=Button(classes,classNames[i],1200,275+i*83,400,64,()=>ClassPressed(index),false);
                classButtons[i]=choice;
                var preview=choice.gameObject.AddComponent<MenuClassPreviewTrigger>();preview.menu=this;preview.index=i;
            }
            PreviewClass(0);
            Button(classes,"VOLVER",80,810,260,55,()=>ShowClasses(false));
            Button(classes,"DESCENDER",1200,810,400,55,()=>AskConfirm(),true);
            Label(classes,"Controls",385,831,780,35,17,Muted,"Selecciona un legado. Confirma para comenzar.");
            classes.gameObject.SetActive(false);
            BuildSaveMenus(root.transform);BuildClassConfirm(root.transform);TuneMenuNavigation();RefreshOptions();
            if(Application.isPlaying){ApplyAuthoredLayout(root.transform);FadeIn(home);}
            RepairText(root.transform);RefreshHome();(continueButton.gameObject.activeSelf?continueButton:first).Select();
        }
        static RectTransform Centered(Transform parent,string name,float w,float h)
        {var r=UIWidgets.Rect(parent,name,0,0,w,h);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;}
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size,Color color,string text)
        {var t=UIWidgets.Label(parent,name,x,y,w,h,size,color);t.text=text;if(size>=30){var font=Resources.Load<TMP_FontAsset>("CindersFonts/IMFellEnglishSC");if(font)t.font=font;}return t;}
        static void MetalPlate(Transform parent,float x,float y,float w,float h)
        {UIWidgets.Ornament(parent,x,y+h-5,w);}
        static void Line(Transform parent,float x,float y,float w,Color color)=>UIWidgets.Image(parent,"Divider",x,y,w,1,color);
        static Button Button(Transform parent,string name,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,bool primary=false)
        {
            var rect=UIWidgets.Rect(parent,name,x,y,w,h);
            var face=rect.gameObject.AddComponent<RelicButtonFace>();face.primary=primary;face.raycastTarget=true;
            var b=rect.gameObject.AddComponent<RelicMenuButton>();b.targetGraphic=face;b.onClick.AddListener(action);
            b.transition=Selectable.Transition.None;
            var label=Label(rect,"Label",48,0,w-76,h,w<350?18:22,Cream,name);label.alignment=TextAlignmentOptions.MidlineLeft;label.fontStyle=FontStyles.Normal;
            var font=Resources.Load<TMP_FontAsset>("CindersFonts/IMFellEnglishSC");if(font)label.font=font;
            return b;
        }
        void Update()
        {
            bool back=Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Back);
            if(confirmPanel&&confirmPanel.gameObject.activeSelf){if(back)CloseConfirm();return;}
            if(classHint&&classes.gameObject.activeSelf&&Time.frameCount%15==0)RefreshClassHint();
            if(controlsSheet&&controlsSheet.gameObject.activeSelf){if(back)ShowControls(false);return;}
            if(slotsPanel&&slotsPanel.gameObject.activeSelf){if(back)ShowSlots(false);return;}
            if(cheats&&cheats.gameObject.activeSelf&&back){ShowCheats(false);return;}
            if(classes && classes.gameObject.activeSelf && back)ShowClasses(false);
            if(options && options.gameObject.activeSelf && back)ShowOptions(false);
            if(footer)footer.text=Codice.Mechanics.GameInput.UsingGamepad?$"{Codice.Mechanics.GameInput.PadLabel(Codice.Mechanics.GameAction.Move)}  Seleccionar      {Codice.Mechanics.GameInput.PadLabel(Codice.Mechanics.GameAction.Confirm)}  Confirmar      {Codice.Mechanics.GameInput.PadLabel(Codice.Mechanics.GameAction.Back)}  Volver":"Flechas  Seleccionar      Enter  Confirmar      Esc  Volver";
            if(displayMode)displayMode.text=Screen.fullScreen?"PANTALLA COMPLETA  •  CAMBIAR":"MODO VENTANA  •  CAMBIAR";
        }
        static void FadeIn(RectTransform panel){if(!Application.isPlaying)return;var group=panel.GetComponent<CanvasGroup>();if(!group)group=panel.gameObject.AddComponent<CanvasGroup>();group.DOKill();group.alpha=0;group.DOFade(1,.45f).SetUpdate(true).SetLink(panel.gameObject);}
        void ShowClasses(bool show){armedClass=-1;RefreshClassHint();if(show)classShowcase.Preload(System.Array.ConvertAll(new[]{"Warrior","Archer","Tank","Mage","Shinobi"},n=>"Class_"+n));home.gameObject.SetActive(!show);classes.gameObject.SetActive(show);FadeIn(show?classes:home);(show?classes:home).GetComponentInChildren<Button>().Select();}
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
            RefreshClassHint();
        }
        static void FadeClassLabel(Text label)
        {
            var group=label.GetComponent<CanvasGroup>();if(!group)group=label.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;group.DOKill();group.alpha=.55f;group.DOFade(1,.25f).SetUpdate(true).SetLink(label.gameObject);
        }
        void ShowOptions(bool show){home.gameObject.SetActive(!show);options.gameObject.SetActive(show);FadeIn(show?options:home);(show?options:home).GetComponentInChildren<Button>().Select();RefreshOptions();}
        void Volume(float delta){AudioListener.volume=Mathf.Clamp01(AudioListener.volume+delta);PlayerPrefs.SetFloat("Codice.Volume",AudioListener.volume);PlayerPrefs.Save();RefreshOptions();}
        void Sensitivity(float delta){PlayerPrefs.SetFloat("Codice.Sensitivity",Mathf.Clamp(PlayerPrefs.GetFloat("Codice.Sensitivity",.09f)+delta,.02f,.3f));PlayerPrefs.Save();RefreshOptions();}
        void RefreshOptions(){values.text=$"VOLUMEN GENERAL                         {AudioListener.volume:P0}\nSENSIBILIDAD DEL RATÓN                 {PlayerPrefs.GetFloat("Codice.Sensitivity",.09f):0.00}\nSENSIBILIDAD DEL MANDO                 {Codice.Mechanics.GameInput.PadSensitivity:0.0}x{(Codice.Mechanics.GameInput.InvertY?"  ·  EJE Y INVERTIDO":"")}\n<size=18><color=#A69F8B>{DeviceLine()}</color></size>";}
        void RefreshOptionsLegacy(){values.text=$"VOLUMEN GENERAL                         {AudioListener.volume:P0}\n\nSENSIBILIDAD DEL RATÓN                 {PlayerPrefs.GetFloat("Codice.Sensitivity",.09f):0.00}";}
    }
}

