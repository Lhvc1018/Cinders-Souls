using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// File bridge for the Claude agent: write AgentScripts/_bridge/request.txt
// ("Type.Method" | "play" | "stop" | "refresh" | "errors" | "shot <path>"); reply in response.txt.
[InitializeOnLoad]
public static class AgentBridge
{
    static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../AgentScripts/_bridge"));
    static double next; static System.Threading.Tasks.Task pending; static string pendingName;
    static readonly StringBuilder log = new StringBuilder(), compile = new StringBuilder();
    static AgentBridge()
    {
        Directory.CreateDirectory(Dir);
        EditorApplication.update += Tick;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) lock (log) log.AppendLine($"[{t}] {m}\n{s}"); else if (m.StartsWith("[Agent]")||m.StartsWith("[NativeBoss]")||m.StartsWith("[Verify]")) lock (log) log.AppendLine(m); };
        SafeWrite(Path.Combine(Dir, "alive.txt"), DateTime.Now.ToString("o") + " compiled");
        UnityEditor.Compilation.CompilationPipeline.compilationStarted += _ => { lock (compile) compile.Clear(); };
        UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (asm, msgs) => { lock (compile) foreach (var m in msgs) if (m.type == UnityEditor.Compilation.CompilerMessageType.Error) compile.AppendLine(m.message); SafeWrite(Path.Combine(Dir, "compile.txt"), compile.ToString()); };
    }
    /// The agent's shell reads these files while Unity writes them; a sharing violation must never reach the Console
    /// (an error there can trigger Error Pause and freeze Play mode).
    static void SafeWrite(string path, string text)
    {
        for (int i = 0; i < 3; i++) { try { File.WriteAllText(path, text); return; } catch (IOException) { System.Threading.Thread.Sleep(5); } catch (UnauthorizedAccessException) { return; } }
    }
    static double lastAlive;
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + .5;
        var req = Path.Combine(Dir, "request.txt");
        if (EditorApplication.timeSinceStartup - lastAlive > .5) { lastAlive = EditorApplication.timeSinceStartup; SafeWrite(Path.Combine(Dir, "alive.txt"), DateTime.Now.ToString("o") + (EditorApplication.isPlaying ? " playing" : " edit") + (EditorApplication.isCompiling ? " compiling" : "")); }
        if (pending != null && pending.IsCompleted)
        {
            string r = pending.IsFaulted ? "FAILED " + pending.Exception.InnerException : (pending.GetType().GetProperty("Result")?.GetValue(pending) ?? "done").ToString();
            SafeWrite(Path.Combine(Dir, "response.txt"), DateTime.Now.ToString("o") + " > " + pendingName + "\n" + r); pending = null;
        }
        if (!File.Exists(req)) return;
        string cmd; try { cmd = File.ReadAllText(req).Trim(); File.Delete(req); } catch (IOException) { return; }
        string result;
        try { result = Run(cmd); } catch (Exception e) { result = "EXCEPTION " + (e.InnerException ?? e); }
        SafeWrite(Path.Combine(Dir, "response.txt"), DateTime.Now.ToString("o") + " > " + cmd + "\n" + result);
    }
    static string Run(string cmd)
    {
        if (cmd == "refresh") { AssetDatabase.Refresh(); return "refreshed"; }
        if (cmd == "play") { lock (log) log.Clear(); EditorApplication.isPlaying = true; return "entering play"; }
        if (cmd == "stop") { EditorApplication.isPlaying = false; return "stopping"; }
        if (cmd == "log") { lock (log) return log.ToString(); }
        if (cmd == "clearlog") { lock (log) log.Clear(); return "cleared"; }
        if (cmd.StartsWith("shot ")) { ScreenCapture.CaptureScreenshot(cmd.Substring(5)); return "captured " + cmd.Substring(5); }
        var dot = cmd.LastIndexOf('.');
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(cmd.Substring(0, dot))).FirstOrDefault(t => t != null);
        if (type == null) return "type not found";
        var m = type.GetMethod(cmd.Substring(dot + 1), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (m == null) return "method not found";
        var value = m.Invoke(null, null);
        if (value is System.Threading.Tasks.Task task) { pending = task; pendingName = cmd; return "started async"; }
        return (value ?? "ok").ToString();
    }
}
