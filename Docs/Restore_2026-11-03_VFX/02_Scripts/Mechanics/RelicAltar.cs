using System.Collections.Generic;
using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    public struct RelicOffer
    {
        public bool active;public int kind;
        public string Name=>active?RelicCatalog.Name((ActiveRelic)kind):Codice.UI.UIWidgets.Passive((PassiveKind)kind);
        public string Description=>active?RelicCatalog.Description((ActiveRelic)kind):RelicCatalog.PassiveDescription((PassiveKind)kind);
    }
    public sealed class RelicAltar:MonoBehaviour,IPlayerInteractable
    {
        public bool Claimed {get;private set;}
        public RelicOffer[] Offers {get;private set;}
        int seed;bool activeReward;
        public int Seed=>seed;public bool ActiveReward=>activeReward;
        public static RelicAltar Create(Vector3 position,int seed,bool activeReward)
        {
            var altar=new GameObject("Relicario de elección").AddComponent<RelicAltar>();altar.transform.position=position;altar.seed=seed;altar.activeReward=activeReward;
            var col=altar.gameObject.AddComponent<SphereCollider>();col.radius=.7f;col.center=Vector3.up*.8f;col.isTrigger=true;
            var prefab=Resources.Load<GameObject>("CindersDungeon/RelicToken");
            if(prefab){var visual=Instantiate(prefab,altar.transform,false);PickupScale.Fit(visual,.30f);visual.transform.localPosition=Vector3.up*.65f;}
            var light=new GameObject("Relic candle").AddComponent<Light>();light.transform.SetParent(altar.transform,false);light.transform.localPosition=Vector3.up;light.type=LightType.Point;light.range=4;light.intensity=1.8f;light.color=new Color(.8f,.65f,.36f);
            return altar;
        }
        public void Interact(PlayerCharacter player)
        {
            if(Claimed||!player.State.IsAlive||player.ControlsBlocked||Vector3.Distance(transform.position,player.transform.position)>3.5f)return;
            var candidates=new List<RelicOffer>();
            for(int i=0;i<5;i++)if(!player.State.HasPassive((PassiveKind)i))candidates.Add(new RelicOffer{kind=i});
            var rng=new System.Random(seed);
            for(int i=candidates.Count-1;i>0;i--){int j=rng.Next(i+1);var tmp=candidates[i];candidates[i]=candidates[j];candidates[j]=tmp;}
            for(int i=0;i<4;i++)if(!player.State.OwnsRelic((ActiveRelic)i))
            {var offer=new RelicOffer{active=true,kind=i};if(activeReward)candidates.Insert(0,offer);else candidates.Add(offer);}
            Offers=candidates.GetRange(0,Mathf.Min(3,candidates.Count)).ToArray();
            if(!Codice.Multiplayer.LanAvatar.IsRemote(player))Codice.UI.RunHUD.Instance?.OpenRelicAltar(this);
        }
        public bool Claim(PlayerCharacter player,int offer,int replace)
        {
            if(Claimed||Offers==null||offer<0||offer>=Offers.Length||!player||Vector3.Distance(player.transform.position,transform.position)>3.5f)return false;
            if(Codice.Multiplayer.LanSession.Client){var ghost=GetComponent<Codice.Multiplayer.LanGhost>();if(!ghost)return false;Codice.Multiplayer.LanSession.Instance.ClaimRelic(ghost.snapshot.id,offer,replace);return true;}
            var choice=Offers[offer];
            bool accepted=choice.active?player.State.EquipRelic((ActiveRelic)choice.kind,replace):player.State.EquipPassive((PassiveKind)choice.kind,replace);
            if(!accepted)return false;
            Claimed=true;Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Pickup);gameObject.SetActive(false);Destroy(gameObject);return true;
        }
    }
}
