using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Codice.Mechanics;using Codice.UI;
public static class BuildAuthoring{
 public static string Run(){
  EditorSceneManager.SaveOpenScenes();
  var menuScene=EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");var menu=Object.FindAnyObjectByType<MainMenuUI>();menu.GenerateEditableMenu();EditorSceneManager.SaveScene(menuScene);
  var scene=EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/Coliseo.unity");EditorSceneManager.SaveScene(scene,"Assets/00_Scenes/Maps/SalaPruebas.unity");
  var run=Object.FindAnyObjectByType<RunScene>();var lab=run.gameObject.AddComponent<CombatTestRoom>();
  lab.enemyPrefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/ExpeditionEnemies"}).Select(g=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).Where(p=>p.GetComponent<Codice.Enemies.BasicEnemy>()||p.GetComponent<Codice.Bosses.BossCharacter>()).ToArray();
  if(lab.enemyPrefabs.Length==0)lab.enemyPrefabs=run.dungeon.prefabs.Select(p=>p.gameObject).ToArray();
  var points=new GameObject("PUNTOS DE PRUEBA · mover libremente");lab.spawnPoints=new Transform[3];for(int i=0;i<3;i++){var point=new GameObject("Enemigo "+(i+1));point.transform.SetParent(points.transform);point.transform.position=new Vector3((i-1)*5,0,8);lab.spawnPoints[i]=point.transform;}
  var notes=new GameObject("F2 · abrir controles de prueba / Inspector · Combat Test Room");
  EditorSceneManager.SaveScene(scene);var builds=EditorBuildSettings.scenes.ToList();if(!builds.Any(x=>x.path==scene.path)){builds.Add(new EditorBuildSettingsScene(scene.path,true));EditorBuildSettings.scenes=builds.ToArray();}
  int count=lab.enemyPrefabs.Length;AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");Selection.activeGameObject=Object.FindAnyObjectByType<MainMenuUI>().gameObject;
  return "Editable MainMenu and SalaPruebas saved; enemy prefabs="+count;
 }
}

