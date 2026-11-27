using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class AgentCoopEditorPair
{
    const string Saved="Cinders.CoopProbe.StartScene";
    [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=Restore;}
    static void Restore(PlayModeStateChange state){
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Saved+"Set",false))return;
        var path=SessionState.GetString(Saved,"");
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        SessionState.SetBool(Saved+"Set",false);
    }
    public static string Start(){
        if(EditorApplication.isPlaying)return "Already playing";
        var room="PROBE-"+System.Guid.NewGuid().ToString("N").Substring(0,8).ToUpperInvariant();
        File.WriteAllText("AgentScripts/_bridge/coop8-room.txt",room);
        SessionState.SetString("Cinders.CoopProbe.Room",room);
        SessionState.SetString(Saved,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Saved+"Set",true);
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/00_Scenes/MainMenu.unity");
        EditorApplication.isPlaying=true;return room;
    }
}
