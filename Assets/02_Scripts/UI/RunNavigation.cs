using Codice.Mechanics;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Codice.UI
{
    public static class RunNavigation
    {
        static AsyncOperation loading;
        static bool transitioning;
        public static bool EndlessMode {get;private set;}
        public static int RequestedExpeditionFloor {get;private set;}=1;
        public static void StartExpeditionTest(Codice.Characters.PlayerClass selected,int floor)
        {
            if(Loading||!MainMenuUI.CheatsUnlocked||Codice.Multiplayer.LanSession.Active)return;
            RequestedExpeditionFloor=Mathf.Clamp(floor,1,RunDirector.FinalExpeditionFloor);
            SelectedClass=selected;EndlessMode=false;PlayerPrefs.DeleteKey("Codice.TestRound");
            Load("Descenso");
        }
        public static Codice.Characters.PlayerClass SelectedClass { get; private set; }
        static SaveSlots.SaveData resume;
        /// Picked up once by the next RunDirector: the floor checkpoint of the active save slot.
        public static SaveSlots.SaveData TakeResume(){var r=resume;resume=null;return r;}
        public static void ContinueSaved()
        {
            if(Loading||!SaveSlots.CanContinue||Codice.Multiplayer.LanSession.Active)return;
            var d=SaveSlots.Current;resume=d;SelectedClass=(Codice.Characters.PlayerClass)d.runClass;
            RequestedExpeditionFloor=Mathf.Clamp(d.runFloor,1,RunDirector.FinalExpeditionFloor);EndlessMode=false;PlayerPrefs.DeleteKey("Codice.TestRound");
            Load("Descenso");
        }
        internal static bool SceneLoading=>loading!=null&&!loading.isDone;
        public static bool Loading=>transitioning||SceneLoading;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){resume=null;loading=null;transitioning=false;RequestedExpeditionFloor=1;EndlessMode=false;SelectedClass=Codice.Characters.PlayerClass.Warrior;}
        public static void StartGame()=>StartGame(SelectedClass,1);
        public static void StartGame(Codice.Characters.PlayerClass selected)
        { StartGame(selected,1); }
        public static void StartGame(Codice.Characters.PlayerClass selected,int round)
        { if(Loading)return;SaveSlots.EnsureActive();RequestedExpeditionFloor=1;EndlessMode=false;SelectedClass=selected;round=Mathf.Clamp(round,1,RoundRules.FinalRound);PlayerPrefs.SetInt("Codice.TestRound",round);PlayerPrefs.Save();Load(round==1&&!Codice.Multiplayer.LanSession.Active ? "Prologo" : RoundRules.SceneForRound(round)); }
        public static void StartEndless(Codice.Characters.PlayerClass selected){if(Loading)return;EndlessMode=true;SelectedClass=selected;PlayerPrefs.DeleteKey("Codice.TestRound");Load("Coliseo");}
        public static void ContinueAfterPrologue() { if(!Loading)Load("Descenso"); }
        public static void StartCoop(Codice.Characters.PlayerClass selected,string scene){if(Loading)return;SelectedClass=selected;Load(scene);}
        public static void MainMenu(){EndlessMode=false;if(Codice.Multiplayer.LanSession.Active)Codice.Multiplayer.LanSession.Instance.Leave();Load("MainMenu");}
        static void Load(string scene)
        {
            if(Loading)return;
            if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Missing scene: "+scene);return;}
            transitioning=true; SceneTransition.Begin(scene);
        }
        internal static void FinishTransition(){transitioning=false;}
        internal static void LoadSceneNow(string scene)
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(RunDirector.Instance){RunDirector.Instance.StopAllCoroutines();RunDirector.Instance.gameObject.SetActive(false);Object.Destroy(RunDirector.Instance.gameObject);}
            loading=SceneManager.LoadSceneAsync(scene);
        }
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
