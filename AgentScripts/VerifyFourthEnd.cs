using System;using UnityEngine;using Codice.Mechanics;using Codice.Expedition;
public static class VerifyFourthEnd {
 public static string Victory(){var r=RunDirector.Instance;if(r.FloorNumber!=12||r.Phase!=RunPhase.Victory||!r.Expedition.Finished)throw new Exception("Final not completed");return "PASS 4-3 final victory";}
 public static string AdvanceLore(){var e=UnityEngine.Object.FindAnyObjectByType<DestinyEncounter>();if(!e)throw new Exception("Missing lore encounter");for(int i=0;i<5;i++)e.Advance();return "Advanced third orb dialogue";}
 public static string Transition(){var r=RunDirector.Instance;if(!r||r.FloorNumber!=10||!r.Expedition)throw new Exception("Transition to 4-1 failed");return "PASS 3-3 orb dialogue transitions into 4-1";}
}
