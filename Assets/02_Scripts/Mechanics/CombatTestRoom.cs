using System.Collections.Generic;using System.Linq;using UnityEngine;using UnityEngine.AI;using Unity.AI.Navigation;using Codice.Characters;using Codice.Enemies;using Codice.Bosses;using Codice.Presentation;
namespace Codice.Mechanics{
 public sealed class CombatTestRoom:MonoBehaviour{
  [Tooltip("Drag enemy or boss prefabs here. Originals remain unchanged.")]public GameObject[] enemyPrefabs;
  public Transform[] spawnPoints;
  [Range(1,25)]public int enemyLevel=1;
  public bool pauseEnemies;
  RunScene scene;readonly List<GameObject> spawned=new List<GameObject>();[Min(0)]public int selected; [Range(1,12)]public int spawnCount=1;bool panel=true;
  // Every enemy and boss prefab under Resources/ExpeditionEnemies is offered, so new content (El Señor de los Terremotos, residents) needs no scene edit.
  void IncludeResourceEnemies(){var list=new List<GameObject>(enemyPrefabs??new GameObject[0]);foreach(var prefab in Resources.LoadAll<GameObject>("ExpeditionEnemies").Concat(Resources.LoadAll<GameObject>("WorldFour")))if(prefab&&!list.Contains(prefab)&&(prefab.GetComponent<BasicEnemy>()||prefab.GetComponent<BossCharacter>()))list.Add(prefab);enemyPrefabs=list.ToArray();}
  public void Begin(RunScene context){IncludeResourceEnemies();scene=context;scene.player.State.LaboratoryImmortal=true;scene.player.State.NetworkRevive();scene.player.SetNarrativeControlsBlocked(true);Debug.Log("[TestRoom] Immortality active; normal outgoing damage preserved");Cursor.lockState=CursorLockMode.None;Cursor.visible=true;var surface=GetComponent<NavMeshSurface>()??gameObject.AddComponent<NavMeshSurface>();surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();if(scene.portal)scene.portal.SetAvailable(false);foreach(var actor in FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))if(actor.gameObject.scene==gameObject.scene){actor.Initialize(scene.player,enemyLevel);actor.SetPaused(pauseEnemies);spawned.Add(actor.gameObject);}foreach(var actor in FindObjectsByType<BossCharacter>(FindObjectsSortMode.None))if(actor.gameObject.scene==gameObject.scene){actor.Initialize(scene.player,enemyLevel);actor.SetPaused(pauseEnemies);spawned.Add(actor.gameObject);}Debug.Log("[TestRoom] Ready: F2 controls, editable spawn points and prefab list");}
  void Update(){if(!scene||!scene.player)return;if(previewUntil>0&&Time.unscaledTime>=previewUntil)EndAnimationPreview();if(UnityEngine.InputSystem.Keyboard.current?.f2Key.wasPressedThisFrame==true){if(previewUntil>0)EndAnimationPreview();else SetPanel(!panel);}}
  public bool SpawnSelected(){if(!scene||enemyPrefabs==null||enemyPrefabs.Length==0 )return false;selected=Mathf.Clamp(selected,0,enemyPrefabs.Length-1);if(!enemyPrefabs[selected])return false;var point=spawnPoints!=null&&spawnPoints.Length>0?(spawnPoints[spawned.Count%spawnPoints.Length]?spawnPoints[spawned.Count%spawnPoints.Length].position:transform.position+Vector3.forward*8):transform.position+Vector3.forward*8;NavMeshHit hit=default;bool found=false;for(int attempt=0;attempt<16;attempt++){var offset=attempt==0?Vector3.zero:Quaternion.Euler(0,attempt*137.5f,0)*Vector3.forward*(2+attempt*.3f);if(NavMesh.SamplePosition(point+offset,out hit,2,NavMesh.AllAreas)&&!Physics.CheckCapsule(hit.position+Vector3.up*.85f,hit.position+Vector3.up*1.8f,.7f,~0,QueryTriggerInteraction.Ignore)){found=true;break;}}if(!found)return false;var go=Instantiate(enemyPrefabs[selected],hit.position,Quaternion.identity);spawned.Add(go);var enemy=go.GetComponent<BasicEnemy>();var boss=go.GetComponent<BossCharacter>();if(!enemy&&!boss){spawned.Remove(go);Destroy(go);return false;}if(enemy){enemy.Initialize(scene.player,enemyLevel);enemy.SetPaused(pauseEnemies);}if(boss){boss.Initialize(scene.player,enemyLevel);boss.SetPaused(pauseEnemies);}return enemy||boss;}
  public void ClearEnemies(){foreach(var go in spawned)if(go)Destroy(go);spawned.Clear();}
  static readonly string[] ClassLabels={"Guerrero","Tanque","Arquero","Mago","Shinobi"};
  float previewUntil;int animationChoice;bool slowPreview;
  public bool PreviewAnimation(int index){
   if(!scene||!scene.player)return false;var presentation=scene.player.GetComponent<ActorPresentation>();var body=presentation?.Body;if(!body)return false;
   var clips=body.runtimeAnimatorController.animationClips.Where(c=>(c.name.StartsWith("AS_")||c.name.StartsWith("Thymesia_"))).Distinct().OrderBy(c=>c.name).ToArray();if(index<0||index>=clips.Length)return false;
   scene.player.GetComponent<PlayerCombat>()?.TryCancelForDodge();scene.player.GetComponent<ShinobiGuard>()?.SetGuard(false);
   SetPanel(false);scene.player.SetNarrativeControlsBlocked(true);presentation.LaboratoryPreviewActive=true;
   string state=(clips[index].name.StartsWith("Thymesia_")?"Preview_":"LordsPreview_")+clips[index].name;presentation.Play(state,0,true);body.speed=1;
   body.SetFloat("ActionSpeed",slowPreview?.5f:1);body.SetFloat("LocomotionRate",slowPreview?.5f:1);
   previewUntil=Time.unscaledTime+clips[index].length/(slowPreview?.5f:1)+.15f;
   var camera=scene.player.GetComponent<HumanPreviewCamera>();if(camera&&!camera.ThirdPerson)camera.Toggle();
   Debug.Log("[TestRoom] Animation preview: "+scene.player.State.Class+" / "+clips[index].name);return true;
  }
  void EndAnimationPreview(){previewUntil=0;if(!scene||!scene.player)return;var presentation=scene.player.GetComponent<ActorPresentation>();if(presentation){presentation.LaboratoryPreviewActive=false;presentation.Play("Ready",0,true);}SetPanel(true);}
  public bool ChangeClass(PlayerClass selected){if(!scene||!scene.player)return false;if(previewUntil>0)EndAnimationPreview();bool changed=scene.player.SelectLaboratoryClass(selected);if(changed){scene.player.SetNarrativeControlsBlocked(panel);Cursor.lockState=panel?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=panel;}return changed;}
  Vector2 listScroll,toolsScroll;string filter="";GUIStyle labelStyle,buttonStyle,titleStyle;
  void OnDisable(){if(scene&&scene.player){var presentation=scene.player.GetComponent<ActorPresentation>();if(presentation)presentation.LaboratoryPreviewActive=false;scene.player.State.LaboratoryImmortal=false;scene.player.SetNarrativeControlsBlocked(false);}}
  void SetPanel(bool value){panel=value;scene.player.SetNarrativeControlsBlocked(value);Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}
  void OnGUI(){
   if(!scene||!panel)return;
   if(labelStyle==null){labelStyle=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};buttonStyle=new GUIStyle(GUI.skin.button){fontSize=19,wordWrap=true,padding=new RectOffset(12,12,9,9)};titleStyle=new GUIStyle(labelStyle){fontSize=25,fontStyle=FontStyle.Bold};}
   var previous=GUI.matrix;float scale=Mathf.Min(Screen.width/1000f,Screen.height/650f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
   var previousColor=GUI.color;GUI.color=new Color(.045f,.055f,.07f,1);GUI.DrawTexture(new Rect(20,20,960,610),Texture2D.whiteTexture);GUI.color=previousColor;
   GUILayout.BeginArea(new Rect(20,20,960,610),GUI.skin.box);
   GUILayout.Label("LABORATORIO DE COMBATE",titleStyle);GUILayout.Label("INMORTAL · Vida mínima: 1 · Daño normal · F2 para jugar",labelStyle);
   GUILayout.BeginHorizontal();GUILayout.BeginVertical(GUILayout.Width(480));
   GUILayout.Label("ENEMIGOS Y JEFES · Buscar",titleStyle);filter=GUILayout.TextField(filter,new GUIStyle(GUI.skin.textField){fontSize=20},GUILayout.Height(36));
   listScroll=GUILayout.BeginScrollView(listScroll,GUILayout.Height(400));
   if(enemyPrefabs!=null)for(int i=0;i<enemyPrefabs.Length;i++){var prefab=enemyPrefabs[i];if(!prefab||(!string.IsNullOrWhiteSpace(filter)&&prefab.name.IndexOf(filter,System.StringComparison.OrdinalIgnoreCase)<0))continue;var color=GUI.backgroundColor;GUI.backgroundColor=i==selected?new Color(.9f,.68f,.24f):Color.white;string kind=prefab.GetComponent<BossCharacter>()?"JEFE":"ENEMIGO";if(GUILayout.Button((i==selected?"► ":"")+kind+" · "+prefab.name,buttonStyle,GUILayout.MinHeight(52)))selected=i;GUI.backgroundColor=color;}
   GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.Space(18);GUILayout.BeginVertical();toolsScroll=GUILayout.BeginScrollView(toolsScroll);
   GUILayout.Label("CLASE DEL JUGADOR",titleStyle);
   int current=(int)scene.player.State.Class;
   int choice=GUILayout.SelectionGrid(current,ClassLabels,2,buttonStyle,GUILayout.Height(126));
   if(choice!=current)ChangeClass((PlayerClass)choice);
   GUILayout.Label("Cambiar clase restaura equipo inicial y recursos. Conserva enemigos e inmortalidad.",labelStyle);
   var body=scene.player.GetComponent<ActorPresentation>()?.Body;
   if(body&&body.runtimeAnimatorController){var clips=body.runtimeAnimatorController.animationClips.Where(c=>(c.name.StartsWith("AS_")||c.name.StartsWith("Thymesia_"))).Distinct().OrderBy(c=>c.name).ToArray();if(clips.Length>0){
    animationChoice=Mathf.Clamp(animationChoice,0,clips.Length-1);GUILayout.Space(8);GUILayout.Label("ANIMACIONES DE CLASE · "+(animationChoice+1)+" / "+clips.Length,titleStyle);
    GUILayout.BeginHorizontal();if(GUILayout.Button("◀",buttonStyle,GUILayout.Width(52)))animationChoice=(animationChoice+clips.Length-1)%clips.Length;GUILayout.Label(clips[animationChoice].name.Replace("AS_","").Replace("_"," "),labelStyle);if(GUILayout.Button("▶",buttonStyle,GUILayout.Width(52)))animationChoice=(animationChoice+1)%clips.Length;GUILayout.EndHorizontal();
    slowPreview=GUILayout.Toggle(slowPreview,"Cámara lenta · 0,5x",buttonStyle);if(GUILayout.Button("REPRODUCIR · F2 para cancelar",buttonStyle))PreviewAnimation(animationChoice);
   }}
   GUILayout.Space(12);GUILayout.Label("PRUEBA SELECCIONADA",titleStyle);
   GUILayout.Label("Cantidad: "+spawnCount+"    Nivel: "+enemyLevel,labelStyle);
   GUILayout.BeginHorizontal();if(GUILayout.Button("−",buttonStyle))spawnCount=Mathf.Max(1,spawnCount-1);if(GUILayout.Button("+",buttonStyle))spawnCount=Mathf.Min(12,spawnCount+1);if(GUILayout.Button("Nivel −",buttonStyle))enemyLevel=Mathf.Max(1,enemyLevel-1);if(GUILayout.Button("Nivel +",buttonStyle))enemyLevel=Mathf.Min(25,enemyLevel+1);GUILayout.EndHorizontal();
   if(GUILayout.Button("CREAR SELECCIÓN × "+spawnCount,buttonStyle,GUILayout.Height(55)))for(int i=0;i<spawnCount;i++)SpawnSelected();
   if(GUILayout.Button("Limpiar enemigos",buttonStyle))ClearEnemies();
   if(GUILayout.Button(pauseEnemies?"Reanudar IA":"Pausar IA",buttonStyle)){pauseEnemies=!pauseEnemies;foreach(var go in spawned)if(go){go.GetComponent<BasicEnemy>()?.SetPaused(pauseEnemies);go.GetComponent<BossCharacter>()?.SetPaused(pauseEnemies);}}
   if(GUILayout.Button("Restaurar vida / maná / armadura",buttonStyle))scene.player.State.NetworkRevive();
   var bench=FindAnyObjectByType<ProceduralContentWorkbench>();if(bench){if(GUILayout.Button("Generar decoración · "+bench.seed,buttonStyle))bench.GeneratePreview();if(GUILayout.Button("Reconstruir navegación",buttonStyle))bench.RebuildNavigation();}
   foreach(var go in spawned)if(go&&go.TryGetComponent<BossCharacter>(out var boss))GUILayout.Label(go.name+" · "+Mathf.CeilToInt(boss.Health)+" / "+boss.MaxHealth,labelStyle);
   GUILayout.EndScrollView();if(GUILayout.Button("VOLVER A PROBAR · F2",buttonStyle,GUILayout.Height(48)))SetPanel(false);GUILayout.EndVertical();GUILayout.EndHorizontal();GUILayout.EndArea();GUI.matrix=previous;
  }
  void OnDrawGizmos(){if(spawnPoints==null)return;Gizmos.color=Color.cyan;foreach(var point in spawnPoints)if(point){Gizmos.DrawWireSphere(point.position,1);Gizmos.DrawLine(point.position,point.position+Vector3.up*2);}}
 }
}

