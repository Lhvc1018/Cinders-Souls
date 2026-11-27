using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Codice.Mechanics;
namespace Codice.UI
{
    public sealed partial class MainMenuUI
    {
        RectTransform cheats,roundSelector;
        TMP_InputField cheatInput;
        TextMeshProUGUI cheatMessage,roundLabel;
        static bool cheatsUnlocked;
        static int selectedRound=1;
        public static int CheatRound => cheatsUnlocked?selectedRound:1;
        public static bool CheatsUnlocked => cheatsUnlocked;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCheats(){cheatsUnlocked=false;selectedRound=1;}
        public static bool UnlockCheats(string code)
        {if(Codice.Mechanics.SessionCheats.Execute(code))return true;if(!string.Equals(code?.Trim(),"jam",System.StringComparison.OrdinalIgnoreCase)&&code!="Jam2026")return false;cheatsUnlocked=true;return true;}
        public static void SelectTestRound(int round){if(cheatsUnlocked)selectedRound=Mathf.Clamp(round,1,RunDirector.FinalExpeditionFloor);}
        void BuildCheats(Transform parent)
        {
            cheats=Centered(parent,"Trucos",760,900);
            Label(cheats,"Title",40,70,680,85,60,Cream,"Trucos");
            Label(cheats,"Hint",40,180,680,70,24,Muted,"jam: selector de mundos, paso libre y panel F3 (salas, clase). Good!: vida, daño y maná infinito. Good! off: desactivar. Macpato: oro infinito.");
            var field=UIWidgets.Image(cheats,"Código",40,290,680,70,new Color(.1f,.12f,.17f));
            field.raycastTarget=true;
            cheatInput=field.gameObject.AddComponent<TMP_InputField>();cheatInput.targetGraphic=field;
            var viewport=UIWidgets.Rect(field.transform,"Viewport",18,10,644,50);viewport.gameObject.AddComponent<RectMask2D>();
            var inputText=Label(viewport,"Text",0,0,644,50,28,Cream,"");inputText.richText=false;
            var placeholder=Label(viewport,"Placeholder",0,0,644,50,24,Muted,"Código de trucos");
            cheatInput.textViewport=viewport;cheatInput.textComponent=inputText;cheatInput.placeholder=placeholder;cheatInput.characterLimit=32;
            cheatInput.onSubmit.AddListener(_=>ApplyCode());
            Button(cheats,"ACTIVAR",40,385,680,60,ApplyCode,true);
            cheatMessage=Label(cheats,"Result",40,465,680,70,22,Gold,"");
            roundSelector=UIWidgets.Rect(cheats,"Selector de rondas",40,555,680,170);
            roundLabel=Label(roundSelector,"Ronda seleccionada",0,0,680,70,24,Cream,"");roundLabel.alignment=TextAlignmentOptions.Center;
            Button(roundSelector,"ANTERIOR",0,90,325,60,()=>ChangeRound(-1));
            Button(roundSelector,"SIGUIENTE",355,90,325,60,()=>ChangeRound(1));
            Button(roundSelector,"GENERAR Y ENTRAR",0,165,680,55,()=>RunNavigation.StartExpeditionTest(classKinds[selectedClass],selectedRound));
            Button(roundSelector,"PROBAR · EL SEÑOR DE LOS TERREMOTOS",0,228,680,45,()=>Codice.Mechanics.JamDebug.StartEarthquakeLord(classKinds[selectedClass]));
            Button(cheats,"VOLVER",40,838,680,55,()=>ShowCheats(false));
            RefreshCheats();cheats.gameObject.SetActive(false);
        }
        void ApplyCode(){cheatMessage.text=UnlockCheats(cheatInput.text.Trim())?"Código aceptado. " + Codice.Mechanics.SessionCheats.Status:"Código incorrecto.";RefreshCheats();}
        void ChangeRound(int delta){SelectTestRound(selectedRound+delta);RefreshCheats();}
        void RefreshCheats(){roundSelector.gameObject.SetActive(cheatsUnlocked);roundLabel.text=$"MUNDO {(selectedRound-1)/3+1} · ETAPA {(selectedRound-1)%3+1}\nNueva generación procedural";}
        void ShowCheats(bool show){home.gameObject.SetActive(!show);cheats.gameObject.SetActive(show);RefreshCheats();if(show&&!cheatsUnlocked)cheatInput.ActivateInputField();else(show?cheats:home).GetComponentInChildren<Button>().Select();}
    }
}
