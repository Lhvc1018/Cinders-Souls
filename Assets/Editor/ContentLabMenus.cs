using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
public static class ContentLabMenus {
 [MenuItem("Cinders Souls/Edición/Abrir laboratorio de contenido")]
 public static void Open(){if(EditorApplication.isPlaying)return;if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/LaboratorioContenido.unity");Selection.activeGameObject=Object.FindAnyObjectByType<Codice.Mechanics.ProceduralContentWorkbench>()?.gameObject;}
 [MenuItem("Cinders Souls/Edición/Abrir catálogo procedural")]
 public static void Catalog(){Selection.activeObject=AssetDatabase.LoadAssetAtPath<Object>("Assets/Resources/CindersDungeon/ContentCatalog.asset");}
 [MenuItem("Cinders Souls/Edición/Guardar selección como prefab")]
 public static void Export(){if(!Selection.activeGameObject)return;var path=EditorUtility.SaveFilePanelInProject("Guardar contenido",Selection.activeGameObject.name,"prefab","Elige carpeta para el nuevo prefab");if(!string.IsNullOrEmpty(path))PrefabUtility.SaveAsPrefabAsset(Selection.activeGameObject,path);}
}
