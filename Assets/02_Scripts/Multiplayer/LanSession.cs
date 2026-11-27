using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using Codice.Mechanics;
using Codice.Presentation;
using Codice.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Codice.Multiplayer
{
    [DefaultExecutionOrder(-150)]
    public sealed partial class LanSession : MonoBehaviour
    {
        /// Inventory commands queued by the guest UI, sent with the next input packet.
        public static int PendingTalismanSlot=-1,PendingTalisman=-1,PendingWeaponSlot=-1,PendingSpellPick=-1;public static bool PendingCast;
        public static LanSession Instance {get;private set;}
        public static bool Active=>Instance;
        public int PartySize=>Mathf.Max(1,members.Length);
        public static bool Host=>Instance&&Instance.host;
        public static bool Client=>Instance&&Instance.client;
        const string Protocol="CindersCoop-8";
        // Available before ExpeditionFloor.Awake; DungeonSeed remains the run's base seed.
        public int FloorNumber {get;private set;}=1;
        int worldRevision;
        sealed class Peer
        {
            public readonly VitalLedger vitals=new VitalLedger(); public readonly Catalog catalog=new Catalog(); public float motionAt=-1; public bool reviveHeld; public float reviveSeen; public float snapshotRtt=.1f;
            public string ticket; public bool returning,waitingForBoss; public int readyRevision=-1,savedSpellChoice; public RunSpell[] savedSpells;public Vector3 spellTimes;
            public LanWire wire; public int id,playerClass; public string name="Invitado",saved;
            public bool accepted;public float lastSeen,joined;public PlayerCharacter player;public LanMessage input;public int memories;
            public int sentSeq,ackSeq;public float[] sendTimes=new float[64];public float sentAt,stateAt;public string stateSent;
        }
        readonly Dictionary<UnityEngine.Object,int> objectIds=new Dictionary<UnityEngine.Object,int>();int nextObjectId=100;
        int Id(UnityEngine.Object value){if(!objectIds.TryGetValue(value,out int id)){id=nextObjectId++;objectIds[value]=id;}return id;}
        readonly List<Peer> peers=new List<Peer>();
        readonly Dictionary<int,LanGhost> ghosts=new Dictionary<int,LanGhost>();
        readonly List<Vector3> hostScrolls=new List<Vector3>();
        TcpListener listener;LanWire server;LanCatalog catalog;RunScene context;
        bool host,client,connecting,started,leaving;int nextId=1,localId=-1,chosenClass;
        bool lastHeavy,lastAttack,lastBlock;float predictedAt=-9;string address="",nickname="Jugador",status="",loadedScene="",localSaved,hostAnim;float hostAnimLeft;
        float nextLobby,heartbeat,nextSend,lastServer,clientYaw,clientPitch;
        float pingSent=-1,pingReceived=-100,pingMs=-1;
        public float PingMilliseconds=>OwnerPing();
        LanMember[] members=Array.Empty<LanMember>();
        LanMessage pending;
        public bool AnyAlive=>Players().Any(p=>p&&p.State.IsAlive);
        public static void Open()
        {if(Instance)return;new GameObject("Partida LAN").AddComponent<LanSession>();}
        void Awake(){if(Instance){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            nickname=PlayerPrefs.GetString("Cinders.CoopNickname","");
            if(string.IsNullOrEmpty(nickname)){nickname="Peregrino "+Guid.NewGuid().ToString("N").Substring(0,4).ToUpperInvariant();PlayerPrefs.SetString("Cinders.CoopNickname",nickname);}
            catalog=Resources.Load<LanCatalog>("LanCatalog");}
        void OnDestroy(){CloseSockets();if(Instance==this)Instance=null;}
        void OnApplicationQuit(){CloseSockets();}
        void CloseSockets(){if(fusion)_=fusion.Stop();fusion=null;listener?.Stop();listener=null;server?.Dispose();server=null;foreach(var peer in peers)peer.wire.Dispose();}
        public void Leave(){leaving=true;CloseSockets();host=client=false;Destroy(gameObject);}
        void RememberNickname()
        {
            nickname=CleanName(nickname);
            if(nickname.Length==0)nickname=PlayerPrefs.GetString("Cinders.CoopNickname","Peregrino");
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(FindAnyObjectByType<CoopProbe>()||FindAnyObjectByType<CoopLatencyProbe>())return;
#endif
            PlayerPrefs.SetString("Cinders.CoopNickname",nickname);
        }
        string localCodes;
        public void CreateHost()
        {
            RememberNickname();
            if(useFusion){CreateFusionHost();return;}
            try{localCodes=LanRoomCode.LocalCodes();listener=new TcpListener(IPAddress.Any,LanWire.Port);listener.Start(4);host=true;localId=0;status="Comparte tu código LAN y espera a tus compañeros.";RefreshMembers();}
            catch(Exception e){status="No se pudo crear la partida: "+e.Message;listener?.Stop();listener=null;}
        }
        public async void Join()
        {
            if(connecting)return;
            RememberNickname();
            appliedCatalog=0;lastWorldSeq=LatestPoseSequence=0;remoteCatalog.Clear();waitingCatalog.Clear();remoteHeader=bufferedPose=pending=null;
            announcedFloor=null;welcomed=waitingAtBoss=floorHeld=false;readySent=released=-1;worldRevision=0;localSaved=null;
            connecting=true;status="Conectando al servidor…";
            if(useFusion&&string.IsNullOrWhiteSpace(address)){status="Escribe el código de sala.";connecting=false;return;}
            try{var connection=useFusion?await NewFusion().StartSession(false,SessionFor(address)):await LanWire.Connect(address.Trim());if(!this||leaving){connection?.Dispose();return;}server=connection;client=true;lastServer=Time.realtimeSinceStartup;server.Send(JsonUtility.ToJson(new LanMessage{type="hello",version=Protocol,state=AppSessionId,playerClass=chosenClass,name=nickname,memories=Codice.Expedition.DescentMemory.Keys}));status="Esperando al anfitrión…";}
            catch(Exception e){if(this)status=e.Message;}
            finally{if(this)connecting=false;}
        }
        public void StartMatch()
        {
            if(!host||started||!catalog)return;started=true;FloorNumber=1;worldRevision=0;DungeonSeed=Guid.NewGuid().GetHashCode();RunNavigation.StartCoop((PlayerClass)chosenClass,"Descenso");
        }
        public const float ReviveRange=2.6f;
        public PlayerCharacter FallenAllyNear(PlayerCharacter by)=>Players().FirstOrDefault(o=>o&&o!=by&&!o.State.IsAlive&&Vector3.Distance(o.transform.position,by.transform.position)<ReviveRange);
        public IEnumerable<PlayerCharacter> Players()
        {
            if(context&&context.player)yield return context.player;
            foreach(var peer in peers)if(peer.accepted&&peer.player)yield return peer.player;
        }
        public PlayerCharacter NearestPlayer(Vector3 position)=>Players().Where(p=>p.State.IsAlive).OrderBy(p=>(p.transform.position-position).sqrMagnitude).FirstOrDefault();
        public void ReviveTeam()
        {
            if(!context)return;
            foreach(var p in Players())if(!p.State.IsAlive){var saved=p.State.Capture();p.RestartAfterDefeat(context.player.transform.position+Vector3.right*1.5f,Quaternion.identity);p.State.Restore(saved);p.State.NetworkRevive();}
        }
        static void CaptureSeat(Peer peer)
        {
            if(!peer.player)return;
            peer.saved=peer.player.State.Capture();
            var book=peer.player.GetComponent<RunSpellbook>();
            if(!book)return;
            peer.savedSpells=book.Known.ToArray();
            peer.savedSpellChoice=book.Selected;
            peer.spellTimes=new Vector3(book.MildUntil,book.PowerUntil,book.Ready);
        }
        public void SaveTeam(){foreach(var peer in peers)CaptureSeat(peer);}
        public void ResetTeam(){foreach(var peer in peers){peer.saved=null;peer.savedSpells=null;peer.spellTimes=Vector3.zero;if(peer.player)peer.player.State.Initialize((PlayerClass)peer.playerClass);}}
        public void EnterHost(RunScene scene)
        {
            context=scene;loadedScene=scene.gameObject.scene.name;
            FloorNumber=scene.Expedition&&RunDirector.Instance?RunDirector.Instance.FloorNumber:1;
            worldRevision++;births.Clear();
            BeginFloorGate();
            foreach(var peer in peers.Where(p=>p.accepted))SpawnPeer(peer);
        }
        void SpawnPeer(Peer peer)
        {
            if(peer.player||!context)return;
            var holder=new GameObject("Remote player construction");holder.SetActive(false);
            var clone=Instantiate(context.player.gameObject,holder.transform);clone.name="LAN • "+peer.name;
            var player=clone.GetComponent<PlayerCharacter>();var avatar=clone.GetComponent<LanAvatar>()??clone.AddComponent<LanAvatar>();avatar.remote=true;avatar.id=peer.id;avatar.playerClass=(PlayerClass)peer.playerClass;
            foreach(var camera in clone.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.tag="Untagged";}
            foreach(var audio in clone.GetComponentsInChildren<AudioListener>(true))audio.enabled=false;
            foreach(var child in clone.GetComponentsInChildren<Transform>(true))if(child.name.StartsWith("First person arms"))Destroy(child.gameObject);
            var spawn=context.player.transform.position+Vector3.right*(peer.id*1.2f);if(NavMesh.SamplePosition(spawn,out var nav,4,NavMesh.AllAreas))spawn=nav.position;clone.transform.position=spawn;
            clone.transform.SetParent(null,true);Destroy(holder);peer.player=player;
            // The avatar is a live clone of the host player (its serialized state came along: weapons, items, health). A new guest starts clean in its own class.
            if(!string.IsNullOrEmpty(peer.saved))player.RestoreRunState(peer.saved);else player.State.Initialize((PlayerClass)peer.playerClass);if(!peer.returning&&!player.State.IsAlive)player.State.NetworkRevive();
            if(peer.savedSpells!=null){var book=player.GetComponent<RunSpellbook>();if(book){book.Known.Clear();book.Known.AddRange(peer.savedSpells);book.Selected=Mathf.Clamp(peer.savedSpellChoice,0,Mathf.Max(0,book.Known.Count-1));book.MildUntil=peer.spellTimes.x;book.PowerUntil=peer.spellTimes.y;book.Ready=peer.spellTimes.z;}}
            peer.returning=false;peer.vitals.Reset();usedGuardPress.Remove(peer.id);
            foreach(var weapon in player.GetComponent<PlayerCombat>().weaponModels)if(weapon)foreach(var renderer in weapon.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            var equipment=clone.AddComponent<LanHeldWeapon>();equipment.player=player;
            player.State.Died+=()=>RunDirector.Instance?.CheckTeamDefeat();
        }
        public void EnterClient(RunScene scene)
        {
            context=scene;loadedScene=scene.gameObject.scene.name;ghosts.Clear();localSaved=null;
            foreach(var enemy in FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            foreach(var boss in FindObjectsByType<BossCharacter>(FindObjectsSortMode.None))Destroy(boss.gameObject);
            foreach(var pickup in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))Destroy(pickup.gameObject);
            foreach(var pickup in FindObjectsByType<LootPickup>(FindObjectsSortMode.None))Destroy(pickup.gameObject);
            if(!scene.player.GetComponent<EstusFlask>())scene.player.gameObject.AddComponent<EstusFlask>();
            var avatar=scene.player.GetComponent<LanAvatar>()??scene.player.gameObject.AddComponent<LanAvatar>();avatar.playerClass=(PlayerClass)chosenClass;
            if(!RunDirector.Instance)new GameObject("LAN round display").AddComponent<RunDirector>();
            RunHUD.Install(scene);clientYaw=scene.player.transform.eulerAngles.y;clientPitch=0;
            if(pending!=null)Apply(pending);
        }
        public static bool PendingFlask;
        public static readonly Dictionary<int,string> VisualSource=new Dictionary<int,string>();   /* dev: which model each ghost got */
        /// Host: id of the guest whose input is being executed. Announcements and 2D sounds made meanwhile belong to that guest's screen.
        public static int ActingFor=-1;
        public static bool ForwardToGuest=>Host&&ActingFor>0;
        public void SayTo(int owner,string text){EmitEffectTo(owner,"Say:"+text,Vector3.zero,1,0);}
        public void Sound2DTo(int owner,int cue){EmitEffectTo(owner,"Sfx2:"+cue,Vector3.zero,1,0);}
        public string HostLinkInfo=>host?GuestPingText():null;
        public static int LastWorldBytes,LastEntities;public static float AckRtt,Skipped;   /* dev stats */   /* dev probe */
        public const string SfxKey="Sfx:",HitFeelKey="HitFeel";
        /// Host: forward a positional sound to the guests (they heard nothing in coop).
        public void EmitSound(int cue,Vector3 point){if(!host||peers.Count==0)return;EmitEffect(SfxKey+cue,point,1,0);}
        public void EmitEffectTo(int owner,string key,Vector3 point,float scale,float lifetime)
        {if(!host)return;var peer=peers.FirstOrDefault(p=>p.accepted&&p.id==owner);if(peer==null)return;peer.wire.Send(JsonUtility.ToJson(new LanMessage{type="effect",name=key,scene=loadedScene,worldRevision=worldRevision,point=point,scale=scale,lifetime=lifetime}));}
        /// Host: code-made VFX (impacts, slashes, bursts) for the guests. dir travels in 'pos'.
        public void EmitFx(string key,Vector3 point,Vector3 dir,float scale,float lifetime,Color tint)
        {if(!host||peers.Count==0||!context)return;var json=JsonUtility.ToJson(new LanMessage{type="effect",name=key,scene=loadedScene,worldRevision=worldRevision,point=point,pos=dir,scale=scale,lifetime=lifetime,tint=tint});foreach(var peer in peers.Where(p=>p.accepted))peer.wire.Send(json);}
        public void EmitEffect(string key,Vector3 point,float scale,float lifetime)
        {if(!host)return;var json=JsonUtility.ToJson(new LanMessage{type="effect",name=key,scene=loadedScene,worldRevision=worldRevision,point=point,scale=scale,lifetime=lifetime});foreach(var peer in peers.Where(p=>p.accepted))peer.wire.Send(json);}
        void ReceiveEffect(LanMessage msg)
        {
            if(!context||RunNavigation.Loading||msg.scene!=loadedScene||msg.worldRevision!=worldRevision)return;
            if(msg.name==DefenseCheck){msg.lifetime=Time.unscaledTime;incomingContacts.Enqueue(msg);return;}
            if(ReceiveCodeEffect(msg))return;
            var prefab=msg.name.StartsWith("Spell:")?Resources.Load<GameObject>("SpellEffects/"+msg.name.Substring(6)):catalog.Find("Vfx:"+msg.name);
            if(!prefab)return;var effect=Instantiate(prefab,msg.point,Quaternion.identity);effect.transform.localScale*=msg.scale;Destroy(effect,Mathf.Clamp(msg.lifetime,.05f,5));
        }
        /// Effects that are code, not prefabs: Gurdi energy attacks and the deflect glint (drawn on guests, never damaging).
        bool ReceiveCodeEffect(LanMessage msg)
        {
            if(msg.name.StartsWith("IAB:")){Codice.Presentation.ImportedAttackVfx.Burst(msg.point,msg.name=="IAB:1",msg.scale);return true;}
            if(msg.name.StartsWith("IV:")){Codice.Presentation.ImpactVfx.Spawn(msg.name.Substring(3),msg.point,msg.pos,msg.scale,msg.lifetime>0?msg.lifetime:1.2f);return true;}
            if(msg.name.StartsWith("LS:")){float.TryParse(msg.name.Substring(3),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float roll);Codice.Presentation.LordsAttackVfx.SlashTinted(msg.point,msg.pos,msg.tint,msg.scale,roll,msg.lifetime>0?msg.lifetime:.28f);return true;}
            if(msg.name=="LI:"){Codice.Presentation.LordsAttackVfx.ImpactTinted(msg.point,msg.pos,msg.tint,msg.scale);return true;}
            if(msg.name.StartsWith("Say:")){Codice.UI.RunHUD.Instance?.Announce(msg.name.Substring(4));return true;}
            if(msg.name.StartsWith("Sfx2:")){if(int.TryParse(msg.name.Substring(5),out int c2)&&!(Time.time-predictedAt<.6f&&(c2==(int)Codice.Presentation.SoundCue.Swing||c2==(int)Codice.Presentation.SoundCue.Heavy)))Codice.Presentation.GameAudio.Play((Codice.Presentation.SoundCue)c2);return true;}
            if(msg.name.StartsWith(SfxKey)){if(int.TryParse(msg.name.Substring(SfxKey.Length),out int cue))Codice.Presentation.GameAudio.PlayLocalAt((Codice.Presentation.SoundCue)cue,msg.point);return true;}
            if(msg.name==HitFeelKey){Codice.Presentation.CoopHitFeel.Local(context.player,msg.point,msg.scale);return true;}
            var tint=new Color(.7f,.45f,.95f);
            if(msg.name==Codice.Presentation.DeflectGlint.NetKey){Codice.Presentation.DeflectGlint.ShowAt(msg.point);return true;}
            if(msg.name==Codice.Bosses.GuardianEnergy.NetWave){Codice.Bosses.GuardianEnergy.ShockwaveVisual(null,msg.point,null,tint);return true;}
            if(msg.name!=Codice.Bosses.GuardianEnergy.NetCharge&&msg.name!=Codice.Bosses.GuardianEnergy.NetBeam)return false;
            var boss=ghosts.Values.Where(g=>g&&g.snapshot!=null&&g.snapshot.key!=null&&g.snapshot.key.StartsWith("Boss:")).OrderBy(g=>(g.transform.position-msg.point).sqrMagnitude).FirstOrDefault();
            if(!boss)return true;
            if(msg.name==Codice.Bosses.GuardianEnergy.NetCharge){var c=Codice.Bosses.GuardianEnergy.ChargeAt(boss.transform,msg.lifetime,tint);Destroy(c,msg.lifetime+.1f);return true;}
            int owner=Mathf.RoundToInt(msg.scale);Transform target=owner==localId?context.player.transform:ghosts.TryGetValue(owner,out var g2)&&g2?g2.transform:null;
            Codice.Bosses.GuardianEnergy.BeamVisual(boss.transform,target,msg.lifetime,tint);return true;
        }
        void RefreshMembers(){members=new[]{new LanMember{id=0,name=nickname,playerClass=chosenClass}}.Concat(peers.Where(p=>p.accepted).Select(p=>new LanMember{id=p.id,name=p.name,playerClass=p.playerClass})).ToArray();}
        void Update()
        {
            TickCombatContacts();
            if(host)
            {
                while(listener!=null&&listener.Pending())
                {
                    var socket=listener.AcceptTcpClient();
                    if(peers.Count>=3){socket.Close();continue;}
                    peers.Add(new Peer{wire=new LanWire(socket),id=nextId++,joined=Time.realtimeSinceStartup,lastSeen=Time.realtimeSinceStartup});
                }
                for(int i=peers.Count-1;i>=0;i--)
                {
                    var peer=peers[i];int handled=0;
                    while(handled++<32&&peer.wire.TryRead(out var json))
                    {
                        try{var msg=JsonUtility.FromJson<LanMessage>(json);ActingFor=peer.id;try{Handle(peer,msg);}finally{ActingFor=-1;}peer.lastSeen=Time.realtimeSinceStartup;}
                        catch{peer.wire.Dispose();}
                    }
                    if(peer.player&&Time.realtimeSinceStartup-peer.lastSeen>1f){var avatar=peer.player.GetComponent<LanAvatar>();avatar.move=Vector2.zero;avatar.attack=avatar.block=avatar.heavy=false;}
                    if(peer.wire.Closed||Time.realtimeSinceStartup-peer.lastSeen>45||!peer.accepted&&Time.realtimeSinceStartup-peer.joined>5)
                    {KeepSeat(peer);if(fusion)fusion.Drop(peer.wire);peer.wire.Dispose();if(peer.player)Destroy(peer.player.gameObject);peers.RemoveAt(i);RefreshMembers();RunDirector.Instance?.CheckTeamDefeat();status="Un compañero se desconectó.";}
                }
                HostLifecycle();
                TickRevival();
                if(Time.realtimeSinceStartup>=nextSend){nextSend=Time.realtimeSinceStartup+.05f;Broadcast();   /* same-network (IP) mode: 30 Hz, it has the bandwidth */}
            }
            else if(client&&server!=null)
            {
                int handled=0;LanMessage newest=null;while(handled++<64&&server.TryRead(out var json))
                {
                    try{var msg=JsonUtility.FromJson<LanMessage>(json);lastServer=Time.realtimeSinceStartup;if(ClientLifecycleMessage(msg))continue;if(msg.type=="poses"||msg.type=="catalog"){msg=Assemble(msg);if(msg==null)continue;}if(msg.type=="pong"){if(msg.elapsed==pingSent){pingMs=Mathf.Max(0,(Time.realtimeSinceStartup-pingSent)*1000);pingReceived=Time.realtimeSinceStartup;}}else if(msg.type=="lobby"){localId=msg.id;members=msg.members;status="Esperando a que el anfitrión inicie.";}else if(msg.type=="world"&&msg.seq>=lastWorldSeq&&(newest==null||msg.seq>=newest.seq)){localId=msg.id;CarryOwnState(newest,msg);newest=msg;}else if(msg.type=="effect"){try{ReceiveEffect(msg);}catch(Exception fx){Debug.LogWarning("LAN effect skipped: "+fx.Message);}}}
                    catch(Exception e){Debug.LogWarning("LAN: "+e);Disconnect("Datos de partida incompatibles.");break;}
                }
                // Only the newest snapshot is applied; older ones queued behind it are skipped (own state carried forward).
                ClientLifecycle();
                if(newest!=null&&server!=null&&welcomed&&!waitingAtBoss&&announcedFloor!=null&&newest.worldRevision==announcedFloor.worldRevision){try{pending=newest;worldReceived=Time.unscaledTime;Apply(newest);lastWorldSeq=newest.seq;}catch(Exception e){Debug.LogWarning("LAN: "+e);Disconnect("Datos de partida incompatibles.");}}
                if(server!=null&&(server.Closed||Time.realtimeSinceStartup-lastServer>45))Disconnect("Se perdió la conexión con el anfitrión.");
                if(server!=null&&Time.realtimeSinceStartup>=heartbeat){heartbeat=Time.realtimeSinceStartup+1;pingSent=Time.realtimeSinceStartup;Send(new LanMessage{type="ping",elapsed=pingSent});}
            }
        }
        void Disconnect(string message){status=message;client=false;if(fusion){var previous=fusion;fusion=null;_=previous.Stop();}server?.Dispose();server=null;started=false;context=null;pending=null;if(SceneManager.GetActiveScene().name!="MainMenu")RunNavigation.MainMenu();}
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        void Handle(Peer peer,LanMessage msg)
        {
            if(!peer.accepted)
            {
                if(msg.type!="hello"||msg.version!=Protocol||(msg.playerClass<0||msg.playerClass>=ClassNames.Length)){peer.wire.Dispose();return;}
                if(!Admit(peer,msg))peer.wire.Dispose();return;
            }
            if(msg.ack>peer.ackSeq&&msg.ack<=peer.sentSeq){peer.ackSeq=msg.ack;float sent=peer.sendTimes[msg.ack%64];if(sent>0){peer.snapshotRtt=Mathf.Lerp(peer.snapshotRtt,Time.realtimeSinceStartup-sent,.35f);AckRtt=Mathf.Lerp(AckRtt,peer.snapshotRtt,.2f);}}
            if(msg.type=="ready"){if(context&&msg.scene==loadedScene&&msg.worldRevision==worldRevision&&msg.floor==FloorNumber)peer.readyRevision=worldRevision;return;}
            if(msg.type=="ping"){peer.wire.Send(JsonUtility.ToJson(new LanMessage{type="pong",elapsed=msg.elapsed}));return;}
            if(msg.type=="ownerVitals"){AcceptVitals(peer,msg);return;}
            if(HandleCombat(peer,msg))return;
            if(msg.type=="motion"){
                if(!floorHeld&&peer.player&&msg.worldRevision==worldRevision&&msg.scene==loadedScene&&msg.hasPos&&Finite(msg.sampleTime)&&msg.sampleTime>peer.motionAt&&Finite(msg.pos.x)&&Finite(msg.pos.y)&&Finite(msg.pos.z)&&Finite(msg.pitch)&&Finite(msg.yaw)&&Finite(msg.move.x)&&Finite(msg.move.y)){
                    peer.motionAt=msg.sampleTime;
                    var actor=peer.player;var av=actor.GetComponent<LanAvatar>();
                    if(actor.State.IsAlive&&!(actor.GetComponent<ChargedAttack>()?.IaidoActive??false)&&(msg.pos-actor.transform.position).sqrMagnitude<400)
                        av.ReceivePosition(actor,msg.pos,msg.sampleTime,msg.yaw);
                    av.move=Vector2.ClampMagnitude(msg.move,1);av.yaw=msg.yaw%360;av.pitch=Mathf.Clamp(msg.pitch,-85,85);
                    peer.reviveSeen=Time.unscaledTime;
                }return;
            }
            if(msg.type!="input"||floorHeld||!peer.player||msg.scene!=loadedScene||msg.worldRevision!=worldRevision)return;
            if(!Finite(msg.yaw)||!Finite(msg.pitch)||!Finite(msg.move.x)||!Finite(msg.move.y)){peer.wire.Dispose();return;}
            var p=peer.player;var avatar=p.GetComponent<LanAvatar>();
            if(msg.hasPos&&!(p.GetComponent<ChargedAttack>()?.IaidoActive??false)&&Finite(msg.pos.x)&&Finite(msg.pos.y)&&Finite(msg.pos.z)&&p.State.IsAlive&&(msg.pos-p.transform.position).sqrMagnitude<400)
                avatar.ReceivePosition(p,msg.pos,msg.sampleTime,msg.yaw);
            if(msg.hasPos){peer.reviveHeld=msg.reviveHeld;peer.reviveSeen=Time.unscaledTime;}
            bool attackPressed=msg.attack&&!avatar.attack;
            avatar.move=Vector2.ClampMagnitude(msg.move,1);avatar.yaw=msg.yaw%360;avatar.pitch=Mathf.Clamp(msg.pitch,-85,85);avatar.attack=msg.attack;avatar.block=msg.block;avatar.heavy=msg.heavy;avatar.jump|=msg.jump;avatar.dash|=msg.dash;
            if(attackPressed&&!PlayerCombat.BufferedMelee(p.State.ActiveWeapon)){p.transform.rotation=Quaternion.Euler(0,avatar.yaw,0);p.GetComponent<PlayerCombat>().RequestAttack();}
            if(msg.slot>=0&&msg.slot<2)p.State.SelectWeapon(msg.slot);if(msg.spell>=0&&msg.spell<4)p.State.SelectSpell(msg.spell);
            {var book=p.GetComponent<Codice.Characters.RunSpellbook>();if(book){if(msg.spellPick>=0&&msg.spellPick<book.Known.Count)book.Selected=msg.spellPick;if(msg.castSpell)book.Cast();}}
            if(msg.talismanSlot>=0&&msg.talismanSlot<Codice.Characters.TalismanCatalog.Slots&&msg.talisman>=-1&&msg.talisman<Codice.Characters.TalismanCatalog.Count)p.State.EquipTalisman(msg.talismanSlot,msg.talisman);
            if(msg.special)p.GetComponent<PlayerMechanics>().RequestUltimate();if(msg.reload)p.GetComponent<PlayerCombat>().TryReload();
            if(msg.passive>=0&&msg.passive<3)p.TryActivatePassive(msg.passive);
            if(msg.flask&&p.State.IsAlive&&p.State.EstusCharges>0)p.GetComponent<ActorPresentation>()?.Play("Heal",EstusFlask.DrinkSeconds,true);
            if(msg.relic>=0&&msg.relic<2)p.GetComponent<RunRelics>()?.Activate(msg.relic);
            if(msg.forge>=0)Codice.Expedition.Blacksmith.NetworkReforge(p,msg.forge);
            if(msg.altar>=0){var altar=objectIds.FirstOrDefault(x=>x.Value==msg.altar).Key as RelicAltar;if(altar){altar.Interact(p);altar.Claim(p,msg.choice,msg.replace);}}
            var merchant=context.dungeon?context.dungeon.merchant:null;
            if(msg.interact){
                if(msg.interactEntity>=0){var orb=objectIds.FirstOrDefault(e=>e.Value==msg.interactEntity).Key as Codice.Expedition.AshBoonOrb;if(orb&&p.State.IsAlive&&!p.ControlsBlocked&&(orb.transform.position-p.transform.position).sqrMagnitude<=12.25f){var from=p.transform.position+Vector3.up*1.35f;bool clear=true;foreach(var hit in Physics.RaycastAll(from,(orb.transform.position-from).normalized,Vector3.Distance(from,orb.transform.position),~0,QueryTriggerInteraction.Ignore)){if(hit.transform.IsChildOf(p.transform)||hit.transform.IsChildOf(orb.transform)||orb.altar&&hit.transform.IsChildOf(orb.altar.transform))continue;clear=false;break;}if(clear)orb.Interact(p);}}
                else p.TryInteract();
            }
            if(merchant&&merchant.Customer==p){if(msg.buy>=0&&msg.buy<4)merchant.TryPurchase(p,msg.buy);if(msg.closeShop)merchant.CloseShop();}
            // Only the host advances the shared round/portal. Guests can still shop.
        }
        int lastWorldSeq;
        /// The host sends a guest its own CharacterState only when it changed: keep it when skipping to a newer snapshot.
        void CarryOwnState(LanMessage older,LanMessage newer)
        {
            if(older?.entities==null||newer?.entities==null)return;
            var a=older.entities.FirstOrDefault(e=>e.owner==localId&&e.key!=null&&e.key.StartsWith("Player:"));var b=newer.entities.FirstOrDefault(e=>e.owner==localId&&e.key!=null&&e.key.StartsWith("Player:"));
            if(a!=null&&b!=null&&string.IsNullOrEmpty(b.state))b.state=a.state;
        }
        void Send(LanMessage msg){msg.ack=lastWorldSeq;if(msg.type=="input"){msg.worldRevision=worldRevision;msg.floor=FloorNumber;}if(server!=null)server.Send(JsonUtility.ToJson(msg));}
        public void ShopCommand(int buy=-1,bool close=false){Send(new LanMessage{type="input",scene=loadedScene,yaw=clientYaw,pitch=clientPitch,buy=buy,closeShop=close});}
        public void ClientControls(PlayerCharacter p)
        {
            if(!client||server==null||server.Closed||!context)return;
            var keyboard=Keyboard.current;var mouse=Mouse.current;
            var avatar=p.GetComponent<LanAvatar>();if(!avatar)return;
            bool shopping=context.dungeon&&context.dungeon.merchant&&context.dungeon.merchant.Customer==p;
            bool allowed=!floorHeld&&p.State.IsAlive&&!shopping&&!(RunHUD.Instance&&RunHUD.Instance.Paused)&&(Cursor.lockState==CursorLockMode.Locked||Codice.Mechanics.GameInput.UsingGamepad);
            if(!allowed&&(mouse?.leftButton.wasPressedThisFrame??false)&&!shopping&&!(RunHUD.Instance&&RunHUD.Instance.Paused)){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(!allowed&&Codice.Mechanics.GameInput.UsingGamepad&&!shopping&&!(RunHUD.Instance&&RunHUD.Instance.Paused)){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            UpdateOwnedVitals(p);
            p.ClientLocomotion(allowed);if(allowed&&Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Attack)&&p.GetComponent<PlayerCombat>().PredictAttack())predictedAt=Time.time;clientYaw=p.transform.eulerAngles.y;clientPitch=p.Pitch;   /* full local camera/lock-on/motor; host trusts the position */
            var msg=new LanMessage{type="input",scene=loadedScene,yaw=clientYaw,pitch=clientPitch,pos=p.transform.position,sampleTime=Time.unscaledTime,hasPos=true};
            msg.move=allowed?Codice.Mechanics.GameInput.Move():Vector2.zero;
            msg.flask=allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Flask);msg.relic=allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Relic1)?0:allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Relic2)?1:-1;
            msg.attack=allowed&&Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Attack);msg.heavy=allowed&&Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.HeavyAttack);bool heavyEdge=msg.heavy!=lastHeavy||msg.attack!=lastAttack;lastHeavy=msg.heavy;lastAttack=msg.attack;   /* attack press goes out at once (was waiting up to 33 ms) */msg.block=allowed&&Codice.Mechanics.GameInput.Held(Codice.Mechanics.GameAction.Block);bool blockEdge=msg.block!=lastBlock;lastBlock=msg.block;
            msg.jump=allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Jump);msg.dash=allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Dodge);msg.special=allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Special);msg.reload=allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Reload);msg.interact=allowed&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Interact);
            if(allowed){int digit=(keyboard?.digit1Key.wasPressedThisFrame??false)?0:(keyboard?.digit2Key.wasPressedThisFrame??false)?1:(keyboard?.digit3Key.wasPressedThisFrame??false)?2:(keyboard?.digit4Key.wasPressedThisFrame??false)?3:-1;if(p.State.ActiveWeapon?.kind==WeaponKind.Staff)msg.spell=digit;else if(digit<2)msg.slot=digit;if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.SwapWeapon)||Codice.Mechanics.GameInput.WeaponScroll()!=0)msg.slot=1-p.State.ActiveSlot;msg.passive=(keyboard?.tKey.wasPressedThisFrame??false)?0:(keyboard?.yKey.wasPressedThisFrame??false)?1:(keyboard?.uKey.wasPressedThisFrame??false)?2:-1;}
            if(msg.interact&&InteractionTarget.Find(p) is LanInteractProxy proxy){var ghost=proxy.GetComponent<LanGhost>();if(ghost&&ghost.snapshot.key=="Boon")msg.interactEntity=ghost.snapshot.id;}
            if(msg.interact&&InteractionTarget.Find(p) is RelicAltar altar){altar.Interact(p);msg.interact=false;}
            if(shopping){msg.buy=(keyboard?.digit1Key.wasPressedThisFrame??false)?0:(keyboard?.digit2Key.wasPressedThisFrame??false)?1:(keyboard?.digit3Key.wasPressedThisFrame??false)?2:(keyboard?.digit4Key.wasPressedThisFrame??false)?3:-1;msg.closeShop=Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Interact)||(keyboard?.escapeKey.wasPressedThisFrame??false)||Codice.Mechanics.GameInput.PadPressed(Codice.Mechanics.GameAction.Back);}
            if(PendingTalismanSlot>=0){msg.talismanSlot=PendingTalismanSlot;msg.talisman=PendingTalisman;PendingTalismanSlot=-1;}
            if(PendingWeaponSlot>=0){msg.slot=PendingWeaponSlot;PendingWeaponSlot=-1;}
            if(PendingFlask){msg.flask=true;PendingFlask=false;}
            if(msg.flask)msg.flask=p.GetComponent<EstusFlask>()?.TryDrink()==true;
            msg.spellPick=PendingSpellPick;if(PendingCast){msg.castSpell=true;PendingCast=false;
                var book=p.GetComponent<RunSpellbook>();
                if(book&&book.PredictCast())predictedAt=Time.time;
            }
            avatar.move=msg.move;avatar.yaw=clientYaw;avatar.pitch=clientPitch;
            // Send edge-triggered actions immediately; continuous input at 30 Hz.
            msg.reviveHeld=allowed&&GameInput.Held(GameAction.Interact);
            bool edge=msg.reviveHeld!=lastReviveHeld||heavyEdge||blockEdge||msg.flask||msg.relic>=0||msg.jump||msg.dash||msg.special||msg.reload||msg.interact||msg.slot>=0||msg.spell>=0||msg.passive>=0||msg.buy>=0||msg.closeShop||msg.talismanSlot>=0||msg.castSpell;
            lastReviveHeld=msg.reviveHeld;
            if(edge)Send(msg);
            if(Time.realtimeSinceStartup>=nextSend){
                nextSend=Time.realtimeSinceStartup+.05f;
                var motion=new LanMessage{type="motion",scene=loadedScene,worldRevision=worldRevision,ack=lastWorldSeq,
                    sampleTime=msg.sampleTime,pos=msg.pos,hasPos=true,yaw=msg.yaw,pitch=msg.pitch,move=msg.move};
                server.SendFast(JsonUtility.ToJson(motion));
            }
        }
        public static int SnapshotWindow(float roundTrip)=>Mathf.Clamp(Mathf.CeilToInt((Mathf.Clamp(roundTrip,0,1)+.034f)*30),2,8);
        static bool SnapshotReady(Peer peer,float now)=>peer.sentSeq-peer.ackSeq<SnapshotWindow(peer.snapshotRtt)||now-peer.sentAt>=Mathf.Clamp(peer.snapshotRtt*1.5f,.12f,.45f);
        void Broadcast()
        {
            if(!started||!context||!RunDirector.Instance){if(Time.unscaledTime<nextLobby)return;nextLobby=Time.unscaledTime+.5f;foreach(var p in peers.Where(p=>p.accepted))p.wire.Send(JsonUtility.ToJson(new LanMessage{type="lobby",id=p.id,members=members}));return;}
            var director=RunDirector.Instance;var entities=new List<LanEntity>();
            foreach(var p in Players())
            {
                int owner=p==context.player?0:p.GetComponent<LanAvatar>().id;string saved=p.State.Capture();var peer=peers.FirstOrDefault(x=>x.id==owner);if(peer!=null){peer.saved=saved;peer.vitals.Observe(p.State);}
                entities.Add(new LanEntity{id=owner,owner=owner,vitalVersion=peer?.vitals.version??default,manaDelay=p.State.ManaDelay,spells=p.GetComponent<Codice.Characters.RunSpellbook>()?.Known.Select(s=>(int)s).ToArray(),spellSel=p.GetComponent<Codice.Characters.RunSpellbook>()?.Selected??0,amount=Mathf.RoundToInt((p.GetComponent<Codice.Characters.PlayerStamina>()?.Value??100)*10),key="Player:"+(int)p.State.Class,position=peer!=null&&Time.time-p.GetComponent<LanAvatar>().posAt<.35f&&!(p.GetComponent<ChargedAttack>()?.IaidoActive??false)?p.GetComponent<LanAvatar>().LastPosition:p.transform.position,rotation=p.transform.rotation,state=saved,weapon=p.State.ActiveWeapon,armor=p.State.Armor,mana=p.State.Mana,flasks=p.State.EstusCharges,health=p.State.Health,maxHealth=p.State.MaxHealth,name=members.FirstOrDefault(m=>m.id==owner)?.name??"Jugador",animation=p.GetComponent<ActorPresentation>()?.CurrentState,animDur=p.GetComponent<ActorPresentation>()?.ActionDuration??0,animLeft=p.GetComponent<ActorPresentation>()?.ActionLeft??0,forced=p.GetComponent<ChargedAttack>()?.IaidoActive??false});
            }
            foreach(var e in FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))entities.Add(WithHitBounds(new LanEntity{id=Id(e),state=PrefabName(e),key="Enemy:"+e.kind+":"+e.variantName,position=e.transform.position,rotation=e.transform.rotation,health=e.Health,maxHealth=e.MaxHealth,name=e.variantName??e.kind.ToString(),animation=e.GetComponent<ActorPresentation>()?.CurrentState,animDur=e.GetComponent<ActorPresentation>()?.ActionDuration??0,animLeft=e.GetComponent<ActorPresentation>()?.ActionLeft??0},e));
            BloodVisual.Live.RemoveAll(l=>!l);foreach(var l in BloodVisual.Live)if(l.enabled&&l.gameObject.activeInHierarchy&&l.positionCount>1&&!l.GetComponentInParent<EnemyProjectile>()&&!l.GetComponentInParent<Codice.Expedition.DescentPortal>())entities.Add(new LanEntity{id=Id(l),key="Line",points=SampleLine(l),color=l.startColor,amount=Mathf.RoundToInt(l.startWidth*1000),available=l.loop});   /* boss/enemy warnings: guests must see them */
            foreach(var b in FindObjectsByType<BossCharacter>(FindObjectsSortMode.None))entities.Add(WithHitBounds(new LanEntity{id=Id(b),state=PrefabName(b),key="Boss:"+b.kind,position=b.transform.position,rotation=b.transform.rotation,health=b.Health,maxHealth=b.MaxHealth,name=b.kind.ToString(),animation=b.GetComponent<ActorPresentation>()?.CurrentState,animDur=b.GetComponent<ActorPresentation>()?.ActionDuration??0,animLeft=b.GetComponent<ActorPresentation>()?.ActionLeft??0},b));
            foreach(var w in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(w),key="Weapon",position=w.transform.position,rotation=w.transform.rotation,weapon=w.weapon});
            foreach(var l in FindObjectsByType<LootPickup>(FindObjectsSortMode.None))if(l.Available)entities.Add(new LanEntity{id=Id(l),key="Loot",position=l.transform.position,rotation=l.transform.rotation,kind=(int)l.kind,passive=(int)l.passive,amount=l.amount});
            foreach(var shot in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(shot),key="Shot",end=shot.NetVelocity,kind=(int)(shot.GetComponent<SpellFlightVisual>()?shot.GetComponent<SpellFlightVisual>().Style:ProjectileStyle.Arcane),position=shot.transform.position,rotation=shot.transform.rotation});
            foreach(var coin in FindObjectsByType<GoldPickup>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(coin),key="Gold",position=coin.transform.position,rotation=coin.transform.rotation});
            foreach(var chest in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(chest),key="Chest",kind=chest.expeditionRoom,position=chest.transform.position,available=chest.Opened});
            foreach(var scroll in FindObjectsByType<Codice.Characters.SpellPickup>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(scroll),key="Scroll",position=scroll.transform.position});
            foreach(var spell in FindObjectsByType<Codice.Characters.RunSpellProjectile>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(spell),key="Projectile:Spell",kind=(int)(spell.GetComponent<SpellFlightVisual>()?.Style??ProjectileStyle.Arcane),end=spell.NetVelocity,position=spell.transform.position,rotation=spell.transform.rotation});
            if(context.Expedition)foreach(int room in context.Expedition.BrokenDrums)entities.Add(new LanEntity{id=-9000-room,key="Drum",kind=room,available=false});
            foreach(var shot in FindObjectsByType<PlayerProjectile>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(shot),key="Projectile:"+shot.NetworkVisual,end=shot.NetVelocity,position=shot.transform.position,rotation=shot.transform.rotation});
            foreach(var p in Players())if(p.GetComponent<PlayerMechanics>().UltimateActive&&p.State.ActiveWeapon?.kind==WeaponKind.Staff)entities.Add(new LanEntity{id=Id(p.GetComponent<PlayerMechanics>()),key="Beam",position=p.view.transform.position+p.view.transform.forward*.7f,end=p.view.transform.position+p.view.transform.forward*28});
            var merchant=context.dungeon?context.dungeon.merchant:null;
            AddExpeditionEntities(entities);
            IncludeBirths(entities);
            var world=new LanMessage{type="world",sampleTime=Time.unscaledTime,scene=loadedScene,level=director.Level,phase=(int)director.Phase,elapsed=director.Elapsed,roundTime=director.RoundElapsed,kills=director.TotalKills,entities=entities.ToArray(),members=members,defeated=context.dungeon?context.dungeon.Defeated:0,total=context.dungeon?context.dungeon.Total:0,shop=merchant&&merchant.Available,offers=merchant?Enumerable.Range(0,merchant.OfferCount).Select(merchant.GetOffer).ToArray():null,portal=context.portal&&context.portal.Available,customer=merchant&&merchant.Customer?(merchant.Customer==context.player?0:merchant.Customer.GetComponent<LanAvatar>().id):-1};
            world.floor=FloorNumber;world.worldRevision=worldRevision;world.memories=Codice.Expedition.DescentMemory.TeamLearned;
            if(context.Expedition){world.secretRevealed=context.Expedition.SecretRevealed;world.seed=DungeonSeed;world.room=context.Expedition.CurrentRoom;world.visited=context.Expedition.Visited;world.cleared=context.Expedition.Cleared;world.encounter=context.Expedition.InEncounter;}
            var watcher=context.Expedition?context.Expedition.GetComponentInChildren<Codice.Expedition.DestinyEncounter>():null;
            var gate=context.Expedition?context.Expedition.GetComponentInChildren<Codice.Expedition.FinalSanctum>():null;
            world.dialogueStage=watcher?watcher.Stage:-2;world.sanctumOpen=gate&&gate.Opened;
            world.orbs=director.DestinyOrbs;world.seal=director.LastBellSeal;world.endingTitle=director.EndingTitle;world.endingText=director.EndingText;
            foreach(var peer in peers)if(peer.accepted&&!peer.waitingForBoss)Publish(peer,world,entities,Time.realtimeSinceStartup);
        }
        void Apply(LanMessage msg)
        {
            if(string.IsNullOrEmpty(msg.scene)||!Application.CanStreamedLevelBeLoaded(msg.scene))throw new InvalidOperationException("Falta mapa de la partida.");
            // Pending keeps the latest packet while loading. Never apply old geometry arrays.
            if(RunNavigation.Loading)return;
            int incomingFloor=Mathf.Clamp(msg.floor,1,RunDirector.FinalExpeditionFloor);
            if(SceneManager.GetActiveScene().name!=msg.scene||msg.worldRevision!=worldRevision||incomingFloor!=FloorNumber||(context&&context.Expedition&&msg.seed!=DungeonSeed))
            {
                context=null;ghosts.Clear();started=true;DungeonSeed=msg.seed;FloorNumber=incomingFloor;worldRevision=msg.worldRevision;RunNavigation.StartCoop((PlayerClass)chosenClass,msg.scene);return;
            }
            if(!context)return;
            RunDirector.Instance.SetNetworkFloorNumber(FloorNumber);
            RunDirector.Instance.ApplyLan(context,msg.level,(RunPhase)msg.phase,msg.elapsed,msg.roundTime,msg.kills);
            if(context.Expedition&&msg.secretRevealed)context.Expedition.RevealSecret();
            ApplyStory(msg);
            LearnTeamMemories(msg.memories);
            if(context.Expedition)context.Expedition.ApplyCoop(msg.room,msg.visited,msg.cleared,msg.encounter,(RunPhase)msg.phase==RunPhase.Victory);
            if(context.dungeon)context.dungeon.ApplyLan(msg.total,msg.defeated);
            if(context.portal)context.portal.SetAvailable(msg.portal);
            var found=new HashSet<int>();hostScrolls.Clear();
            foreach(var entity in msg.entities??Array.Empty<LanEntity>())
            {
                if(entity.key=="Puzzle"){context.Expedition?.ApplyCoopPuzzle(entity.kind,entity.available);continue;}
                if(entity.key=="Drum"){context.Expedition?.ApplyCoopDrum(entity.kind);continue;}
                if(entity.key=="Scroll"){hostScrolls.Add(entity.position);continue;}
                if(entity.key=="Chest"){if(context.Expedition)context.Expedition.EnsureCoopChest(entity.kind,entity.available);else{var chest=FindObjectsByType<TreasureChest>(FindObjectsSortMode.None).FirstOrDefault(c=>Vector3.Distance(c.transform.position,entity.position)<.1f);if(chest)chest.ApplyLan(entity.available);}continue;}
                if(entity.owner==localId)
                {
                    context.player.GetComponent<Codice.Characters.PlayerStamina>()?.SetNetwork(entity.amount/10f);
                    {var book=context.player.GetComponent<Codice.Characters.RunSpellbook>();if(book&&entity.spells!=null){bool same=book.Known.Count==entity.spells.Length;for(int k=0;same&&k<entity.spells.Length;k++)same=(int)book.Known[k]==entity.spells[k];
                     if(!same){bool learned=entity.spells.Length>book.Known.Count;book.Known.Clear();foreach(int k in entity.spells)book.Known.Add((Codice.Characters.RunSpell)k);book.Selected=Mathf.Clamp(entity.spellSel,0,Mathf.Max(0,book.Known.Count-1));if(learned&&book.Known.Count>0)Codice.UI.RunHUD.Instance?.Announce("HECHIZO APRENDIDO · "+Codice.Characters.RunSpellbook.Names[(int)book.Known[book.Known.Count-1]]);}}}
                    bool first=string.IsNullOrEmpty(localSaved);bool wasAlive=context.player.State.IsAlive;if(!string.IsNullOrEmpty(entity.state)){if(first)context.player.RestoreRunState(entity.state);else if(entity.state!=localSaved)context.player.State.Restore(entity.state,true);localSaved=entity.state;}
                    ReceiveOwnedVitals(entity,first);
                    /* the guest owns its position: snap only on spawn, revival, host-driven moves (Iaido) or a huge divergence */if(entity.forced){context.player.NetworkActionUntil=Time.time+.2f;}if(first||(!wasAlive&&context.player.State.IsAlive)||entity.forced||Vector3.Distance(context.player.transform.position,entity.position)>15f){var controller=context.player.GetComponent<CharacterController>();controller.enabled=false;context.player.transform.position=entity.position;controller.enabled=true;}
                    if(!wasAlive&&context.player.State.IsAlive)context.player.EndShop();
                    var pose=context.player.GetComponent<ActorPresentation>();bool restarted=entity.animation==hostAnim&&entity.animLeft>hostAnimLeft+.08f;   /* same combo state started again */
                    bool echo=Time.time-predictedAt<Mathf.Max(.35f,PingMilliseconds/1000f+.2f)&&entity.animation!="Hit"&&entity.animation!="Death"&&entity.animation!="Heal";   /* host confirming an attack we already show */
                    if(pose&&!echo&&(entity.animation!=hostAnim||restarted)&&entity.animLeft>.05f&&ActorPresentation.IsActionState(entity.animation)&&entity.animation!="Dash"&&!context.player.IsDashing){float d=Mathf.Clamp(entity.animDur>0?entity.animDur:.5f,.15f,3f);pose.Play(entity.animation,d,true);context.player.NetworkActionUntil=Time.time+Mathf.Min(entity.animLeft,d)*.85f;}   /* dodge is local; attacks lock movement like on the host */   /* play each host action ONCE at its real speed (before: replayed every packet, squeezed into 0.1 s) */
                    hostAnim=entity.animation;hostAnimLeft=entity.animLeft;   /* locomotion stays local; only host-resolved actions (hits, attacks, death) are mirrored */
                    continue;
                }
                found.Add(entity.id);bool fresh=!ghosts.TryGetValue(entity.id,out var ghost)||!ghost;
                if(!fresh&&entity.key=="Weapon"&&JsonUtility.ToJson(ghost.snapshot.weapon)!=JsonUtility.ToJson(entity.weapon)){Destroy(ghost.gameObject);fresh=true;}
                if(fresh){var go=Visual(entity);ghost=go.AddComponent<LanGhost>();ghosts[entity.id]=ghost;}
                ghost.Apply(entity,fresh,msg.sampleTime);
                if(entity.key.StartsWith("Encounter:")||entity.key.StartsWith("Memory:")||entity.key=="Forge"||entity.key=="Boon")LanInteractProxy.Attach(ghost.gameObject,entity.name);
                if(entity.key=="Companion:Pilgrim"){var a=ghost.GetComponentInChildren<Animator>();if(a&&a.runtimeAnimatorController)a.SetBool("Moving",entity.animation=="Moving");}
            }
            foreach(var id in ghosts.Keys.Where(id=>!found.Contains(id)).ToArray()){if(ghosts[id])Destroy(ghosts[id].gameObject);ghosts.Remove(id);}
            // Scrolls are built locally from the seed; one taken on the host disappears here too.
            if(msg.entities!=null)foreach(var scroll in FindObjectsByType<Codice.Characters.SpellPickup>(FindObjectsSortMode.None))if(!hostScrolls.Any(h=>(h-scroll.transform.position).sqrMagnitude<.25f))Destroy(scroll.gameObject);
            if(context.dungeon&&context.dungeon.merchant)context.dungeon.merchant.ApplyLan(msg.shop,msg.offers,msg.customer==localId?context.player:null);
        }
        /* Same model the host shows (was the ammo box for everything: "pilas" out of the chests). */
        static GameObject LootModel(LanEntity e)
        {
            var k=(LootKind)e.kind;
            if(k==LootKind.Armor){var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.transform.localScale=Vector3.one*.11f;orb.GetComponent<Renderer>().sharedMaterial=LootPickup.ArmorMaterial;orb.SetActive(false);Destroy(orb,1);return orb;}
            if(k==LootKind.Passive)return Resources.Load<GameObject>("CindersDungeon/Passive"+e.passive);
            if(k==LootKind.Mana)return Resources.Load<GameObject>("CindersDungeon/ManaShard");
            if(k==LootKind.Potion)return Resources.Load<GameObject>("CindersDungeon/EstusBottle");
            if(k==LootKind.Shuriken)return Resources.Load<GameObject>("Shuriken");
            if(k==LootKind.Arrows)return Resources.Load<GameObject>("CindersDungeon/ArrowBundle");
            return Resources.Load<GameObject>("CindersDungeon/Cartridges")??PresentationLibrary.Load().ammunitionModel;
        }
        static Vector3[] SampleLine(LineRenderer l){int n=l.positionCount,m=Mathf.Min(n,24);var r=new Vector3[m];for(int i=0;i<m;i++){float f=m==1?0:i*(n-1f)/(m-1);var point=l.GetPosition(Mathf.RoundToInt(f));r[i]=l.useWorldSpace?point:l.transform.TransformPoint(point);}return r;}
        public static void ApplyLine(LineRenderer l,LanEntity e){if(!l||e.points==null)return;l.loop=e.available;l.positionCount=e.points.Length;l.SetPositions(e.points);l.startColor=l.endColor=e.color;l.startWidth=l.endWidth=Mathf.Max(.01f,e.amount/1000f);}
        GameObject Visual(LanEntity entity)
        {
            if(entity.key=="Line"){var g=new GameObject("Network warning");var l=BloodVisual.Line(g,entity.color,entity.amount/1000f);BloodVisual.Live.Remove(l);ApplyLine(l,entity);return g;}
            if(entity.key=="Shot"||entity.key=="Projectile:Spell"){var shot=new GameObject("Network spell");shot.transform.position=entity.position;shot.transform.rotation=entity.rotation;SpellFlightVisual.Add(shot,(ProjectileStyle)Mathf.Clamp(entity.kind,0,3));return shot;}
            if(entity.key=="BoonPlinth"){var plinth=GameObject.CreatePrimitive(PrimitiveType.Cylinder);plinth.transform.localScale=new Vector3(2.4f,.35f,2.4f);var stone=Resources.Load<Material>("WorldFour/Basalto estratificado");if(stone)plinth.GetComponent<Renderer>().sharedMaterial=stone;return plinth;}
            if(entity.key=="Altar")return RelicAltar.Create(entity.position,entity.kind,entity.available).gameObject;
            var living=LivingVisual(entity);if(living)return living;
            GameObject prefab=null;
            if(entity.key.StartsWith("Player:"))
            {
                // Same humanoid model + animator the owner sees on themselves (not the old chibi visuals).
                int index=int.Parse(entity.key.Substring(7));
                prefab=Resources.Load<GameObject>("Presentation/Classes/"+(PlayerClass)index)??Resources.Load<GameObject>("Presentation/Human/HumanPlayer");
            }
            else if(entity.key.StartsWith("Projectile:"))prefab=entity.key=="Projectile:Magic"?PresentationLibrary.Load().magicProjectile:Resources.Load<GameObject>(entity.key=="Projectile:Star"?"Shuriken":"PlayerArrow");
            else if(entity.key.StartsWith("Enemy:")){
                string asset=entity.name switch {
                    "Lobo de hollin" or "Salteador de la cripta" or "Golem funerario" => "MuckResidents/"+entity.name,
                    _ when entity.name!=null&&entity.name.Contains("MUÑECA") => "CreepyDoll",
                    _ when entity.name!=null&&entity.name.Contains("OSARIO") => "CryptSkeleton",
                    _ => "Hellhound"
                };
                prefab=ResolveActor(entity.state,EnemyFolders)??Resources.Load<GameObject>("ExpeditionEnemies/"+asset);
            }
            else if(entity.key.StartsWith("Boss:"))prefab=ResolveActor(entity.state,BossFolders)??Resources.Load<GameObject>(entity.key=="Boss:"+BossKind.AngelKnight?"ExpeditionEnemies/AngelKnight":"ExpeditionEnemies/HorrorInquisitor");
            else if(entity.key=="Weapon")prefab=WeaponVisualCatalog.Load()?.Get(entity.weapon);
            else if(entity.key=="Gold")prefab=Resources.Load<GameObject>("GoldCoin");
            else if(entity.key=="Loot")prefab=LootModel(entity);
            else prefab=catalog?catalog.Find(entity.key):null;
            VisualSource[entity.id]=prefab?prefab.name:"PRIMITIVE";
            var holder=new GameObject("Network visual");holder.SetActive(false);
            GameObject go=prefab?Instantiate(prefab,holder.transform):GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(holder.transform,false);go.name=entity.name??entity.key;
            foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))DestroyImmediate(script);
            foreach(var nav in go.GetComponentsInChildren<NavMeshAgent>(true))DestroyImmediate(nav);
            foreach(var body in go.GetComponentsInChildren<Rigidbody>(true))DestroyImmediate(body);
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))DestroyImmediate(collider);
            if(entity.key=="Shot"||entity.key=="BloodBolt"){go.transform.localScale=Vector3.one*.18f;go.GetComponentInChildren<Renderer>().sharedMaterial=BloodVisual.Material;}
            if(entity.key=="BloodPool"){go.transform.localScale=new Vector3(entity.amount*.02f,.035f,entity.amount*.02f);go.GetComponentInChildren<Renderer>().sharedMaterial=BloodVisual.Material;var block=new MaterialPropertyBlock();block.SetColor("_Color",new Color(.3f,.01f,.025f,.8f));go.GetComponent<Renderer>().SetPropertyBlock(block);}
            if(entity.key=="Beam"){foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.enabled=false;var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Resources.Load<Material>("SpecialGlow");line.startColor=line.endColor=Color.cyan;line.startWidth=line.endWidth=.22f;line.positionCount=2;}
            foreach(var animator in go.GetComponentsInChildren<Animator>(true)){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;if(entity.key.StartsWith("Player:")){animator.runtimeAnimatorController=ClassAnimationControllers.Load((PlayerClass)int.Parse(entity.key.Substring(7)));animator.fireEvents=false;}else animator.fireEvents=false;}
            go.SetActive(true);go.transform.SetParent(null,false);Destroy(holder);
            if(entity.key.StartsWith("Player:")){var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length>0){var bb=skins[0].bounds;foreach(var r in skins)bb.Encapsulate(r.bounds);if(bb.size.y>.2f)go.transform.localScale*=1.8f/bb.size.y;}}
            return go;
        }
        void LegacyOnGUI()
        {
            if(started&&context)
            {
                string text=(useFusion?"FUSION 2":"LAN")+" • "+(host?"Anfitrión":nickname)+" • "+members.Length+"/4";
                if(!context.player.State.IsAlive&&RunDirector.Instance&&RunDirector.Instance.Phase!=RunPhase.Defeated)text+=" • Caído: vuelves en la siguiente ronda";
                GUI.Box(new Rect(Screen.width/2-230,115,460,28),text);return;
            }
            var old=GUI.matrix;float scale=Mathf.Min(Screen.width/960f,Screen.height/650f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-900*scale)/2,20,0),Quaternion.identity,Vector3.one*scale);
            GUI.ModalWindow(26767,new Rect(0,0,900,600),id=>
            {
                GUILayout.Space(20);GUILayout.Label("COOPERATIVO • HASTA 4 JUGADORES");GUILayout.Space(12);
                if(!host&&!client&&!connecting)
                {
                    GUILayout.Label("Tu nombre");nickname=GUILayout.TextField(nickname,20);
                    useFusion=GUILayout.Toggle(useFusion,"Servidor · sala privada por código");
                    GUILayout.Label("Clase inicial");chosenClass=GUILayout.SelectionGrid(chosenClass,new[]{"Caballero","Tanque","Arquero","Mago","Shinobi"},3);
                    if(GUILayout.Button("CREAR PARTIDA",GUILayout.Height(50)))CreateHost();
                    GUILayout.Label(useFusion?"Código de sala":"IP local del anfitrión");address=GUILayout.TextField(address,40);
                    if(GUILayout.Button("UNIRSE",GUILayout.Height(50)))Join();
                }
                else
                {
                    if(host){GUILayout.Label(useFusion?"Código de sala: "+address:"IP local: "+string.Join(" / ",Dns.GetHostAddresses(Dns.GetHostName()).Where(a=>a.AddressFamily==AddressFamily.InterNetwork).Select(a=>a.ToString())));}
                    foreach(var member in members)GUILayout.Label(member.name+" • "+((PlayerClass)member.playerClass));
                    if(host&&GUILayout.Button("INICIAR PARTIDA",GUILayout.Height(55)))StartMatch();
                }
                GUILayout.Space(15);GUILayout.Label(status);
                GUILayout.Label("Misma versión del juego en todos los equipos. La partida termina si sale el anfitrión.");
                GUILayout.Label("El anfitrión inicia la partida y avanza por los portales. Los caídos vuelven en la siguiente ronda.");
                if(GUILayout.Button("VOLVER",GUILayout.Height(45)))Leave();
            },"PARTIDA LAN");GUI.matrix=old;
        }
    }
}


