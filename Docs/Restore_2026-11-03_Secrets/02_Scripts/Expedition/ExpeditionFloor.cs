using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Codice.Expedition
{
    [DefaultExecutionOrder(-100)]
    public sealed partial class ExpeditionFloor : MonoBehaviour
    {
        public RunScene context;
        public GameObject floorPrefab,wallPrefab,gatePrefab,torchPrefab,propPrefab,pillarPrefab;
        public Material stoneMaterial;
        public int fixedSeed;
        public int Seed {get;private set;}
        public int CurrentRoom {get;private set;}
        public int PlayerRoom {get;private set;}
        public readonly bool[] Visited=new bool[RoomCount],Cleared=new bool[RoomCount];
        public readonly TreasureChest[] Chests=new TreasureChest[RoomCount];
        public int ClearedRooms {get;private set;}
        public bool InEncounter {get;private set;}
        public bool Finished {get;private set;}
        public ExpeditionLayout Layout {get;private set;}
        public const int RoomCount=ExpeditionLayout.Count;
        public bool IsBossRoom=>CurrentRoom==ExpeditionLayout.Boss;
        public string RoomTitle=>TitleFor(PlayerRoom);
        public string TitleFor(int room)=>room==Layout.MerchantRoom?"Refugio del mercader":new[]{"Vestíbulo de los olvidados","Galería bifurcada","Cripta del juramento","Celdas de la vigilia","Atrio de las dos sendas","Osario de ceniza","Capilla de sangre","Umbral de la condena","Santuario del inquisidor"}[room];
        public string Objective=>Finished?"EL INQUISIDOR HA CAÍDO":InEncounter?IsBossRoom?"DERROTA AL INQUISIDOR":"PURIFICA LA SALA":"ELIGE TU RUTA · ENCUENTRA AL INQUISIDOR";
        public string MapCaption=>string.Join(" — ",Enumerable.Range(0,6).Select(i=>i==CurrentRoom?"["+(i==5?"JEFE":i==2?"RELIQUIA":(i+1).ToString())+"]":i<CurrentRoom?"◆":"◇"));
        readonly List<GameObject> entrances=new List<GameObject>(),exits=new List<GameObject>();
        readonly Dictionary<int,List<GameObject>> seals=new Dictionary<int,List<GameObject>>();
        RunDirector director;NavMeshSurface surface;VolumeProfile profile;
        bool ready;float retryEncounterAfter;
        void Awake()
        {
            if(!context)context=GetComponent<RunScene>();
            Seed=Codice.Multiplayer.LanSession.Active?Codice.Multiplayer.LanSession.Instance.DungeonSeed:fixedSeed!=0?fixedSeed:Guid.NewGuid().GetHashCode();Layout=new ExpeditionLayout(Seed);
            if(!Layout.Connected())throw new InvalidOperationException("Disconnected expedition");
            context.dungeon.startOnPlay=false;context.dungeon.merchant=null;
            context.arena.startOnPlay=false;context.arena.managedByRun=true;
            context.dungeon.expeditionPrefab=Resources.Load<BasicEnemy>("ExpeditionEnemies/Hellhound");
            context.arena.expeditionPrefab=Resources.Load<Codice.Bosses.BossCharacter>("ExpeditionEnemies/HorrorInquisitor");
            if(!context.dungeon.expeditionPrefab || !context.arena.expeditionPrefab)
                Debug.LogError("[Expedition] Falta generar los prefabs de Hellhound o Horror Inquisitor.");
            Build();
            Debug.Log($"[Expedition] Generated seed={Seed}; rooms={RoomCount}; links={Layout.Links.Count}; cells={Layout.Cells.Count}; connected=true");
        }
        public Vector3 Center(int room)=>new Vector3(Layout.Rooms[room].x*4,0,Layout.Rooms[room].y*4);
        GameObject Place(GameObject prefab,Vector3 p,float yaw,Transform parent)
        {
            var go=Instantiate(prefab,p,Quaternion.Euler(0,yaw,0),parent);go.SetActive(true);
            if(stoneMaterial && (prefab==floorPrefab||prefab==wallPrefab||prefab==pillarPrefab))
                foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(stoneMaterial,r.sharedMaterials.Length).ToArray();
            return go;
        }
        void Build()
        {
            var root=new GameObject("Seeded dungeon · "+Seed).transform;root.SetParent(transform);
            var sides=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
            foreach(var cell in Layout.Cells.OrderBy(c=>c.y).ThenBy(c=>c.x))
            {
                var p=new Vector3(cell.x*4,0,cell.y*4);
                Place(floorPrefab,p-Vector3.up*.35f,0,root);
                // Roof uses the same modular stone, rotated to expose its detailed face below.
                int chamber=Layout.RoomAt(p);float ceiling=chamber>=0&&Layout.Width(chamber)>=4?6.8f:5.4f;
                var roof=Place(floorPrefab,p+Vector3.up*ceiling,0,root);roof.transform.rotation=Quaternion.Euler(180,0,0);
                foreach(var collider in roof.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}
                for(int side=0;side<4;side++)if(!Layout.Cells.Contains(cell+sides[side]))
                {
                    var delta=new Vector3(sides[side].x,0,sides[side].y);
                    var wall=Place(wallPrefab,p+delta*2,side*90,root);wall.transform.localScale=new Vector3(1,ceiling/4.3f,1);
                    if((cell.x+cell.y)%4==0)
                    {
                        var torch=Place(torchPrefab,p+delta*1.65f+Vector3.up*1.7f,side*90,root);
                        Glow(torch.transform.position-delta*.5f+Vector3.up*.7f,new Color(1,.36f,.12f),2.8f,10,torch.transform);
                    }
                }
            }
            for(int i=0;i<RoomCount;i++)
            {
                var c=Center(i);seals[i]=new List<GameObject>();
                var boundaries=new HashSet<Vector2Int>();
                foreach(var path in Layout.Corridors)for(int j=1;j<path.Count;j++)
                {
                    bool a=Layout.RoomAt(new Vector3(path[j-1].x*4,0,path[j-1].y*4))==i;
                    bool b=Layout.RoomAt(new Vector3(path[j].x*4,0,path[j].y*4))==i;
                    if(a==b)continue;var sum=path[j-1]+path[j];if(!boundaries.Add(sum))continue;
                    var delta=path[j]-path[j-1];var gate=Place(gatePrefab,new Vector3(sum.x*2,0,sum.y*2),delta.x!=0?90:0,root);
                    gate.name="Room seal "+i;gate.SetActive(false);seals[i].Add(gate);entrances.Add(gate);
                }
                DecorateRoom(i,root);
                var tint=i==2?new Color(.70f,.45f,.2f):i==3?new Color(.25f,.5f,.42f):i==4?new Color(.55f,.25f,.22f):new Color(.33f,.43f,.6f);
                Glow(c+Vector3.up*4,tint,1.5f,24,root);
            }
            var cc=context.player.GetComponent<CharacterController>();cc.enabled=false;
            context.player.transform.SetPositionAndRotation(new Vector3(0,.1f,-3),Quaternion.identity);cc.enabled=true;
            // Closed seals must not partition the navigation bake. Runtime carving handles combat seals.
            foreach(var gate in entrances.Concat(exits))gate.SetActive(false);
            surface=new GameObject("Runtime dungeon navigation").AddComponent<NavMeshSurface>();
            surface.transform.SetParent(root);surface.collectObjects=CollectObjects.All;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask=~(1<<30);Physics.SyncTransforms();surface.BuildNavMesh();
            foreach(var gate in entrances.Concat(exits))
            {var obstacle=gate.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(0,2,0);obstacle.size=new Vector3(4,4,.6f);obstacle.carving=true;}
            var points=new GameObject("Room spawn markers").transform;points.SetParent(root);
            context.dungeon.spawnPoints=Enumerable.Range(0,4).Select(i=>{var t=new GameObject("Guardian "+i).transform;t.SetParent(points);return t;}).ToArray();
            context.arena.spawnPoint=new GameObject("Inquisidor spawn").transform;context.arena.spawnPoint.SetParent(points);
            context.portal?.gameObject.SetActive(false);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.15f,.21f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.022f;RenderSettings.fogColor=new Color(.025f,.03f,.042f);RenderSettings.skybox=null;
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)light.intensity=.2f;
            context.player.view.backgroundColor=RenderSettings.fogColor;context.player.view.clearFlags=CameraClearFlags.SolidColor;
            var cameraData=context.player.view.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;cameraData.volumeLayerMask=1;
            var volume=new GameObject("Ashen dungeon grade").AddComponent<Volume>();volume.transform.SetParent(root);volume.isGlobal=true;volume.priority=10;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.22f;vignette.smoothness.value=.6f;
        }
        void Glow(Vector3 p,Color color,float intensity,float range,Transform parent)
        {var l=new GameObject("Ember light").AddComponent<Light>();l.transform.SetParent(parent);l.transform.position=p;l.color=color;l.type=LightType.Point;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
        public void Begin(RunDirector run)
        {
            director=run;ready=true;ClearedRooms=1;CurrentRoom=0;
            PlayerRoom=0;Visited[0]=Cleared[0]=true;
            if(!context.player.GetComponent<EstusFlask>())context.player.gameObject.AddComponent<EstusFlask>();
            SpawnChest(0,false,false,1);
            director.SetExpeditionPhase(RunPhase.Cleared,"Las celdas de ceniza · Encuentra al inquisidor");
            context.dungeon.Completed+=CompleteRoom;context.arena.onCompleted.AddListener(CompleteRoom);
        }
        void Update()
        {
            if(Codice.Multiplayer.LanSession.Client)return;
            if(ready&&context.player){int room=Layout.RoomAt(context.player.transform.position);if(room>=0&&Visited[room])PlayerRoom=room;}
            if(!ready||Finished||InEncounter||!context.player.State.IsAlive||director.Phase==RunPhase.Defeated||Time.time<retryEncounterAfter)return;
            int next=Layout.RoomAt(context.player.transform.position);
            if(next>=0&&!Cleared[next]&&Layout.Inside(next,context.player.transform.position))EnterRoom(next);
        }
        public bool EnterRoom(int index)
        {
            if(!ready||InEncounter||Finished||index<1||index>=RoomCount||Cleared[index]||!context.player.State.IsAlive)return false;
            if(!Layout.Links.Any(e=>e.y==index&&Cleared[e.x]))return false;
            if(!Layout.Inside(index,context.player.transform.position))return false;
            // Validate spawn surfaces before advancing progression or closing any seal.
            if(index!=Layout.MerchantRoom&&index!=Layout.StatueRoom)
            {
                try
                {
                    var c=Center(index);
                    for(int i=0;i<4;i++)context.dungeon.spawnPoints[i].position=SpawnPosition(c+new Vector3(i%2==0?-3:3,.1f,i<2?3:6));
                    context.arena.spawnPoint.position=SpawnPosition(c+new Vector3(0,.1f,5));
                }
                catch(InvalidOperationException error){retryEncounterAfter=Time.time+2;Debug.LogError(error.Message);return false;}
            }
            int previous=CurrentRoom;CurrentRoom=PlayerRoom=index;Visited[index]=true;director.SetExpeditionRoom(index+1);
            if(index==Layout.MerchantRoom||index==Layout.StatueRoom)
            {
                Cleared[index]=true;ClearedRooms++;director.SetExpeditionPhase(RunPhase.Cleared,"Refugio seguro · Examina las ofrendas");return true;
            }
            if(Codice.Multiplayer.LanSession.Host){int n=0;foreach(var mate in Codice.Multiplayer.LanSession.Instance.Players()){if(mate==context.player||!mate.State.IsAlive)continue;var cc=mate.GetComponent<CharacterController>();cc.enabled=false;mate.transform.position=SpawnPosition(context.player.transform.position+Vector3.right*(++n*1.1f));cc.enabled=true;}}
            InEncounter=true;Seal(index,true);
            director.SetExpeditionPhase(RunPhase.Combat,RoomTitle+" · Los sellos se cierran");
            var hound=context.dungeon.expeditionPrefab;
            var doll=Resources.Load<BasicEnemy>("ExpeditionEnemies/CreepyDoll");var skeleton=Resources.Load<BasicEnemy>("ExpeditionEnemies/CryptSkeleton");
            context.dungeon.expeditionResidents=index==1?new[]{hound,hound,doll?doll:hound}:index==3?new[]{skeleton?skeleton:hound,doll?doll:hound,hound,doll?doll:hound}:new[]{skeleton?skeleton:hound,hound,doll?doll:hound,skeleton?skeleton:hound,hound};
            bool started=IsBossRoom?context.arena.BeginEncounter(5):context.dungeon.BeginEncounter(1+index/3,Roster(index));
            if(!started){InEncounter=false;CurrentRoom=previous;Visited[index]=false;Seal(index,false);retryEncounterAfter=Time.time+2;director.SetExpeditionRoom(CurrentRoom+1);director.SetExpeditionPhase(RunPhase.Cleared,"Reintentando encuentro · Los sellos permanecen abiertos");Debug.LogError("Room encounter failed "+index+" seed="+Seed);return false;}
            Debug.Log($"[Expedition] Room {index+1} entered; boss={IsBossRoom}; seed={Seed}");return true;
        }
        EnemyKind[] Roster(int index)
        {
            var rng=new System.Random(Seed ^ (index*7919));int count=rng.Next(1,index<4?4:6);
            return Enumerable.Range(0,count).Select(i=>(EnemyKind)(i==0?0:rng.Next(0,index==1?2:4))).ToArray();
        }
        void CompleteRoom()
        {
            if(!InEncounter||(!context.player.State.IsAlive&&!Codice.Multiplayer.LanSession.Host))return;
            InEncounter=false;Cleared[CurrentRoom]=true;ClearedRooms++;Seal(CurrentRoom,false);
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ReviveTeam();
            GoldPickup.CollectRemaining(context.player);
            if(IsBossRoom){Finished=true;director.SetExpeditionPhase(RunPhase.Victory,"El inquisidor ha caído. El primer descenso es tuyo.");}
            else{Reward(false);director.SetExpeditionPhase(RunPhase.Cleared,"Sellos quebrados · Elige tu siguiente ruta");}
            Debug.Log($"[Expedition] Room {CurrentRoom+1} cleared; victory={Finished}");
        }
        void Reward(bool relic)
        {
            var p=Center(CurrentRoom)+new Vector3(2,.2f,0);
            if(!relic)LootPickup.Create(p,LootKind.Arrows,12);
            context.player.State.AddGold(relic?50:30);
            SpawnChest(CurrentRoom,relic||CurrentRoom==4,relic,CurrentRoom<=1?1:2);
            if(CurrentRoom==1)RelicAltar.Create(Center(CurrentRoom)+new Vector3(-3,.1f,0),Seed^CurrentRoom*7919,false);
        }
        void Seal(int room,bool closed){foreach(var gate in seals[room])gate.SetActive(closed);}
        public void ApplyCoop(int room,bool[] visited,bool[] cleared,bool encounter,bool finished)
        {
            if(room<0||room>=RoomCount||visited?.Length!=RoomCount||cleared?.Length!=RoomCount)return;
            CurrentRoom=room;InEncounter=encounter;Finished=finished;ClearedRooms=0;
            for(int i=0;i<RoomCount;i++){Visited[i]=visited[i];Cleared[i]=cleared[i];if(Cleared[i])ClearedRooms++;Seal(i,encounter&&i==room);}
            int local=Layout.RoomAt(context.player.transform.position);if(local>=0)PlayerRoom=local;
        }
        public void EnsureCoopChest(int room,bool opened){if(room<0||room>=RoomCount)return;SpawnChest(room,false,false,room<=1?1:2);Chests[room].ApplyLan(opened);}
        void OnDestroy()
        {
            if(context&&context.dungeon)context.dungeon.Completed-=CompleteRoom;
            if(context&&context.arena)context.arena.onCompleted.RemoveListener(CompleteRoom);
            if(surface)surface.RemoveData();if(profile)Destroy(profile);
        }
    }
}
