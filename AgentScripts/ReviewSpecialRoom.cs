using UnityEngine;
using Codice.Expedition;
using Codice.Mechanics;
public static class ReviewSpecialRoom
{
    public static string Merchant()
    {
        var floor=Object.FindAnyObjectByType<ExpeditionFloor>();var p=floor.context.player;int room=floor.Layout.MerchantRoom;if(room<0)return "No merchant this seed";
        RunDirector.Instance.SetExpeditionPhase(RunPhase.Cleared,"");p.enabled=false;p.State.Initialize(Codice.Characters.PlayerClass.Warrior);
        var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=floor.Center(room)+new Vector3(0,.15f,-4);cc.enabled=true;
        p.view.transform.LookAt(floor.Center(room)+new Vector3(0,1.4f,2));Time.timeScale=0;return "Merchant room staged";
    }
}
