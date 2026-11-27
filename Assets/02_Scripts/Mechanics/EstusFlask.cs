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
            if(Drinking||!player.State.IsAlive||player.ControlsBlocked||player.IsStaggered||player.IsDashing||
                !player.Grounded||Time.timeScale<=0||Codice.UI.RunNavigation.Loading||player.State.EstusCharges<=0||player.State.Health>=player.State.MaxHealth||
                (combat&&(combat.IsSwinging||combat.IsReloading))||(Codice.UI.RunHUD.Instance&&(Codice.UI.RunHUD.Instance.Paused||Codice.UI.RunHUD.Instance.ResultsVisible)))return false;
            GetComponent<ActorPresentation>()?.Play("Heal",DrinkSeconds,true);Drinking=true;elapsed=0;healthAtStart=player.State.Health;player.SetHealingControlsBlocked(true);if(bottle)bottle.SetActive(true);return true;
        }
        void Update()
        {
            if(!Drinking){if(!Codice.Multiplayer.LanSession.Client&&!Codice.Multiplayer.LanAvatar.IsRemote(player)&&Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Flask))TryDrink();return;}
            if(!player.State.IsAlive||player.IsStaggered||player.State.Health<healthAtStart||Codice.UI.RunNavigation.Loading){Interrupt();return;}
            if(Time.timeScale<=0)return;
            elapsed+=Time.deltaTime;
            if(bottle)bottle.SetActive(!(GetComponent<HumanPreviewCamera>()?.ThirdPerson??false));
            if(bottle){bottle.transform.localPosition=new Vector3(.25f,-.28f+Mathf.Sin(Progress*Mathf.PI)*.12f,.62f);bottle.transform.localRotation=Quaternion.Euler(-25*Progress,0,-15);}
            if(elapsed<DrinkSeconds)return;
            if(player.State.DrinkEstus()){RelicPulse.Spawn(transform.position+Vector3.up*.2f,1.2f,new Color(1,.58f,.12f));GameAudio.Play(SoundCue.Heal);Debug.Log("[Estus] Healed 40%; charges="+player.State.EstusCharges);}
            Interrupt();
        }
        public void Interrupt(){Drinking=false;elapsed=0;if(player)player.SetHealingControlsBlocked(false);if(bottle)bottle.SetActive(false);}
        void OnDisable(){Interrupt();}
        void OnDestroy(){if(bottle)Destroy(bottle);}
    }
}
