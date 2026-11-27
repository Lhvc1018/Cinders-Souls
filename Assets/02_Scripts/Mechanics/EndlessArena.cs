using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.AI;using Unity.AI.Navigation;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Codice.Characters;using Codice.Enemies;
namespace Codice.Mechanics
{
 public sealed class EndlessArena:MonoBehaviour
 {
  public int Wave{get;private set;} public int Total{get;private set;} public int Defeated{get;private set;}
  public bool BetweenWaves{get;private set;} public ArenaSpawnDoor[] Doors{get;private set;}
  RunScene scene;RunDirector run;readonly List<BasicEnemy> live=new List<BasicEnemy>();readonly List<Material> materials=new List<Material>();VolumeProfile profile;NavMeshSurface surface;TreasureChest reward;bool nextRequested;
  public bool ContinueNextWave(){if(!BetweenWaves||!reward||!reward.Opened||!scene.player.State.IsAlive)return false;nextRequested=true;return true;}
  void Update(){if(UnityEngine.InputSystem.Keyboard.current?.enterKey.wasPressedThisFrame==true)ContinueNextWave();}
  public void Begin(RunScene context,RunDirector director)
  {
   scene=context;run=director;if(scene.portal)scene.portal.SetAvailable(false);if(scene.dungeon){scene.dungeon.startOnPlay=false;scene.dungeon.merchant=null;}
   var architecture=GameObject.Find("Coliseo / Architecture");if(architecture)architecture.transform.localScale*=1.35f;
   var shader=Shader.Find("Universal Render Pipeline/Lit");
   if(architecture)foreach(var renderer in architecture.GetComponentsInChildren<Renderer>()){
    var array=renderer.sharedMaterials;for(int i=0;i<array.Length;i++){if(!array[i])continue;var mat=new Material(array[i]);var old=mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor"):mat.color;mat.shader=shader;mat.SetColor("_BaseColor",Color.Lerp(old,new Color(.13f,.14f,.17f),.55f));mat.SetFloat("_Smoothness",.25f);materials.Add(mat);array[i]=mat;}renderer.sharedMaterials=array;
   }
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.15f,.17f,.22f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.025f,.03f,.045f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.009f;
   foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){light.color=new Color(.56f,.65f,.85f);light.intensity=.55f;}
   profile=ScriptableObject.CreateInstance<VolumeProfile>();var volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=50;volume.profile=profile;profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;profile.Add<ColorAdjustments>(true).postExposure.value=.15f;
   Doors=new ArenaSpawnDoor[4];for(int i=0;i<4;i++){var outward=Quaternion.Euler(0,i*90,0)*Vector3.forward;Doors[i]=ArenaSpawnDoor.Create(outward*22,-outward,transform);}
   foreach(var oldSurface in FindObjectsByType<NavMeshSurface>())oldSurface.RemoveData();NavMesh.RemoveAllNavMeshData();
   surface=gameObject.AddComponent<NavMeshSurface>();surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.collectObjects=CollectObjects.All;surface.BuildNavMesh();
   foreach(var door in Doors)door.FinishNavigation();
   StartCoroutine(Waves());Debug.Log("[EndlessArena] Coliseo expanded 1.35x; four animated gates; unlimited waves");
  }
  IEnumerator Waves()
  {
   while(scene.player&&scene.player.State.IsAlive)
   {
    Wave=Wave==int.MaxValue?Wave:Wave+1;Total=Mathf.Min(40,5+Mathf.Min(Wave,18)*2);Defeated=0;BetweenWaves=false;run.SetArenaWave(Wave,true);scene.dungeon.ApplyLan(Total,0);
    for(int i=0;i<Total;i++){
     while(live.Count>=12&&scene.player.State.IsAlive){live.RemoveAll(x=>!x||!x.IsAlive);yield return new WaitForSeconds(.25f);}
     if(!scene.player.State.IsAlive)yield break;
     ArenaSpawnDoor door=null;float best=-1;foreach(var candidate in Doors){float distance=Vector3.Distance(candidate.transform.position,scene.player.transform.position);if(distance>best&&!candidate.Busy){best=distance;door=candidate;}}
     if(!door){i--;yield return new WaitForSeconds(.2f);continue;}door.Open();yield return new WaitUntil(()=>door.IsOpen||!scene.player.State.IsAlive);if(!scene.player.State.IsAlive)yield break;
     if(!NavMesh.SamplePosition(door.SpawnPoint,out var hit,2,NavMesh.AllAreas)){Debug.LogError("[EndlessArena] Gate lacks navigable spawn");door.CloseWhenClear();i--;yield return new WaitForSeconds(1);continue;}
     var prefab=scene.dungeon.prefabs[i%scene.dungeon.prefabs.Length];var enemy=Instantiate(prefab,hit.position,door.transform.rotation,transform);enemy.Initialize(scene.player,Mathf.Min(60,Wave));enemy.Died+=OnDefeated;live.Add(enemy);door.CloseWhenClear();yield return new WaitForSeconds(.75f);
    }
    while(Defeated<Total&&scene.player.State.IsAlive)yield return null;if(!scene.player.State.IsAlive)yield break;
    BetweenWaves=true;nextRequested=false;run.SetArenaWave(Wave,false);GoldPickup.CollectRemaining(scene.player);
    if(reward)Destroy(reward.gameObject);reward=Instantiate(Resources.Load<GameObject>("CindersDungeon/RewardChest"),Vector3.up*.04f,Quaternion.identity,transform).GetComponent<TreasureChest>();reward.expeditionReward=true;reward.price=0;reward.rewardSeed=Wave*7919;reward.rewardTier=Mathf.Clamp(1+Wave/5,1,3);
    // The reward remains until opened; Enter explicitly advances the next wave afterwards.
    while(scene.player.State.IsAlive&&!nextRequested)yield return null;
    yield return new WaitForSeconds(1);
   }
  }
  void OnDefeated(BasicEnemy enemy){enemy.Died-=OnDefeated;live.Remove(enemy);Defeated++;scene.dungeon.ApplyLan(Total,Defeated);}
  public void Halt(){StopAllCoroutines();foreach(var enemy in live)if(enemy)enemy.SetPaused(true);if(Doors!=null)foreach(var door in Doors)door.CloseWhenClear();}
  void OnDestroy(){foreach(var enemy in live)if(enemy)enemy.Died-=OnDefeated;foreach(var material in materials)if(material)Destroy(material);if(profile)Destroy(profile);}
 }
 public sealed class ArenaSpawnDoor:MonoBehaviour
 {
  Transform leaf;BoxCollider barrier;float progress;bool opening,closing;float closeAt;
  public bool IsOpen=>progress>=.99f;public bool Busy=>opening||closing||progress>.01f;
  public Vector3 SpawnPoint=>transform.position-transform.forward*1.5f;
  public static ArenaSpawnDoor Create(Vector3 point,Vector3 forward,Transform parent)
  {
   var root=new GameObject("Arena spawn gate");root.transform.SetParent(parent);root.transform.SetPositionAndRotation(point,Quaternion.LookRotation(forward));var door=root.AddComponent<ArenaSpawnDoor>();
   var stone=Resources.Load<Material>("CindersStone/Wall");
   void Block(string name,Vector3 pos,Vector3 size){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=pos;go.transform.localScale=size;if(stone)go.GetComponent<Renderer>().sharedMaterial=stone;}
   Block("Gate left pier",new Vector3(-2,1.8f,0),new Vector3(.6f,3.6f,1));Block("Gate right pier",new Vector3(2,1.8f,0),new Vector3(.6f,3.6f,1));Block("Gate lintel",new Vector3(0,3.5f,0),new Vector3(4.6f,.6f,1));
   var leaf=new GameObject("Animated portcullis");leaf.transform.SetParent(root.transform,false);door.leaf=leaf.transform;
   for(int i=-4;i<=4;i++){var bar=GameObject.CreatePrimitive(PrimitiveType.Cube);bar.name="Iron bar";bar.transform.SetParent(leaf.transform,false);bar.transform.localPosition=new Vector3(i*.4f,1.6f,0);bar.transform.localScale=new Vector3(.09f,3.2f,.12f);Destroy(bar.GetComponent<Collider>());if(stone)bar.GetComponent<Renderer>().sharedMaterial=stone;}
   door.barrier=leaf.AddComponent<BoxCollider>();door.barrier.center=Vector3.up*1.6f;door.barrier.size=new Vector3(3.7f,3.2f,.35f);door.barrier.enabled=false;
   var light=root.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.36f,.12f);light.intensity=2;light.range=8;
   return door;
  }
  public void FinishNavigation(){barrier.enabled=true;}
  public void Open(){opening=true;closing=false;closeAt=0;Debug.Log("[ArenaGate] Opening "+transform.position);}
  public void CloseWhenClear(){opening=false;closing=true;closeAt=Time.time+1.8f;}
  void Update(){
   if(opening)progress=Mathf.MoveTowards(progress,1,Time.deltaTime*1.6f);
   if(closing&&Time.time>=closeAt){bool occupied=false;foreach(var c in Physics.OverlapBox(transform.position+Vector3.up, new Vector3(2,1.2f,2),transform.rotation))if(c.GetComponentInParent<BasicEnemy>()||c.GetComponentInParent<PlayerCharacter>()){occupied=true;break;}if(!occupied)progress=Mathf.MoveTowards(progress,0,Time.deltaTime*1.6f);if(progress<=0){closing=false;Debug.Log("[ArenaGate] Closed");}}
   leaf.localPosition=Vector3.up*progress*3.6f;barrier.enabled=progress<.95f;
  }
 }
}
