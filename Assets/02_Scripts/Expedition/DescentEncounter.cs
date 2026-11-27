using UnityEngine;
using Codice.Characters;
using Codice.Mechanics;
namespace Codice.Expedition
{
    public enum DescentOffer { Pilgrim,Wisp,Rest }
    public sealed class DescentEncounter:MonoBehaviour,IPlayerInteractable
    {
        public DescentOffer offer;public bool Used{get;private set;}
        public int Price=>offer==DescentOffer.Pilgrim?180:offer==DescentOffer.Wisp?100:0;
        public string Description=>Used?"PACTO CUMPLIDO":offer==DescentOffer.Rest?"BRASERO DEL ERRANTE\n[E] Recuperar 35% de vida y 30 maná · Un uso":
            (offer==DescentOffer.Pilgrim?"IREL · PEREGRINO\nLanza dardos al enemigo durante el combate.":"LUMBRE · MASCOTA DE CENIZA\nRestaura 4 maná cada 6 segundos en combate.")+"\n[E] Contratar · "+Price+" oro · Hasta terminar el intento\nMáximo un ayudante y una mascota.";
        public void Interact(PlayerCharacter player)
        {
            var run=RunDirector.Instance;
            if(Used||!run||!player||!player.State.IsAlive||player.ControlsBlocked||Vector3.Distance(player.transform.position,transform.position)>3.5f||Codice.Multiplayer.LanSession.Client)return;
            if(offer==DescentOffer.Rest){if(player.State.Health>=player.State.MaxHealth&&player.State.Mana>=player.State.MaxMana)return;Used=true;player.State.Heal(player.State.MaxHealth*.35f);player.State.RestoreMana(30);}
            else {
                bool wisp=offer==DescentOffer.Wisp;
                if(wisp?player.State.BondedWisp:player.State.HiredPilgrim){Codice.UI.RunHUD.Instance?.Announce("Ya tienes ese compañero en este descenso.");return;}
                if(!player.State.SpendGold(Price)){Codice.UI.RunHUD.Instance?.Announce("Necesitas "+Price+" oro para este pacto.");return;}
                Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Purchase);Used=true;if(wisp)player.State.BondedWisp=true;else player.State.HiredPilgrim=true;
                DescentCompanion.Create(player,wisp);
            }
            Codice.UI.RunHUD.Instance?.Announce(offer==DescentOffer.Rest?"El brasero se apaga. Recuperas fuerzas.":"Pacto sellado · El compañero viajará contigo.");
            foreach(var c in GetComponents<Collider>())c.enabled=false;
            if(offer!=DescentOffer.Rest)gameObject.SetActive(false);
        }
        public static DescentEncounter Create(Vector3 p,Transform parent,DescentOffer offer)
        {
            var go=new GameObject(offer.ToString()+" · Encuentro");go.transform.SetParent(parent);go.transform.position=p;
            var e=go.AddComponent<DescentEncounter>();e.offer=offer;
            var collider=go.AddComponent<CapsuleCollider>();collider.radius=.6f;collider.height=2;collider.center=Vector3.up;collider.isTrigger=true;
            if(offer==DescentOffer.Pilgrim)DescentCompanion.PilgrimVisual(go.transform);
            else DescentCompanion.FlameVisual(go.transform,offer==DescentOffer.Wisp?new Color(.3f,.85f,1):new Color(1,.55f,.15f));
            return e;
        }
    }
}
