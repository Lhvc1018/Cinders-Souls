using Codice.Characters;
using UnityEngine;
namespace Codice.Expedition
{
    public sealed class MerchantDisplay:MonoBehaviour,IPlayerInteractable
    {
        public Merchant merchant;public int slot;
        public void Interact(PlayerCharacter player){merchant.Interact(player);}
        void Update(){var offer=merchant?merchant.GetOffer(slot):null;if(offer!=null&&offer.sold)gameObject.SetActive(false);}
    }
}
