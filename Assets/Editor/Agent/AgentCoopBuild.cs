using System.Diagnostics;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
public static class AgentCoopBuild{
 static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
 static string Exe=>Path.Combine(Root,"Builds","CoopTest","CindersCoopTest.exe");
 public static string Build(){var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
  var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Exe,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  return "build "+r.summary.result+" errors="+r.summary.totalErrors+" size="+(r.summary.totalSize/1048576)+"MB time="+r.summary.totalTime.TotalSeconds.ToString("F0")+"s";}
 public static int GuestClass=4; public static bool ProbeLan,ProbeFloors;
 public static string LaunchFloors(){ProbeFloors=true;try{return Launch();}finally{ProbeFloors=false;}}
 public static string LaunchLan(){ProbeLan=true;try{return Launch();}finally{ProbeLan=false;}}   /* 4 = Shinobi */
 public static string LaunchOptions(){if(!File.Exists(Exe))return "no build";string dir=Path.Combine(Root,"AgentScripts","_bridge");Process.Start(new ProcessStartInfo(Exe,$"-screen-fullscreen 0 -screen-width 1600 -screen-height 900 -options-shot \"{dir}\""){UseShellExecute=false});return "launched options";}
 public static string Launch(){if(!File.Exists(Exe))return "no build";string dir=Path.Combine(Root,"AgentScripts","_bridge");string room="PROBE-"+System.Guid.NewGuid().ToString("N").Substring(0,6).ToUpperInvariant();
  foreach(var role in new[]{"host","client"}){var report=Path.Combine(dir,"coop_"+role+".txt");File.WriteAllText(report,"");
   Process.Start(new ProcessStartInfo(Exe,$"-screen-fullscreen 0 -screen-width 960 -screen-height 540 -coop-probe {role} {room} \"{report}\" -guest-class {GuestClass}{(ProbeLan?" -probe-lan":"")}{(ProbeFloors?" -probe-floors":"")}"){UseShellExecute=false});}
  return "launched host+client room="+room;}
 public static string Release(){var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();var exe=Path.Combine(Root,"Builds","CindersSouls_Online","CindersSouls.exe");
  var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=exe,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  return "release "+r.summary.result+" errors="+r.summary.totalErrors+" size="+(r.summary.totalSize/1048576)+"MB time="+r.summary.totalTime.TotalSeconds.ToString("F0")+"s -> "+exe;}
}
