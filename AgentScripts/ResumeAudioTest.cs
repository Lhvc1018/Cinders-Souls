using UnityEngine;using UnityEditor;
public static class ResumeAudioTest{public static string Run(){bool paused=EditorApplication.isPaused;EditorApplication.isPaused=false;return "Was paused="+paused;}}
