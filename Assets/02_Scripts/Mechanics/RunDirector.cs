using System;
using System.Collections;
using Codice.Characters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Codice.Mechanics
{
    public enum RunPhase { Combat, Shop, Cleared, Travelling, Defeated, Victory }
    public sealed partial class RunDirector : MonoBehaviour
    {
        public static RunDirector Instance { get; private set; }
        public int Level { get; private set; }
        public EndlessArena Endless {get;private set;}
        public const int FinalExpeditionFloor = 15;
        public bool DestinyOrbCollected => DestinyOrbs!=0;
        public bool WatcherEncounterComplete {get;private set;}
        
        public void CompleteWatcherEncounter(){if(DestinyOrbCollected)WatcherEncounterComplete=true;}
        public int FloorNumber { get; private set; } = 1;
        public void SetNetworkFloorNumber(int floor){if(Codice.Multiplayer.LanSession.Client)FloorNumber=Mathf.Clamp(floor,1,FinalExpeditionFloor);}
        RunSpell[] carriedSpells; int carriedSpellSelection;
        float carriedMild,carriedPower,carriedSpellReady;
        public float Elapsed { get; private set; }
        public float RoundElapsed { get; private set; }
        public RunPhase Phase { get; private set; }
        public bool CombatActive => Phase == RunPhase.Combat;
        public int TotalKills { get; private set; }
        public bool HiredPilgrim, BondedWisp;
        public float ShopRemaining { get; private set; }
        public PlayerCharacter Player => scene ? scene.player : null;
        public Codice.Expedition.ExpeditionFloor Expedition => scene ? scene.Expedition : null;
        public event Action Changed;
        public event Action<string> Notice;
        RunScene scene;
        string snapshot,checkpoint,checkpointScene;int checkpointLevel;float checkpointElapsed;
        bool transition;
        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; FloorNumber=Codice.UI.RunNavigation.RequestedExpeditionFloor; DontDestroyOnLoad(gameObject);
            var resume=Codice.UI.RunNavigation.TakeResume();
            if(resume!=null)
            {
                snapshot=resume.runState;carriedSpells=System.Array.ConvertAll(resume.runSpells??new int[0],s=>(RunSpell)s);carriedSpellSelection=resume.runSpellSelection;
                DestinyOrbs=resume.runOrbs;LastBellSeal=resume.runBell;WatcherEncounterComplete=resume.runWatcher;HiredPilgrim=resume.runPilgrim;BondedWisp=resume.runWisp;
                Elapsed=resume.runElapsed;TotalKills=resume.runKills;Debug.Log($"[Saves] Continuing descent on floor {FloorNumber}");
            }
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void Enter(RunScene context)
        {
            scene = context;
            if (Level == 0) Level = Mathf.Clamp(PlayerPrefs.GetInt("Codice.TestRound", 1), 1, RoundRules.FinalRound);
            PlayerPrefs.DeleteKey("Codice.TestRound");
            if (!string.IsNullOrEmpty(snapshot)) { context.player.RestoreRunState(snapshot); snapshot = null; }
            if(carriedSpells!=null){var book=context.player.GetComponent<RunSpellbook>();book.Known.Clear();book.Known.AddRange(carriedSpells);book.Selected=Mathf.Clamp(carriedSpellSelection,0,Mathf.Max(0,book.Known.Count-1));book.MildUntil=Time.time+carriedMild;book.PowerUntil=Time.time+carriedPower;book.Ready=Time.time+carriedSpellReady;carriedSpells=null;}
            context.player.State.Died += OnDeath;
            if(context.GetComponent<CombatTestRoom>()){transition=false;SetPhase(RunPhase.Combat);context.GetComponent<CombatTestRoom>().Begin(context);return;}
            if(Codice.UI.RunNavigation.EndlessMode){Level=1;transition=false;Endless=context.gameObject.AddComponent<EndlessArena>();Endless.Begin(context,this);return;}
            if(context.Expedition){Level=1;transition=false;checkpoint=null;context.Expedition.Begin(this);if(!Codice.UI.MainMenuUI.CheatsUnlocked)SaveSlots.Checkpoint(this);return;}
            if (context.dungeon)
            {
                context.dungeon.Completed += RoundCleared;
                if(context.dungeon.merchant){context.dungeon.merchant.onIntermissionStarted.AddListener(OpenShop);context.dungeon.merchant.onRoundContinued.AddListener(CloseShop);}
            }
            if (context.arena) context.arena.onCompleted.AddListener(RoundCleared);
            transition = false; BeginRound();
        }
        void Update()
        {
            if (Codice.Multiplayer.LanSession.Client || !scene || transition) return;
            if (Phase == RunPhase.Shop && scene.dungeon && scene.dungeon.merchant)
            {
                var merchant = scene.dungeon.merchant;
                if (!merchant.Customer) ShopRemaining = Mathf.Max(0, ShopRemaining - Time.deltaTime);
                if (ShopRemaining <= 0 || Codice.Mechanics.GameInput.KeyPressed(Codice.Mechanics.GameAction.Confirm)) merchant.ContinueRound();
            }
            if (Player && (Player.State.IsAlive||Codice.Multiplayer.LanSession.Host&&Codice.Multiplayer.LanSession.Instance.AnyAlive) && Phase != RunPhase.Defeated && Phase != RunPhase.Victory)
            {
                Elapsed += Time.deltaTime;
                if (CombatActive) RoundElapsed += Time.deltaTime;
            }
            if (Phase == RunPhase.Defeated && (Codice.Mechanics.GameInput.KeyPressed(Codice.Mechanics.GameAction.Restart) || Codice.Mechanics.GameInput.PadPressed(Codice.Mechanics.GameAction.Restart))) RestartRun();
        }
        void BeginRound()
        {
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ReviveTeam();
            checkpoint=Player.State.Capture();checkpointScene=SceneManager.GetActiveScene().name;checkpointLevel=Level;checkpointElapsed=Elapsed;
            if(scene.portal)scene.portal.SetAvailable(false);
            RoundElapsed = 0; SetPhase(RunPhase.Combat);
            bool boss = scene.UsesBossEncounter(Level);
            bool started = boss ? scene.arena.BeginEncounter(Level) : scene.UsesDungeonEncounter(Level) && scene.dungeon.BeginEncounter(Level, RoundRules.Roster(Level));
            if (!started) { SetPhase(RunPhase.Cleared); Debug.LogError("No se pudo iniciar la ronda " + Level, scene); return; }
            if (!boss) SpawnLoot();
            Notice?.Invoke("¡Ronda " + Level + " iniciada!");
            Debug.Log("Ronda " + Level + " iniciada.", scene);
        }
        public void SetArenaWave(int wave,bool combat){Level=wave;RoundElapsed=0;SetPhase(combat?RunPhase.Combat:RunPhase.Cleared);Notice?.Invoke(combat?"Oleada "+wave:"Oleada superada · Abre el cofre y pulsa Enter");}
        public void SetExpeditionRoom(int room){if(!Expedition)return;Level=room;RoundElapsed=0;Changed?.Invoke();}
        public void SetExpeditionPhase(RunPhase phase,string notice){if(!Expedition)return;SetPhase(phase);Notice?.Invoke(notice);}
        void OpenShop() { ShopRemaining=25; SetPhase(RunPhase.Shop); Notice?.Invoke("Mercader disponible • [E] Comprar • [Enter] Continuar ronda"); }
        void CloseShop() { if (Phase == RunPhase.Shop) SetPhase(RunPhase.Combat); }
        void RoundCleared()
        {
            if (Phase != RunPhase.Combat || !Player || (!Player.State.IsAlive&&!Codice.Multiplayer.LanSession.Host)) return;
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ReviveTeam();
            GoldPickup.CollectRemaining(Player);
            if (RoundElapsed < 180) { Player.State.AddGold(100); Notice?.Invoke("¡Bono de Rapidez +100 Oro!"); Debug.Log("Bono de rapidez: +100 oro."); }
            SetPhase(RunPhase.Cleared);
            if (RoundRules.IsFinal(Level))
            { SetPhase(RunPhase.Victory); Notice?.Invoke("¡El Ork Berserker ha caído! El Códice de Ceniza ha sido conquistado."); return; }
            if (RoundRules.OpensPortalAfter(Level))
            { if(scene.portal)scene.portal.SetAvailable(true); Notice?.Invoke("¡Portal abierto! Pulsa E para continuar el viaje."); Debug.Log("Portal disponible: acércate y pulsa E para continuar."); }
            else StartCoroutine(NextDungeonRound());
        }
        IEnumerator NextDungeonRound()
        {
            yield return new WaitForSeconds(3);
            if (Phase != RunPhase.Cleared || !Player || (!Player.State.IsAlive&&!Codice.Multiplayer.LanSession.Host)) yield break;
            Level++; BeginRound();
        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// Dev probes only: jump straight to a floor keeping the run (same path as the portal, without its checks).
        public void DevJumpFloor(int floor)
        {
            if(transition||!Player)return;snapshot=Player.State.Capture();var book=Player.GetComponent<RunSpellbook>();carriedSpells=book.Known.ToArray();carriedSpellSelection=book.Selected;
            Player.GetComponent<PlayerMechanics>()?.Cancel();if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.SaveTeam();
            FloorNumber=Mathf.Clamp(floor,1,FinalExpeditionFloor);Level=1;transition=true;SetPhase(RunPhase.Travelling);StartCoroutine(LoadAfterVeil("Descenso"));
        }
#endif
        public bool TravelNextFloor()
        {
            if(FloorNumber==3&&!WatcherEncounterComplete)return false;
            if(transition||FloorNumber>=FinalExpeditionFloor||!Expedition||!Expedition.Finished||Phase!=RunPhase.Cleared||!Player||!Player.State.IsAlive||Codice.Multiplayer.LanSession.Client)return false;
            if(!Application.CanStreamedLevelBeLoaded("Descenso"))return false;
            snapshot=Player.State.Capture();var book=Player.GetComponent<RunSpellbook>();carriedSpells=book.Known.ToArray();carriedSpellSelection=book.Selected;
            carriedMild=Mathf.Max(0,book.MildUntil-Time.time);carriedPower=Mathf.Max(0,book.PowerUntil-Time.time);carriedSpellReady=Mathf.Max(0,book.Ready-Time.time);
            Player.GetComponent<PlayerMechanics>()?.Cancel();
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.SaveTeam();
            FloorNumber++;Level=1;transition=true;SetPhase(RunPhase.Travelling);
            Debug.Log($"[Expedition] Portal crossed: floor {FloorNumber-1} -> {FloorNumber}; inventory preserved");
            Codice.UI.FloorTransition.Show("DESCENDIENDO", $"Piso {(FloorNumber-1)/3+1}-{(FloorNumber-1)%3+1} · el frasco se rellena");
            StartCoroutine(LoadAfterVeil("Descenso"));return true;
        }
        System.Collections.IEnumerator LoadAfterVeil(string scene){yield return new WaitForSecondsRealtime(.36f);SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);}
        public bool Travel()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            if (transition || Phase != RunPhase.Cleared || !scene || !Player || !Player.State.IsAlive) return false;
            string destination = RoundRules.SceneForRound(Level + 1);
            if (!Application.CanStreamedLevelBeLoaded(destination)) { Debug.LogError("Falta escena en Build Settings: " + destination); return false; }
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.SaveTeam();
            snapshot = Player.State.Capture(); Level++; transition = true; SetPhase(RunPhase.Travelling);
            SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
            return true;
        }
        void OnDeath()
        {
            if(Codice.Multiplayer.LanSession.Host&&Codice.Multiplayer.LanSession.Instance.AnyAlive)return;
            StopAllCoroutines();if(Endless)Endless.Halt(); SetPhase(RunPhase.Defeated);
            if(Expedition&&!Codice.UI.MainMenuUI.CheatsUnlocked)SaveSlots.EndRun(false);
            Player.GetComponent<PlayerMechanics>()?.Cancel();
            if (scene.arena && scene.arena.ActiveBoss) scene.arena.ActiveBoss.SetPaused(true);
            foreach (var enemy in FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
            Debug.Log("Partida terminada. Pulsa R para comenzar de nuevo desde el Calabozo.");
        }
        public bool RestartRun()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            if ((Phase != RunPhase.Defeated && Phase != RunPhase.Victory) || transition) return false;
            Time.timeScale=1;
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ResetTeam();
            HiredPilgrim=false;BondedWisp=false;
            transition = true; snapshot = null; carriedSpells=null; FloorNumber=1; ResetEnding(); WatcherEncounterComplete=false; Level = 1; TotalKills=0; Elapsed = RoundElapsed = 0;
            SceneManager.LoadSceneAsync(scene&&scene.GetComponent<CombatTestRoom>()?scene.gameObject.scene.name:Endless?"Coliseo":Expedition?"Descenso":RoundRules.SceneForRound(1), LoadSceneMode.Single); return true;
        }
        public bool RestartLevel()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            if(Expedition||Endless)return RestartRun();
            if(transition||string.IsNullOrEmpty(checkpoint)||(Phase!=RunPhase.Defeated&&Phase!=RunPhase.Cleared))return false;
            StopAllCoroutines();transition=true;snapshot=checkpoint;Level=checkpointLevel;Elapsed=checkpointElapsed;RoundElapsed=0;Time.timeScale=1;
            SetPhase(RunPhase.Travelling);SceneManager.LoadSceneAsync(checkpointScene,LoadSceneMode.Single);return true;
        }
        public void CheckTeamDefeat(){if(Codice.Multiplayer.LanSession.Host&&!Codice.Multiplayer.LanSession.Instance.AnyAlive)OnDeath();}
        public void ApplyLan(RunScene context,int level,RunPhase phase,float elapsed,float roundTime,int kills){scene=context;Level=level;Phase=phase;Elapsed=elapsed;RoundElapsed=roundTime;TotalKills=kills;Changed?.Invoke();}
        void SetPhase(RunPhase phase) { Phase = phase; Changed?.Invoke(); }
        public void RegisterKill(){TotalKills++;Changed?.Invoke();}
        void SpawnLoot()
        {
            // Clean previous round loot; keep quantities bounded across an endless run.
            foreach (var pickup in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None)) Destroy(pickup.gameObject);
            foreach (var pickup in FindObjectsByType<LootPickup>(FindObjectsSortMode.None)) if (!pickup.respawns) Destroy(pickup.gameObject);
            if(scene.lootMarkers==null)return;
            for (int i = 0; i < scene.lootMarkers.Length; i++)
            {
                if (i % 2 == 0) Player.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance((WeaponKind)UnityEngine.Random.Range(0, 10), Mathf.Clamp(2 + (Level - 1) / 5, 2, 4)), scene.lootMarkers[i].position);
                else LootPickup.Create(scene.lootMarkers[i].position, i%6==1?LootKind.Arrows:i%6==3?LootKind.Shuriken:LootKind.Passive, i%6==5?1:8);
            }
        }
    }
}

