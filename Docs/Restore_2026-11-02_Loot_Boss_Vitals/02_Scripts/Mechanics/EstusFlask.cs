using Codice.Characters;
using Codice.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Codice.Mechanics
{
    public sealed class EstusFlask:MonoBehaviour
    {
        public const float DrinkSeconds=1.05f;
        public bool Drinking {get;private set;}
        public float Progress=>Drinking?elapsed/DrinkSeconds:0;
        PlayerCharacter player;float elapsed,healthAtStart;GameObject bottle;
        void Awake()
        {
            player=GetComponent<PlayerCharacter>();var prefab=Resources.Load<GameObject>("CindersDungeon/EstusBottle");
            if(prefab&&player.view){bottle=Instantiate(prefab,player.view.transform,false);bottle.transform.localPosition=new Vector3(.25f,-.28f,.62f);bottle.SetActive(false);}
        }
        public bool TryDrink()
        {
            var combat=player.GetComponent<PlayerCombat>();
            if(Drinking||Codice.Multiplayer.LanSession.Active||!player.State.IsAlive||player.ControlsBlocked||player.IsStaggered||player.IsDashing||
                !player.Grounded||Time.timeScale<=0||Codice.UI.RunNavigation.Loading||player.State.EstusCharges<=0||player.State.Health>=player.State.MaxHealth||
                (combat&&(combat.IsSwinging||combat.IsReloading))||(Codice.UI.RunHUD.Instance&&(Codice.UI.RunHUD.Instance.Paused||Codice.UI.RunHUD.Instance.ResultsVisible)))return false;
            Drinking=true;elapsed=0;healthAtStart=player.State.Health;player.SetHealingControlsBlocked(true);if(bottle)bottle.SetActive(true);return true;
        }
        void Update()
        {
            if(!Drinking){if(Keyboard.current!=null&&Keyboard.current.hKey.wasPressedThisFrame)TryDrink();return;}
            if(!player.State.IsAlive||player.IsStaggered||player.State.Health<healthAtStart||Codice.UI.RunNavigation.Loading){Interrupt();return;}
            if(Time.timeScale<=0)return;
            elapsed+=Time.deltaTime;
            if(bottle){bottle.transform.localPosition=new Vector3(.25f,-.28f+Mathf.Sin(Progress*Mathf.PI)*.12f,.62f);bottle.transform.localRotation=Quaternion.Euler(-25*Progress,0,-15);}
            if(elapsed<DrinkSeconds)return;
            if(player.State.DrinkEstus()){RelicPulse.Spawn(transform.position+Vector3.up*.2f,1.2f,new Color(1,.58f,.12f));GameAudio.Play(SoundCue.Pickup);Debug.Log("[Estus] Healed 40%; charges="+player.State.EstusCharges);}
            Interrupt();
        }
        public void Interrupt(){Drinking=false;elapsed=0;if(player)player.SetHealingControlsBlocked(false);if(bottle)bottle.SetActive(false);}
        void OnDisable(){Interrupt();}
        void OnDestroy(){if(bottle)Destroy(bottle);}
    }
}
