using Codice.Characters;
using Codice.Presentation;
using UnityEngine;
namespace Codice.Mechanics
{
    public sealed class EstusShrine:MonoBehaviour,IPlayerInteractable
    {
        public bool Used {get;private set;}
        public void Interact(PlayerCharacter player)
        {
            if(Used||!player||player.ControlsBlocked||Time.timeScale<=0||Vector3.Distance(transform.position,player.transform.position)>3.5f)return;
            if(!player.State.RefillEstus()){Codice.UI.RunHUD.Instance?.Announce("El frasco ya está lleno");return;}
            Used=true;Codice.UI.RunHUD.Instance?.Announce("Frasco de ceniza · 3 cargas restauradas");GameAudio.Play(SoundCue.Pickup);
            Debug.Log("[Estus] Shrine spent; charges="+player.State.EstusCharges);
        }
    }
}
