using UnityEngine;

namespace Codice.Characters
{
    public sealed class WeaponPickup : MonoBehaviour, IPlayerInteractable
    {
        public WeaponInstance weapon = new WeaponInstance(WeaponKind.Sword);
        public GameObject visual;
        public bool Emerging { get; private set; }
        Vector3 revealOrigin, revealEnd;
        float revealTime, revealDuration;
        Collider[] revealColliders;
        bool[] colliderEnabled;
        public void BeginChestReveal(Vector3 destination, float duration)
        {
            revealOrigin=transform.position;revealEnd=destination;
            revealTime=0;revealDuration=Mathf.Max(.01f,duration);Emerging=true;
            revealColliders=GetComponentsInChildren<Collider>();colliderEnabled=new bool[revealColliders.Length];
            for(int i=0;i<revealColliders.Length;i++){colliderEnabled[i]=revealColliders[i].enabled;revealColliders[i].enabled=false;}
        }
        void Update()
        {
            if(!Emerging)return;
            revealTime+=Time.deltaTime;float t=Mathf.Clamp01(revealTime/revealDuration);
            transform.position=Vector3.Lerp(revealOrigin,revealEnd,t*t*(3-2*t));
            if(t<1)return;
            Emerging=false;
            for(int i=0;i<revealColliders.Length;i++)if(revealColliders[i])revealColliders[i].enabled=colliderEnabled[i];
        }
        public void Interact(PlayerCharacter player)
        {
            if (Emerging || weapon == null || !player.State.IsAlive || player.ControlsBlocked) return;
            if (ClassArsenal.Forbidden(player.State.Class, weapon)) { Codice.UI.RunHUD.Instance?.Announce(ClassArsenal.ClassName(player.State.Class) + " no puede empuñar " + Codice.UI.UIWidgets.WeaponName(weapon)); Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Pickup); return; }
            if(!Codice.Multiplayer.LanSession.Active&&Codice.UI.RunHUD.Instance){Codice.UI.RunHUD.Instance.OpenWeaponChoice(this);return;}
            EquipImmediately(player);
        }
        public void EquipImmediately(PlayerCharacter player,int slot=-1)
        {
            if(Emerging||weapon==null||!player||!player.State.IsAlive)return;
            var previous = slot<0?player.State.Equip(weapon):player.State.EquipIntoSlot(weapon,slot);
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Pickup);
            weapon = previous;
            if (previous == null) Destroy(gameObject);
            else
            {
                name = "Dropped " + previous.kind + " T" + previous.tier;
                player.GetComponent<PlayerCombat>()?.AddPickupVisual(this);
            }
        }
    }
}
