using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Codice.UI;
public static class CindersAuthoringMenus{
 [MenuItem("Cinders Souls/Edición/Abrir menú editable")]
 public static void Menu(){if(EditorApplication.isPlaying)return;if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");var menu=Object.FindAnyObjectByType<MainMenuUI>();if(menu&&!menu.transform.Find("MENU EDITABLE"))menu.GenerateEditableMenu();Selection.activeGameObject=menu?menu.gameObject:null;}
 [MenuItem("Cinders Souls/Edición/Abrir sala de pruebas")]
 public static void Lab(){if(EditorApplication.isPlaying)return;if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/SalaPruebas.unity");Selection.activeGameObject=Object.FindAnyObjectByType<Codice.Mechanics.CombatTestRoom>()?.gameObject;}
}
