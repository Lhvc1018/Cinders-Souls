using Codice.Characters;
using Codice.Mechanics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Codice.Presentation
{
    // Persistent audio belongs to the run, not to actors that disappear on death.
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }
        public GameAudioLibrary Library { get; private set; }
        public AudioClip CurrentMusic => music != null ? music[activeMusic].clip : null;
        public int PlayedCount { get; private set; }
        public SoundCue LastCue { get; private set; }
        public bool GameplayPaused { get; private set; }
        AudioSource[] music, voices;
        AudioSource tension, ambience;
        public bool BossTensionActive=>tension&&tension.isPlaying&&tension.volume>.001f;
        bool[] interfaceVoice;
        float[] ready;int[] cueCounts;
        public int Count(SoundCue cue)=>cueCounts==null?0:cueCounts[(int)cue];
        int activeMusic;
        float scanAt;
        RunDirector director;
        RunPhase lastPhase;
        readonly System.Random random=new System.Random();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        { if(!Instance)new GameObject("Game audio").AddComponent<GameAudio>(); }
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            Library=Resources.Load<GameAudioLibrary>("GameAudioLibrary");
            if(!Library){Debug.LogError("Missing GameAudioLibrary. Run Codice/Audio/Install library.");return;}
            AudioListener.volume=PlayerPrefs.GetFloat("Codice.Volume",1);
            music=new[]{Source("Music A"),Source("Music B")};foreach(var s in music){s.loop=true;s.volume=0;s.priority=0;}
            voices=new AudioSource[24];interfaceVoice=new bool[24];for(int i=0;i<voices.Length;i++)voices[i]=Source("Effect "+i);
            tension=Source("Boss critical tension");tension.loop=true;tension.priority=10;
            ambience=Source("Ambient bed");ambience.loop=true;ambience.priority=5;ambience.volume=0;
            ready=new float[System.Enum.GetValues(typeof(SoundCue)).Length];cueCounts=new int[ready.Length];
            SceneManager.sceneLoaded+=SceneLoaded;CombatFeedback.Resolved+=Impact;
        }
        AudioSource Source(string name)
        {var go=new GameObject(name);go.transform.SetParent(transform);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.dopplerLevel=0;return s;}
        void OnDestroy()
        {if(Instance!=this)return;SceneManager.sceneLoaded-=SceneLoaded;CombatFeedback.Resolved-=Impact;Instance=null;}
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(!Library)return;
            foreach(var s in voices)s.Stop();tension.Stop();tension.volume=0;GameplayPaused=false;director=null;scanAt=0;
            System.Array.Clear(ready,0,ready.Length);
            var clip=scene.name=="MainMenu"?Library.menu:(scene.name=="Descenso"||scene.name=="Calabozo"||scene.name=="Criptas"||scene.name=="Bastion")?Library.dungeon:(scene.name=="Coliseo"||scene.name=="FinalArena")?Library.arena:null;
            if(CurrentMusic==clip)return;
            activeMusic=1-activeMusic;var next=music[activeMusic];next.Stop();next.clip=clip;next.volume=0;if(clip)next.Play();
        }
        void Update()
        {
            if(!Library)return;
            bool paused=Time.timeScale==0;
            if(paused!=GameplayPaused)
            {
                GameplayPaused=paused;
                for(int i=0;i<voices.Length;i++)if(!interfaceVoice[i]){if(paused)voices[i].Pause();else voices[i].UnPause();}
            }
            float target=paused?.12f:director&&(director.Phase==RunPhase.Defeated||director.Phase==RunPhase.Cleared||director.Phase==RunPhase.Victory)?.18f:.32f;
            target *= Library.musicVolume;
            for(int i=0;i<2;i++){music[i].volume=Mathf.MoveTowards(music[i].volume,i==activeMusic?target:0,Time.unscaledDeltaTime*.4f);if(i!=activeMusic&&music[i].volume==0)music[i].Stop();}
            var run=RunDirector.Instance;
            if(run&&run.Expedition){var boss=run.Expedition.context.arena.ActiveBoss;
                bool combat=run.Phase==RunPhase.Combat;bool critical=combat&&boss&&boss.IsAlive&&boss.Health<=boss.MaxHealth*.3f;
                bool bossFight=combat&&boss&&boss.IsAlive&&Library.boss;
                SelectMusic(run.Phase==RunPhase.Victory?Library.ending:bossFight?Library.boss:combat?Library.arena:Library.dungeon);
                float level=critical&&!paused?.13f*Library.musicVolume:0;
                tension.volume=Mathf.MoveTowards(tension.volume,level,Time.unscaledDeltaTime*.15f);
                if(critical&&!paused&&!tension.isPlaying&&Library.bossTension){tension.clip=Library.bossTension;tension.Play();}
                if(tension.volume<=0&&tension.isPlaying)tension.Stop();
            }else if(tension.isPlaying){tension.Stop();tension.volume=0;}
            bool expedition=run&&run.Expedition&&run.Phase!=RunPhase.Victory;
            if(expedition&&Library.ambience&&!ambience.isPlaying){ambience.clip=Library.ambience;ambience.Play();}
            ambience.volume=Mathf.MoveTowards(ambience.volume,expedition&&!paused?Library.ambienceVolume:0,Time.unscaledDeltaTime*.2f);
            if(ambience.volume<=0&&ambience.isPlaying&&!expedition)ambience.Stop();
            if(director!=run){director=run;lastPhase=RunPhase.Combat;}
            if(director&&director.Phase!=lastPhase)
            {lastPhase=director.Phase;if(lastPhase==RunPhase.Defeated)Play(SoundCue.Defeat);else if(lastPhase==RunPhase.Victory)Play(director.TrueEndingReady&&!string.IsNullOrEmpty(director.EndingTitle)?SoundCue.Finale:SoundCue.Victory);}
            if(Time.unscaledTime>=scanAt)
            {
                scanAt=Time.unscaledTime+.25f;
                foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(!button.GetComponent<AudioButton>())button.gameObject.AddComponent<AudioButton>();
            }
        }
        void SelectMusic(AudioClip clip)
        {
            if(CurrentMusic==clip)return;SelectMusicCore(clip);}
        void SelectMusicCore(AudioClip clip)
        {activeMusic=1-activeMusic;var next=music[activeMusic];next.Stop();next.clip=clip;next.volume=0;if(clip)next.Play();
        }
        static void Impact(CombatFeedback.Result hit)
        {if(hit.target is PlayerCharacter)return;if(hit.amount>0||hit.blocked)PlayAt(hit.blocked?SoundCue.Block:SoundCue.Hit,hit.position);}
        public static void Play(SoundCue cue){if(Codice.Multiplayer.LanSession.ForwardToGuest){Codice.Multiplayer.LanSession.Instance.Sound2DTo(Codice.Multiplayer.LanSession.ActingFor,(int)cue);return;}Instance?.Emit(cue,Vector3.zero,false);}
        public static void PlayAt(SoundCue cue,Vector3 point){Instance?.Emit(cue,point,true);if(cue!=SoundCue.Footstep&&Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.EmitSound((int)cue,point);}   /* coop: guests hear host-resolved hits, swings, chests... */
        public static void PlayLocalAt(SoundCue cue,Vector3 point)=>Instance?.Emit(cue,point,true);
        void Emit(SoundCue cue,Vector3 point,bool spatial)
        {
            if(!Library||voices==null)return;
            bool ui=cue==SoundCue.Click||cue==SoundCue.Purchase||cue==SoundCue.Error||cue==SoundCue.Victory||cue==SoundCue.Defeat||cue==SoundCue.BossLow||cue==SoundCue.BossPhase||cue==SoundCue.Finale;
            if(!ui&&(Time.timeScale==0||Codice.UI.RunNavigation.Loading))return;
            if(Time.unscaledTime<ready[(int)cue])return;
            var entry=System.Array.Find(Library.effects,e=>e.cue==cue);
            if(entry==null||entry.clips==null||entry.clips.Length==0)return;
            // Reserve a few voices for UI so combat cannot swallow menu feedback.
            int first=ui?20:0,end=ui?24:20,index=-1;
            for(int i=first;i<end;i++)if(!voices[i].isPlaying){index=i;break;}
            if(index<0&&(ui||cue==SoundCue.Parry))index=end-1;
            if(index<0)return;
            var clip=entry.clips[random.Next(entry.clips.Length)];if(!clip)return;
            var source=voices[index];source.Stop();source.transform.position=point;source.clip=clip;
            source.spatialBlend=spatial?1:0;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=2;source.maxDistance=24;
            source.pitch=ui?1:.96f+(float)random.NextDouble()*.08f;source.volume=entry.volume;interfaceVoice[index]=ui;
            source.Play();ready[(int)cue]=Time.unscaledTime+entry.cooldown;LastCue=cue;PlayedCount++;cueCounts[(int)cue]++;
        }
    }
}
