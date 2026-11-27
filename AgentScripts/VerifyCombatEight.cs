using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Codice.Characters;
using Codice.Expedition;
using Codice.Bosses;
public static class VerifyCombatEight {
 public static string AssetsAndLayouts(){
  int giants=0;for(int seed=0;seed<256;seed++)for(int floor=1;floor<=6;floor++){
   var layout=new ExpeditionLayout(seed,floor);if(!layout.Connected()||layout.Count<7||layout.Count>10)throw new Exception("Layout "+seed+"/"+floor);
   bool giant=ExpeditionFloor.BossResource(seed,floor).EndsWith("Terremotos");if(giant){giants++;if(layout.Width(layout.Boss)<10||layout.Radius(layout.Boss)<10)throw new Exception("Small giant arena");}
   if(floor==3&&!ExpeditionFloor.BossResource(seed,floor).EndsWith("AngelKnight"))throw new Exception("Angel selection");
  }
  if(giants==0)throw new Exception("No giant seeds");int deaths=0;
  foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/ExpeditionEnemies"})){
   var path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var a=prefab.GetComponentInChildren<Animator>(true);if(!a||!a.runtimeAnimatorController)continue;
   if(!AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("Muck")&&prefab.name!="El Señor de los Terremotos")continue;
   var c=(AnimatorController)a.runtimeAnimatorController;var state=c.layers[0].stateMachine.states.First(x=>x.state.name=="Death").state;var clip=state.motion as AnimationClip;
   if(!clip||clip.length<1||state.speed<=0||AnimationUtility.GetCurveBindings(clip).Length<2)throw new Exception("Missing death "+path);deaths++;
  }
  var sword=Resources.Load<GameObject>("KnightStartingSword");if(!sword)throw new Exception("Sword missing");
  var b=Resources.Load<GameObject>("ExpeditionEnemies/El Señor de los Terremotos");if(!b||b.GetComponent<CapsuleCollider>().height!=12||b.GetComponent<BossCharacter>().kind!=BossKind.BigChunk)throw new Exception("Chunk invalid");
  var tank=new CharacterState();tank.Initialize(PlayerClass.Tank);var assassin=new CharacterState();assassin.Initialize(PlayerClass.Berserker);if(tank.MaxHealth<=assassin.MaxHealth||tank.MoveSpeed>=assassin.MoveSpeed)throw new Exception("Class identity stats");
  return "PASS 1536 layouts; giant seeds="+giants+"; animated Muck deaths="+deaths+"; Terremotos ~11.5m; class stats; sword.";
 }
 public static string SpawnBoss(){var run=Codice.Mechanics.RunDirector.Instance;var f=run.Expedition;for(int i=0;i<f.Layout.Boss;i++)f.Cleared[i]=true;var cc=run.Player.GetComponent<CharacterController>();cc.enabled=false;run.Player.transform.position=f.Center(f.Layout.Boss);cc.enabled=true;run.Player.State.LaboratoryImmortal=true;return "Moved into boss arena";}
 public static string InspectBoss(){var f=Codice.Mechanics.RunDirector.Instance.Expedition;var b=f.context.arena.ActiveBoss;if(!b)throw new Exception("No boss in room "+f.CurrentRoom);var skins=b.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length==0||skins.All(r=>!r.enabled))throw new Exception("Invisible boss");var path=new UnityEngine.AI.NavMeshPath();if(!UnityEngine.AI.NavMesh.CalculatePath(b.transform.position,f.context.player.transform.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new Exception("Boss unreachable");return "PASS "+b.name+" skins="+skins.Length+" path complete; pos="+b.transform.position;}
}
