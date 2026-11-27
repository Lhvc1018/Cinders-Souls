using System.Linq;using UnityEditor;using UnityEngine;
public static class PrepareExpansion{
 public static string Run(){
 foreach(var w in Resources.FindObjectsOfTypeAll<EditorWindow>())if(w.GetType().Name=="SetupMessageWindow"||w.GetType().Name=="TMP_PackageResourceImporterWindow")w.Close();
 EditorApplication.isPaused=false;Application.runInBackground=true;
 return "Closed package notice windows; editor unpaused";
 }
}
