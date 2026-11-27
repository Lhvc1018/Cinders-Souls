using Codice.Characters;
using UnityEngine;
namespace Codice.Expedition
{
    public sealed class BlessingStatue:MonoBehaviour,IPlayerInteractable
    {
        public StatueBlessing blessing;public int price=100;
        bool purchasing;
        public void Interact(PlayerCharacter player)
        {
            if(purchasing||Codice.Multiplayer.LanSession.Client||!player||!player.State.IsAlive||player.ControlsBlocked||Vector3.Distance(player.transform.position,transform.position)>3.5f||player.State.HasBlessing(blessing)||player.State.Gold<price)return;
            // Reserve the permanent blessing before notifications can repeat the transaction.
            purchasing=true;try{if(player.State.SpendGold(price)){player.State.GrantBlessing(blessing);Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Purchase);}}finally{purchasing=false;}
        }
    }
}
