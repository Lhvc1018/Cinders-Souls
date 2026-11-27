using System;using System.IO;using System.Linq;using System.Reflection;using System.Text;using System.Threading.Tasks;using UnityEngine;
using Codice.Characters;using Codice.Expedition;using Codice.Mechanics;using Codice.UI;using Codice.Multiplayer;
public static class AgentJamCoop {
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../MapReview/agent/"));
 public static async Task<string> Jam(){var sb=new StringBuilder();MainMenuUI.UnlockCheats("jam");ExpeditionFloor.DebugSeed=1000;RunNavigation.StartExpeditionTest(PlayerClass.Warrior,1);await Task.Delay(9000);
  var run=RunDirector.Instance;var f=run.Expedition;var p=run.Player;p.State.LaboratoryImmortal=true;int target=Mathf.Min(3,f.Layout.Boss-1);
  sb.AppendLine($"floor={f.FloorNumber} rooms={f.RoomCount} enabled={JamDebug.Enabled} free={JamDebug.FreeRoam}");
  bool tp=JamDebug.Teleport(target);await Task.Delay(2500);int seals=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Room seal")&&t.gameObject.activeInHierarchy);
  sb.AppendLine($"teleport room {target+1}: ok={tp} inEncounter={f.InEncounter} current={f.CurrentRoom+1} previousCleared={f.Cleared[Mathf.Max(1,target-1)]} activeSeals={seals}");
  bool ch=JamDebug.ChangeClass(PlayerClass.Shinobi);await Task.Delay(700);sb.AppendLine($"class change ok={ch} now={p.State.Class} katana={p.State.ActiveWeapon?.katana} alive={p.State.IsAlive}");
  var jam=UnityEngine.Object.FindAnyObjectByType<JamDebug>();typeof(JamDebug).GetMethod("Show",F).Invoke(jam,new object[]{true});await Task.Delay(500);ScreenCapture.CaptureScreenshot(Dir+"jam_panel.png");await Task.Delay(500);typeof(JamDebug).GetMethod("Show",F).Invoke(jam,new object[]{false});
  bool boss=JamDebug.Teleport(f.Layout.Boss);await Task.Delay(3000);sb.AppendLine($"boss room direct: ok={boss} boss={(f.context.arena.ActiveBoss?f.context.arena.ActiveBoss.name:"none")}");
  return sb.ToString();}
 public static async Task<string> Lobby(){foreach(var room in UnityEngine.Object.FindObjectsByType<CombatTestRoom>(FindObjectsSortMode.None))room.enabled=false;LanSession.Open();await Task.Delay(1200);ScreenCapture.CaptureScreenshot(Dir+"coop_menu.png");await Task.Delay(600);var s=LanSession.Instance;var sb=new StringBuilder();
  typeof(LanSession).GetField("useFusion",F).SetValue(s,true);s.CreateHost();string status="";for(int i=0;i<40;i++){await Task.Delay(500);status=(string)typeof(LanSession).GetField("status",F).GetValue(s);if(LanSession.Host||(!(bool)typeof(LanSession).GetField("connecting",F).GetValue(s)&&i>2))break;}
  var fusion=typeof(LanSession).GetField("fusion",F).GetValue(s) as FusionCoopTransport;
  sb.AppendLine($"host={LanSession.Host} status='{status}' runner={(fusion&&fusion.Runner?fusion.Runner.IsRunning.ToString():"none")} session={(fusion&&fusion.Runner&&fusion.Runner.SessionInfo!=null?fusion.Runner.SessionInfo.Name+" region="+fusion.Runner.SessionInfo.Region:"-")} party={CoopScaling.Party} summary={CoopScaling.Summary}");
  await Task.Delay(500);ScreenCapture.CaptureScreenshot(Dir+"coop_lobby.png");await Task.Delay(700);s.Leave();return sb.ToString();}
}
