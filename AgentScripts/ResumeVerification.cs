using UnityEditor;using UnityEngine;public static class ResumeVerification{public static string Run(){EditorApplication.isPaused=false;Time.timeScale=1;return "resumed";}}
