using System;using Codice.Mechanics;using Codice.Presentation;
public static class VerifyEndingReset {
 public static string Restart(){return "Restart="+RunDirector.Instance.RestartRun();}
 public static string Check(){var r=RunDirector.Instance;var a=GameAudio.Instance;if(r.FloorNumber!=1||r.DestinyOrbs!=0||r.LastBellSeal||r.EndingTitle!="")throw new Exception("Ending state leaked across attempts");if(a.CurrentMusic!=a.Library.dungeon)throw new Exception("Exploration music not restored");return "PASS new attempt resets ending requirements and restores exploration music";}
}
