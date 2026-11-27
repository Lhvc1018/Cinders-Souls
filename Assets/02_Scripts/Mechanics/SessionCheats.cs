using UnityEngine;
namespace Codice.Mechanics{
 public static class SessionCheats{
  public static bool God{get;private set;} public static bool Rich{get;private set;}
  public static string Status=>"Good!: "+(God?"ACTIVO":"inactivo")+" · Macpato: "+(Rich?"ACTIVO":"inactivo");
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){God=Rich=false;}
  public static bool Execute(string code){
   code=(code??"").Trim();
   if(string.Equals(code,"Good!",System.StringComparison.OrdinalIgnoreCase)){God=true;if(God&&RunDirector.Instance&&RunDirector.Instance.Player)RunDirector.Instance.Player.State.NetworkRevive();}
   else if(string.Equals(code,"Good! off",System.StringComparison.OrdinalIgnoreCase)){God=false;}
   else if(code=="Macpato")Rich=!Rich;else return false;
   if(RunDirector.Instance&&RunDirector.Instance.Player)RunDirector.Instance.Player.State.NotifyChanged();Debug.Log("[Cheats] "+Status);return true;
  }
 }
}
