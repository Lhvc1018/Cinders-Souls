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
        Material floorStone;
        public int fixedSeed;
        /// Editor verification only: reproducible seed for the next generated floor (0 = random).
        public static int DebugSeed;
        public ExpeditionContentCatalog contentCatalog;
        public int Seed {get;private set;}
        public int CurrentRoom {get;private set;}
        public int PlayerRoom {get;private set;}
        public bool[] Visited {get;private set;}
        public bool[] Cleared {get;private set;}
        public TreasureChest[] Chests {get;private set;}
        public int ClearedRooms {get;private set;}
        public bool InEncounter {get;private set;}
        public bool Finished {get;private set;}
        public ExpeditionLayout Layout {get;private set;}
        public int FloorNumber=>Codice.Multiplayer.LanSession.Client?Codice.Multiplayer.LanSession.Instance.FloorNumber:RunDirector.Instance?RunDirector.Instance.FloorNumber:Codice.UI.RunNavigation.RequestedExpeditionFloor;
        public string BossTitle=>context.arena.expeditionPrefab?context.arena.expeditionPrefab.name.ToUpperInvariant():"JEFE";
        public int RoomCount=>Layout?.Count??0;
        public bool SecretRevealed {get;private set;}
        public bool IsBossRoom=>CurrentRoom==Layout.Boss;
        public string RoomTitle=>TitleFor(PlayerRoom);
        public string TitleFor(int room)=>room==Layout.Boss?(FloorNumber==3?"Trono del ángel caído":BossTitle):room==Layout.SecretRoom?"Cámara del juramento roto":IsVault(room)?VaultTitle(room):IsMemoryEncounter(room)?"Refugio de los errantes":IsSideRoom(room)?"Cámara lateral · "+(FloorNumber>=13?WorldFiveName:FloorNumber>=10?WorldFourName:CemeteryName):room==Layout.StatueRoom?"Capilla del pacto":room==Layout.MerchantRoom?"Refugio del mercader":FloorNumber>=13?WorldFiveName+" · Sala "+(room+1):FloorNumber>=10?WorldFourName+" · Sala "+(room+1):FloorNumber>=7?CemeteryName+" · Sala "+(room+1):FloorNumber>=4?WorldTwoName+" · Sala "+(room+1):new[]{"Vestíbulo de los olvidados","Galería bifurcada","Cripta del juramento","Celdas de la vigilia","Atrio de las dos sendas","Osario de ceniza","Capilla de sangre","Umbral de la condena","Santuario del inquisidor"}[room];
        public string Objective=>Finished?(FloorNumber<RunDirector.FinalExpeditionFloor?"CRUZA EL PORTAL":director&&director.Phase==RunPhase.Victory?"DESCENSO COMPLETADO":"EXAMINA EL UMBRAL DEL ALBA"):InEncounter?IsBossRoom?"DERROTA AL JEFE":"PURIFICA LA SALA":"ELIGE TU RUTA · ENCUENTRA AL JEFE";
        public string MapCaption=>string.Join(" — ",Enumerable.Range(0,RoomCount).Select(i=>i==CurrentRoom?"["+(i==5?"JEFE":i==2?"RELIQUIA":(i+1).ToString())+"]":i<CurrentRoom?"[+]":"[-]"));
        readonly List<GameObject> entrances=new List<GameObject>(),exits=new List<GameObject>();
        readonly Dictionary<int,List<GameObject>> seals=new Dictionary<int,List<GameObject>>();
        RunDirector director;NavMeshSurface surface;VolumeProfile profile;
        bool ready;float retryEncounterAfter;
        void Awake()
        {
            if(!context)context=GetComponent<RunScene>(); if(!contentCatalog)contentCatalog=Resources.Load<ExpeditionContentCatalog>("CindersDungeon/ContentCatalog");
            Seed=Codice.Multiplayer.LanSession.Active?Codice.Multiplayer.LanSession.Instance.DungeonSeed:fixedSeed!=0?fixedSeed:DebugSeed!=0?DebugSeed:Guid.NewGuid().GetHashCode();DebugSeed=0;Seed=unchecked(Seed+(FloorNumber-1)*104729);Layout=new ExpeditionLayout(Seed,FloorNumber,Codice.Multiplayer.LanSession.Active?0:DescentMemory.Keys);
            Visited=new bool[RoomCount];Cleared=new bool[RoomCount];Chests=new TreasureChest[RoomCount];
            if(!Layout.Connected())throw new InvalidOperationException("Disconnected expedition");
            context.dungeon.startOnPlay=false;context.dungeon.merchant=null;
            context.arena.startOnPlay=false;context.arena.managedByRun=true;
            context.dungeon.expeditionPrefab=Resources.Load<BasicEnemy>("ExpeditionEnemies/Hellhound");
            context.arena.expeditionPrefab=Resources.Load<Codice.Bosses.BossCharacter>(BossResource(Seed,FloorNumber));
            // Campaign boss selection is seeded; editor catalog overrides do not replace world progression.
            if(!context.dungeon.expeditionPrefab || !context.arena.expeditionPrefab)
                Debug.LogError("[Expedition] Falta generar los prefabs de Hellhound o Horror Inquisitor.");
            stoneMaterial=Resources.Load<Material>("CindersStone/Wall")??stoneMaterial;
            floorStone=Resources.Load<Material>("CindersStone/Floor")??stoneMaterial;
            Build();
            Debug.Log($"[Expedition] Generated seed={Seed}; rooms={RoomCount}; links={Layout.Links.Count}; cells={Layout.Cells.Count}; connected=true");
        }
        public static string BossResource(int seed,int floor)=>floor==15?"WorldFive/Bosses/Berserker Orco":floor==14?"WorldFive/Bosses/Rey Trasgo":floor==13?"WorldFive/Bosses/Ogro del Foso":floor==10?"WorldFour/Bosses/Maestre de Escoria":floor==11?"WorldFour/Bosses/Custodio del Contrapeso":floor==12?"WorldFour/Bosses/La Voz del Horno":floor==9?"ExpeditionEnemies/Necromancer":floor==8?"ExpeditionEnemies/Gurdi del Abismo":floor==7?"ExpeditionEnemies/HorrorInquisitor":ExpeditionLayout.GiantBoss(seed,floor)?"ExpeditionEnemies/El Señor de los Terremotos":floor==3?"ExpeditionEnemies/AngelKnight":floor==4?"ExpeditionEnemies/Duende Mutante":floor==5?"ExpeditionEnemies/Gurdi del Abismo":floor==6?"ExpeditionEnemies/Lanza del Infierno":((unchecked((uint)seed*2654435761u)>>16)&1)==0?"ExpeditionEnemies/HorrorInquisitor":"ExpeditionEnemies/MonsterMutant";
        public Vector3 Center(int room)=>new Vector3(Layout.Rooms[room].x*4,Layout.Height(room),Layout.Rooms[room].y*4);
        GameObject Place(GameObject prefab,Vector3 p,float yaw,Transform parent)
        {
            var go=Instantiate(prefab,p,Quaternion.Euler(0,yaw,0),parent);go.SetActive(true);
            if(stoneMaterial && (prefab==floorPrefab||prefab==wallPrefab||prefab==pillarPrefab))
                foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(prefab==floorPrefab?floorStone:stoneMaterial,r.sharedMaterials.Length).ToArray();
            return go;
        }
        void Build()
        {
            var root=new GameObject("Seeded dungeon · "+Seed).transform;root.SetParent(transform);
            var sides=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
            foreach(var cell in Layout.Cells.OrderBy(c=>c.y).ThenBy(c=>c.x))
            {
                var p=new Vector3(cell.x*4,Layout.LevelAt(cell),cell.y*4);
                bool stair=FloorNumber>=7&&BuildWorldThreeCell(cell,root);
                if(FloorNumber<4)Place(floorPrefab,p-Vector3.up*.35f,0,root);
                else if(!stair&&FloorNumber>=13)Camp("Ground",p-Vector3.up*.35f,0,root);
                else if(!stair&&FloorNumber>=10)Forge("Floor",p-Vector3.up*.35f,0,root);
                else if(!stair)Module(FloorNumber>=7?"CemeteryGround":FloorNumber==4?"Grating":"Paving",p-Vector3.up*.35f,0,root);
                // Roof uses the same modular stone, rotated to expose its detailed face below.
                int chamber=Layout.RoomAt(p);float ceiling=FloorNumber==3&&chamber==Layout.Boss?10f:chamber>=0&&Layout.Width(chamber)>=4?6.8f:5.4f;
                if(FloorNumber<4){var roof=Place(floorPrefab,p+Vector3.up*ceiling,0,root);roof.transform.rotation=Quaternion.Euler(180,0,0);
                foreach(var collider in roof.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}}
                for(int side=0;side<4;side++)if(!Layout.Cells.Contains(cell+sides[side]))
                {
                    var delta=new Vector3(sides[side].x,0,sides[side].y);
                    if(FloorNumber>=13)WorldFiveBoundary(p+delta*2,side,root);else if(FloorNumber>=10)WorldFourBoundary(p+delta*2,side,root);else if(FloorNumber>=7)CemeteryBoundary(p+delta*2,side,root);else if(FloorNumber>=4)WorldTwoBoundary(p+delta*2,side,root);else {var wall=Place(wallPrefab,p+delta*2,side*90,root);wall.transform.localScale=new Vector3(1,ceiling/4.3f,1);}
                    if(FloorNumber<10&&(cell.x+cell.y)%4==0)
                    {
                        var torch=Place(torchPrefab,p+delta*1.65f+Vector3.up*1.7f,side*90,root);
                        Codice.Presentation.EmberFire.Attach(torch.transform,torch.transform.position-delta*.35f+Vector3.up*.65f);
                        Glow(Codice.Presentation.EmberFire.Head(torch.transform,torch.transform.position-delta*.35f)+Vector3.up*.15f-delta*.25f,FloorNumber==3&&chamber==Layout.Boss?new Color(.48f,.68f,1):new Color(1,.57f,.30f),2.4f,11,torch.transform);
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
                    var delta=path[j]-path[j-1];var gate=Place(gatePrefab,new Vector3(sum.x*2,Layout.Height(i),sum.y*2),delta.x!=0?90:0,root);
                    gate.name="Room seal "+i;DressSeal(gate);gate.SetActive(false);seals[i].Add(gate);entrances.Add(gate);
                }
                DecorateRoom(i,root);if(FloorNumber>=13)WorldFiveRoom(i,root);else if(FloorNumber>=10)WorldFourRoom(i,root);else if(FloorNumber>=7){CemeteryRoom(i,root);WorldThreeDressing(i,root);}else if(FloorNumber>=4)WorldTwoRoom(i,root);
                var tint=FloorNumber==3&&i==Layout.Boss?new Color(.43f,.48f,.67f):i==2?new Color(.53f,.45f,.32f):i==3?new Color(.34f,.42f,.39f):i==4?new Color(.48f,.33f,.29f):new Color(.40f,.46f,.53f);
                Glow(c+Vector3.up*4,tint,2.4f,24,root);
            }
            // Brann's anvil stands in the pact chapel from 1-2 onward (deterministic, so guests build it too).
            if(FloorNumber>=2&&Layout.StatueRoom>0&&Codice.Prologue.VigiaLessons.ForgeRevealed)Blacksmith.Create(Center(Layout.StatueRoom)+new Vector3(-3.6f,0,-4.5f),90,root,FloorNumber);
            BuildAngelSanctuary(root);
            var cc=context.player.GetComponent<CharacterController>();cc.enabled=false;
            context.player.transform.SetPositionAndRotation(new Vector3(0,.1f,-3),Quaternion.identity);cc.enabled=true;
            DecorateCorridors(root);   // before the bake so corridor props carve the navmesh
            // Closed seals must not partition the navigation bake. Runtime carving handles combat seals.
            foreach(var gate in entrances.Concat(exits))gate.SetActive(false);
            surface=new GameObject("Runtime dungeon navigation").AddComponent<NavMeshSurface>();
            surface.transform.SetParent(root);surface.collectObjects=CollectObjects.All;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask=~(1<<30);Physics.SyncTransforms();surface.BuildNavMesh();
            foreach(var gate in entrances.Concat(exits))
            {var obstacle=gate.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(0,2,0);obstacle.size=new Vector3(4,4,.6f);obstacle.carving=true;}
            BuildSecretWall(root);BuildVault(root);BuildHazards(root);BuildBiomeMechanic(root);
            var points=new GameObject("Room spawn markers").transform;points.SetParent(root);
            context.dungeon.spawnPoints=Enumerable.Range(0,4).Select(i=>{var t=new GameObject("Guardian "+i).transform;t.SetParent(points);return t;}).ToArray();
            context.arena.spawnPoint=new GameObject("Inquisidor spawn").transform;context.arena.spawnPoint.SetParent(points);
            context.portal?.gameObject.SetActive(false);
            Codice.Presentation.PlayerVisibilityLight.ConfigureAmbient();
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.022f;RenderSettings.fogColor=new Color(.025f,.03f,.042f);RenderSettings.skybox=null;
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)light.intensity=.2f;
            context.player.view.backgroundColor=RenderSettings.fogColor;context.player.view.clearFlags=CameraClearFlags.SolidColor;
            var cameraData=context.player.view.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;cameraData.volumeLayerMask=1;cameraData.stopNaN=true;   // NaN pixels from shaders were spread by bloom into white dots
            var volume=new GameObject("Ashen dungeon grade").AddComponent<Volume>();volume.transform.SetParent(root);volume.isGlobal=true;volume.priority=10;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.22f;vignette.smoothness.value=.6f;
            var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
            var grade=profile.Add<ColorAdjustments>(true);grade.saturation.value=-24;grade.contrast.value=4;grade.postExposure.value=.6f;
            var balance=profile.Add<WhiteBalance>(true);balance.temperature.value=-7;
            var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1.3f;bloom.intensity.value=.12f;bloom.scatter.value=.35f;bloom.clamp.value=12;   // tiny over-bright specks (HDR emission) no longer bloom into fireflies
            var grain=profile.Add<FilmGrain>(true);grain.type.value=FilmGrainLookup.Thin1;grain.intensity.value=.12f;grain.response.value=.8f;
            WorldTwoLighting(root);
            if(FloorNumber>=13)WorldFiveAtmosphere(root);else if(FloorNumber>=10)WorldFourAtmosphere(root);else if(FloorNumber>=7)CemeteryAtmosphere(root);
            ApplyLook(root);
        }
        Light Glow(Vector3 p,Color color,float intensity,float range,Transform parent)
        {var l=new GameObject("Ember light").AddComponent<Light>();l.transform.SetParent(parent);l.transform.position=p;l.color=color;l.type=LightType.Point;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;}
        public void Begin(RunDirector run)
        {
            director=run;ready=true;ClearedRooms=1;CurrentRoom=0;foreach(var mate in Team()){mate.State.RefreshFloorBoons();mate.State.RefillEstus();}/* each new floor starts with a full flask */Codice.Prologue.VigiaLessons.Reach(FloorNumber);
            PlayerRoom=0;Visited[0]=Cleared[0]=true;
            if(!context.player.GetComponent<EstusFlask>())context.player.gameObject.AddComponent<EstusFlask>();
            SpawnChest(0,false,false,1);
            BeginLivingDescent();
            director.SetExpeditionPhase(RunPhase.Cleared,FloorNumber>=13?"5-"+(FloorNumber-12)+" · "+WorldFiveName:FloorNumber>=10?"4-"+(FloorNumber-9)+" · "+WorldFourName:FloorNumber>=7?"3-"+(FloorNumber-6)+" · "+CemeteryName:FloorNumber==3?"1-3 · El trono del ángel":FloorNumber>=4?"2-"+(FloorNumber-3)+" · "+WorldTwoName:"1-"+FloorNumber+" · Encuentra al guardián de estas salas");
            context.dungeon.Completed+=CompleteRoom;context.arena.onCompleted.AddListener(CompleteRoom);
            StartPrewarm();
            Codice.UI.FloorTransition.Release();
        }
        void Update()
        {
            // The "you are here" room must follow the player on hosts AND guests (guests returned early before this,
            // so their minimap highlight froze until the next network packet).
            if(Layout!=null&&context&&context.player){int room=Layout.RoomAt(context.player.transform.position);if(room>=0&&room!=PlayerRoom){PlayerRoom=room;if(director&&!Codice.Multiplayer.LanSession.Client)director.SetExpeditionRoom(room+1);}}
            if(Codice.Multiplayer.LanSession.Client)return;
            if(ready)TickCompanions();
            if(ready&&InEncounter&&Codice.Mechanics.JamDebug.FreeRoam&&PlayerRoom>=0&&PlayerRoom!=CurrentRoom)Codice.Mechanics.JamDebug.ClearEncounter();
            if(!ready||Finished||InEncounter||director.Phase==RunPhase.Defeated||Time.time<retryEncounterAfter)return;
            if(Codice.Multiplayer.LanSession.Host){foreach(var participant in Codice.Multiplayer.LanSession.Instance.Players()){if(!participant||!participant.State.IsAlive)continue;int next=Layout.RoomAt(participant.transform.position);if(next>=0&&!Cleared[next]&&Layout.Inside(next,participant.transform.position)&&EnterRoom(next,participant))break;}}
            else {int next=Layout.RoomAt(context.player.transform.position);if(next>=0&&!Cleared[next]&&Layout.Inside(next,context.player.transform.position))EnterRoom(next);}

        }
        public bool CanEnterBoss()=>Layout.Links.Any(e=>(e.y==Layout.Boss&&Cleared[e.x])||(e.x==Layout.Boss&&Cleared[e.y]));
        public bool EnterRoom(int index,PlayerCharacter entrant=null)
        {
            entrant=entrant?entrant:context.player;
            if(Codice.Multiplayer.LanSession.WaitingForFloor||!ready||InEncounter||Finished||index<1||index>=RoomCount||Cleared[index]||!entrant.State.IsAlive)return false;
            if(index==Layout.SecretRoom&&!SecretRevealed)return false;
            bool free=Codice.Mechanics.JamDebug.FreeRoam;
            if(index==Layout.Boss && !CanEnterBoss() && !free)return false;
            if(!free&&!Layout.Links.Any(e=>(e.y==index&&Cleared[e.x])||(e.x==index&&Cleared[e.y])))return false;
            if(!Layout.Inside(index,entrant.transform.position))return false;
            if(Codice.Multiplayer.LanSession.Host) foreach(var mate in Codice.Multiplayer.LanSession.Instance.Players())
                if(mate && mate.State.IsAlive && !Layout.Inside(index,mate.transform.position)) return false;
            // Validate spawn surfaces before advancing progression or closing any seal.
            if(index!=Layout.MerchantRoom&&index!=Layout.StatueRoom&&index!=Layout.SecretRoom&&!IsMemoryEncounter(index)&&!IsVaultTreasure(index))
            {
                try
                {
                    var positions=index==Layout.Boss||IsVaultBoss(index)?null:FarSpawnPositions(index,Roster(index).Length);
                    if(index==Layout.Boss||IsVaultBoss(index)){
                        // Keep large bosses off galleries and clear of perimeter decorations.
                        var wanted=Center(index)-context.player.transform.forward*(ExpeditionLayout.GiantBoss(Seed,FloorNumber)?12:7);
                        if(!NavMesh.SamplePosition(wanted,out var bossPoint,3,NavMesh.AllAreas))throw new InvalidOperationException("Boss floor unavailable; seed="+Seed);
                        context.arena.spawnPoint.position=bossPoint.position;
                    }
                    else {
                        while(context.dungeon.spawnPoints.Length<positions.Length){var marker=new GameObject("Far enemy spawn").transform;marker.SetParent(transform);var markers=context.dungeon.spawnPoints;Array.Resize(ref markers,markers.Length+1);markers[markers.Length-1]=marker;context.dungeon.spawnPoints=markers;}
                        for(int i=0;i<positions.Length;i++)context.dungeon.spawnPoints[i].position=positions[i];
                    }
                }
                catch(InvalidOperationException error){retryEncounterAfter=Time.time+2;Debug.LogError(error.Message);return false;}
            }
            int previous=CurrentRoom;CurrentRoom=PlayerRoom=index;Visited[index]=true;director.SetExpeditionRoom(index+1);
            if(index==Layout.MerchantRoom||index==Layout.StatueRoom||index==Layout.SecretRoom||IsMemoryEncounter(index)||IsVaultTreasure(index))
            {
                if(IsVaultTreasure(index))OpenVaultTreasure(index);
                if(index==Layout.SecretRoom){SpawnChest(index,false,false,3);RelicAltar.Create(Center(index)+new Vector3(-3,.1f,0),Seed^971,false);}
                Cleared[index]=true;ClearedRooms++;director.SetExpeditionPhase(RunPhase.Cleared,"Refugio seguro · Examina las ofrendas");return true;
            }

            InEncounter=true;
            foreach(var companion in FindObjectsByType<DescentCompanion>(FindObjectsSortMode.None))companion.Regroup(this,index);
            Seal(index,true);
            director.SetExpeditionPhase(RunPhase.Combat,RoomTitle+" · Los sellos se cierran");
            var hound=context.dungeon.expeditionPrefab;
            var doll=Resources.Load<BasicEnemy>("ExpeditionEnemies/CreepyDoll");var skeleton=Resources.Load<BasicEnemy>("ExpeditionEnemies/CryptSkeleton");
            context.dungeon.expeditionResidents=index==1?new[]{hound,hound,doll?doll:hound}:index==3?new[]{skeleton?skeleton:hound,doll?doll:hound,hound,doll?doll:hound}:new[]{skeleton?skeleton:hound,hound,doll?doll:hound,skeleton?skeleton:hound,hound};
            if(contentCatalog && contentCatalog.additionalEnemies!=null)context.dungeon.expeditionResidents=context.dungeon.expeditionResidents.Concat(contentCatalog.additionalEnemies.Where(e=>e)).ToArray();
            if(!IsBossRoom)
            {
                // Muck residents are the recurring common enemies: two per room, three from floor 3.
                // Inserted right after the first crypt resident so short rosters (3 enemies) still meet them.
                string[] species = FloorNumber == 1
                    ? new[]{"Lobo de hollin","Salteador de la cripta","Lobo de hollin","Hondero de la cripta"}
                    : FloorNumber == 2
                    ? new[]{"Salteador de la cripta","Lobo de hollin","Golem funerario","Hondero de la cripta"}
                    : new[]{"Golem funerario","Hondero de la cripta","Salteador de la cripta","Lobo de hollin"};
                var rng = new System.Random(Seed ^ (index * 503) ^ (FloorNumber * 173));
                int count = FloorNumber >= 3 ? 3 : 2;
                var residents = context.dungeon.expeditionResidents.ToList();
                int golems = 0;
                for (int i = 0; i < count; i++)
                {
                    string pick = species[rng.Next(species.Length)];
                    if (pick == "Golem funerario" && ++golems > 1) pick = "Salteador de la cripta"; // one slam telegraph at a time stays readable
                    var resident = Resources.Load<BasicEnemy>("ExpeditionEnemies/MuckResidents/" + pick);
                    if (resident) residents.Insert(Mathf.Min(1 + i, residents.Count), resident); // spawn order = list order; rosters can be as short as 3
                }
                context.dungeon.expeditionResidents = residents.ToArray();
            }
            if(FloorNumber>=13&&!IsBossRoom)context.dungeon.expeditionResidents=WorldFiveRoster(index);
            else if(FloorNumber>=10&&!IsBossRoom)context.dungeon.expeditionResidents=WorldFourRoster(index);
            else if(FloorNumber>=7&&!IsBossRoom)context.dungeon.expeditionResidents=CemeteryRoster(index);
            bool started=IsBossRoom?context.arena.BeginEncounter(5):IsVaultBoss(index)?BeginVaultBoss():context.dungeon.BeginEncounter(1+index/3,Roster(index));
            if(!started){InEncounter=false;CurrentRoom=previous;Visited[index]=false;Seal(index,false);retryEncounterAfter=Time.time+2;director.SetExpeditionRoom(CurrentRoom+1);director.SetExpeditionPhase(RunPhase.Cleared,"Reintentando encuentro · Los sellos permanecen abiertos");Debug.LogError("Room encounter failed "+index+" seed="+Seed);return false;}
            Debug.Log($"[Expedition] Room {index+1} entered; boss={IsBossRoom}; seed={Seed}");return true;
        }
        EnemyKind[] Roster(int index)
        {
            var rng=new System.Random(Seed ^ (index*7919));var t=EncounterTable.For(FloorNumber);
            int count=(IsSideRoom(index)?rng.Next(t.sideMin,t.sideMax+1):rng.Next(t.min,(index<4?t.maxEarly:t.maxLate)+1))+Codice.Multiplayer.CoopScaling.ExtraEnemies;
            count=Mathf.Max(t.min,count);   // a combat room is never under the table minimum
            // First enemy is always a frontliner; the rest follow the world's weights (early rooms skip the heaviest kinds).
            return Enumerable.Range(0,count).Select(i=>i==0?EnemyKind.Swordsman:t.Pick(rng,index==1)).ToArray();
        }
        void CompleteRoom()
        {
            if(!InEncounter||(!context.player.State.IsAlive&&!Codice.Multiplayer.LanSession.Host))return;
            InEncounter=false;Cleared[CurrentRoom]=true;ClearedRooms++;Seal(CurrentRoom,false);
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ReviveTeam();
            GoldPickup.CollectRemaining(context.player);
            SpawnChest(CurrentRoom,false,false,Mathf.Clamp(FloorNumber,1,3));
            if(IsBossRoom)
            {
                Finished=true;
                director.RecoverBellSeal();
                if(!Codice.Multiplayer.LanSession.Client&&(FloorNumber==1||FloorNumber==4||FloorNumber==7))RouteMemoryPickup.Create((FloorNumber-1)/3,Center(CurrentRoom)+new Vector3(-4,.8f,0),transform);
                if(FloorNumber==3||FloorNumber==6||FloorNumber==9)
                {
                    director.SetExpeditionPhase(RunPhase.Cleared,"RECUPERA EL ORBE · HABLA CON EL VIGÍA");
                    DestinyEncounter.Create(this);
                }
                else if(FloorNumber<RunDirector.FinalExpeditionFloor)
                {
                    director.SetExpeditionPhase(RunPhase.Cleared,FloorNumber==12?"SELLO DE LA ÚLTIMA CAMPANA RECUPERADO · Cruza el portal":"El guardián ha caído · Cruza el portal");
                    DescentPortal.Create(Center(CurrentRoom)+Vector3.right*5,transform,FloorNumber+1);
                }
                else {director.SetExpeditionPhase(RunPhase.Cleared,"EL UMBRAL DEL ALBA · Examina la entrada y elige tu final");FinalSanctum.Create(this);}
            }
            else{Reward(false);director.SetExpeditionPhase(RunPhase.Cleared,"Sellos quebrados · Elige tu siguiente ruta");if(IsVaultBoss(CurrentRoom))FinishVaultBoss();else TryBoonAltar(CurrentRoom);}
            Debug.Log($"[Expedition] Room {CurrentRoom+1} cleared; victory={Finished}");
        }
        void Reward(bool relic)
        {
            var p=Center(CurrentRoom)+new Vector3(2,.2f,0);
            if(!relic)LootPickup.Create(p,LootKind.Arrows,12);
            context.player.State.AddGold(relic?50:30);
            SpawnChest(CurrentRoom,relic||CurrentRoom==4||IsSideRoom(CurrentRoom),relic,CurrentRoom<=1?1:2);
            if(CurrentRoom==1)RelicAltar.Create(Center(CurrentRoom)+new Vector3(-3,.1f,0),Seed^CurrentRoom*7919,false);
        }
        // Only real changes toggle the portcullis (its SealDrop plays the clang). The coop guest re-applies the room state
        // ~20 times per second; re-playing the sound every packet was the endless "hit" noise guests heard.
        void Seal(int room,bool closed){if(closed&&Codice.Mechanics.JamDebug.FreeRoam)return;foreach(var gate in seals[room])if(gate.activeSelf!=closed)gate.SetActive(closed);}
        public void OpenAllSeals(){for(int i=0;i<RoomCount;i++)foreach(var gate in seals[i])gate.SetActive(false);}
        public void ApplyCoop(int room,bool[] visited,bool[] cleared,bool encounter,bool finished)
        {
            EnsureLedger();
            if(room<0||room>=RoomCount||visited?.Length!=RoomCount||cleared?.Length!=RoomCount)return;
            CurrentRoom=room;InEncounter=encounter;Finished=finished||cleared[Layout.Boss];ClearedRooms=0;
            if(Finished&&FloorNumber!=3&&FloorNumber!=6&&FloorNumber!=9&&FloorNumber<RunDirector.FinalExpeditionFloor&&!GetComponentInChildren<DescentPortal>())DescentPortal.Create(Center(Layout.Boss)+Vector3.right*5,transform,FloorNumber+1);
            for(int i=0;i<RoomCount;i++){Visited[i]=visited[i];Cleared[i]=cleared[i];if(Cleared[i])ClearedRooms++;Seal(i,encounter&&i==room);}
            int local=Layout.RoomAt(context.player.transform.position);if(local>=0)PlayerRoom=local;
        }
        public void EnsureCoopChest(int room,bool opened){if(room<0||room>=RoomCount)return;SpawnChest(room,false,false,room<=1?1:2);Chests[room].ApplyLan(opened);}
        void OnDestroy()
        {
            if(context&&context.dungeon)context.dungeon.Completed-=CompleteRoom;
            if(context&&context.arena)context.arena.onCompleted.RemoveListener(CompleteRoom);
            if(surface)surface.RemoveData();if(profile)Destroy(profile);if(cemeterySky)Destroy(cemeterySky);
        }
    }
}

