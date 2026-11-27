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
    public sealed class ExpeditionFloor : MonoBehaviour
    {
        public RunScene context;
        public GameObject floorPrefab,wallPrefab,gatePrefab,torchPrefab,propPrefab,pillarPrefab;
        public Material stoneMaterial;
        public int fixedSeed;
        public int Seed {get;private set;}
        public int CurrentRoom {get;private set;}
        public int ClearedRooms {get;private set;}
        public bool InEncounter {get;private set;}
        public bool Finished {get;private set;}
        public ExpeditionLayout Layout {get;private set;}
        public const int RoomCount=6;
        public bool IsBossRoom=>CurrentRoom==5;
        public string RoomTitle=>new[]{"Vestíbulo de los olvidados","Galería de los grilletes","Relicario del peregrino","Celdas de la vigilia","Atrio de la condena","Santuario del inquisidor"}[CurrentRoom];
        public string Objective=>Finished?"EL INQUISIDOR HA CAÍDO":InEncounter?IsBossRoom?"DERROTA AL INQUISIDOR":"ELIMINA A LOS SABUESOS":CurrentRoom==0?"EXPLORA · SIGUE LAS BRASAS":"SALA PURIFICADA · AVANZA AL NORTE";
        public string MapCaption=>string.Join(" — ",Enumerable.Range(0,6).Select(i=>i==CurrentRoom?"["+(i==5?"JEFE":i==2?"RELIQUIA":(i+1).ToString())+"]":i<CurrentRoom?"◆":"◇"));
        readonly List<GameObject> entrances=new List<GameObject>(),exits=new List<GameObject>();
        RunDirector director;NavMeshSurface surface;VolumeProfile profile;
        bool ready;
        void Awake()
        {
            if(!context)context=GetComponent<RunScene>();
            Seed=fixedSeed!=0?fixedSeed:Guid.NewGuid().GetHashCode();Layout=new ExpeditionLayout(Seed);
            if(!Layout.Connected())throw new InvalidOperationException("Disconnected expedition");
            context.dungeon.startOnPlay=false;context.dungeon.merchant=null;
            context.arena.startOnPlay=false;context.arena.managedByRun=true;
            context.dungeon.expeditionPrefab=Resources.Load<BasicEnemy>("ExpeditionEnemies/Hellhound");
            context.arena.expeditionPrefab=Resources.Load<Codice.Bosses.BossCharacter>("ExpeditionEnemies/HorrorInquisitor");
            if(!context.dungeon.expeditionPrefab || !context.arena.expeditionPrefab)
                Debug.LogError("[Expedition] Falta generar los prefabs de Hellhound o Horror Inquisitor.");
            Build();
            Debug.Log($"[Expedition] Generated seed={Seed}; rooms=6; cells={Layout.Cells.Count}; connected=true");
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
                var roof=Place(floorPrefab,p+Vector3.up*5.4f,0,root);roof.transform.rotation=Quaternion.Euler(180,0,0);
                foreach(var collider in roof.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}
                for(int side=0;side<4;side++)if(!Layout.Cells.Contains(cell+sides[side]))
                {
                    var delta=new Vector3(sides[side].x,0,sides[side].y);
                    var wall=Place(wallPrefab,p+delta*2,side*90,root);wall.transform.localScale=new Vector3(1,1.25f,1);
                    if((cell.x+cell.y)%4==0)
                    {
                        var torch=Place(torchPrefab,p+delta*1.65f+Vector3.up*1.7f,side*90,root);
                        Glow(torch.transform.position-delta*.5f+Vector3.up*.7f,new Color(1,.36f,.12f),2.8f,10,torch.transform);
                    }
                }
            }
            for(int i=0;i<RoomCount;i++)
            {
                var c=Center(i);float edge=Layout.Radius(i)*4+2;
                var entrance=Place(gatePrefab,c-Vector3.forward*edge,0,root);entrance.name="Seal · entrance "+i;entrance.SetActive(false);entrances.Add(entrance);
                var exit=Place(gatePrefab,c+Vector3.forward*edge,0,root);exit.name="Seal · exit "+i;exit.SetActive(i>0&&i!=2&&i!=5);exits.Add(exit);
                for(int side=-1;side<=1;side+=2)
                {
                    Place(propPrefab,c+new Vector3(side*(edge-1.5f),0,edge-1.5f),side*20,root);
                    Place(pillarPrefab,c+new Vector3(side*(edge-1.5f),0,-edge+1.5f),0,root);
                }
                Glow(c+Vector3.up*4,new Color(.33f,.43f,.6f),1.2f,18,root);
            }
            var cc=context.player.GetComponent<CharacterController>();cc.enabled=false;
            context.player.transform.SetPositionAndRotation(new Vector3(0,.1f,-3),Quaternion.identity);cc.enabled=true;
            // Closed seals must not partition the navigation bake. Runtime carving handles combat seals.
            foreach(var gate in entrances.Concat(exits))gate.SetActive(false);
            surface=new GameObject("Runtime dungeon navigation").AddComponent<NavMeshSurface>();
            surface.transform.SetParent(root);surface.collectObjects=CollectObjects.All;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask=~(1<<30);Physics.SyncTransforms();surface.BuildNavMesh();
            for(int i=0;i<exits.Count;i++)exits[i].SetActive(i>0&&i!=2&&i!=5);
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
            director.SetExpeditionPhase(RunPhase.Cleared,"Las celdas de ceniza · Encuentra al inquisidor");
            context.dungeon.Completed+=CompleteRoom;context.arena.onCompleted.AddListener(CompleteRoom);
        }
        void Update()
        {
            if(!ready||Finished||InEncounter||!context.player.State.IsAlive||director.Phase==RunPhase.Defeated)return;
            int next=CurrentRoom+1;if(next>=RoomCount)return;
            Vector3 delta=context.player.transform.position-Center(next);float radius=Layout.Radius(next)*4;
            if(Mathf.Abs(delta.x)<radius&&Mathf.Abs(delta.z)<radius)EnterRoom(next);
        }
        public bool EnterRoom(int index)
        {
            if(!ready||InEncounter||Finished||index!=CurrentRoom+1||index>=RoomCount||!context.player.State.IsAlive)return false;
            var delta=context.player.transform.position-Center(index);float radius=Layout.Radius(index)*4;
            if(Mathf.Abs(delta.x)>=radius||Mathf.Abs(delta.z)>=radius)return false;
            CurrentRoom=index;director.SetExpeditionRoom(index+1);
            if(index==2)
            {
                Reward(true);ClearedRooms++;director.SetExpeditionPhase(RunPhase.Cleared,"Relicario · Recuperas fuerzas y encuentras un arma");return true;
            }
            InEncounter=true;entrances[index].SetActive(true);exits[index].SetActive(index<5);
            director.SetExpeditionPhase(RunPhase.Combat,RoomTitle+" · Los sellos se cierran");
            var center=Center(index);
            for(int i=0;i<4;i++)context.dungeon.spawnPoints[i].position=center+new Vector3(i%2==0?-5:5,.1f,i<2?0:5);
            context.arena.spawnPoint.position=center+new Vector3(0,.1f,5);
            bool started=index==5?context.arena.BeginEncounter(5):context.dungeon.BeginEncounter(index==1?1:index==3?2:3,Roster(index));
            if(!started){InEncounter=false;entrances[index].SetActive(false);exits[index].SetActive(false);throw new InvalidOperationException("Room encounter failed "+index);}
            Debug.Log($"[Expedition] Room {index+1} entered; boss={index==5}; seed={Seed}");return true;
        }
        EnemyKind[] Roster(int index)
        {
            var rng=new System.Random(Seed ^ (index*7919));int count=index==1?3:index==3?4:5;
            return Enumerable.Range(0,count).Select(i=>(EnemyKind)(i==0?0:rng.Next(0,index==1?2:4))).ToArray();
        }
        void CompleteRoom()
        {
            if(!InEncounter||!context.player.State.IsAlive)return;
            InEncounter=false;ClearedRooms++;entrances[CurrentRoom].SetActive(false);exits[CurrentRoom].SetActive(false);
            GoldPickup.CollectRemaining(context.player);
            if(IsBossRoom){Finished=true;director.SetExpeditionPhase(RunPhase.Victory,"El inquisidor ha caído. El primer descenso es tuyo.");}
            else{Reward(false);director.SetExpeditionPhase(RunPhase.Cleared,"Sello quebrado · Explora la siguiente sala");}
            Debug.Log($"[Expedition] Room {CurrentRoom+1} cleared; victory={Finished}");
        }
        void Reward(bool relic)
        {
            var p=Center(CurrentRoom)+new Vector3(2,.2f,0);
            if(relic){context.player.State.Heal(context.player.State.MaxHealth*.4f);context.player.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance((WeaponKind)new System.Random(Seed).Next(0,10),2),p);}
            else LootPickup.Create(p,LootKind.Arrows,12);
            context.player.State.AddGold(relic?50:30);
            RelicAltar.Create(Center(CurrentRoom)+new Vector3(-2,.1f,0),Seed^CurrentRoom*7919,relic);
        }
        void OnDestroy()
        {
            if(context&&context.dungeon)context.dungeon.Completed-=CompleteRoom;
            if(context&&context.arena)context.arena.onCompleted.RemoveListener(CompleteRoom);
            if(surface)surface.RemoveData();if(profile)Destroy(profile);
        }
    }
}
