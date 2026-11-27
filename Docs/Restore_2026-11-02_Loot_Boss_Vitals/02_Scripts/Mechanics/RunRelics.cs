using Codice.Characters;
using Codice.Presentation;
using UnityEngine;

namespace Codice.Mechanics
{
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class RunRelics : MonoBehaviour
    {
        PlayerCharacter player;
        void Awake(){player=GetComponent<PlayerCharacter>();}
        public bool Activate(int slot)
        {
            if(Codice.Multiplayer.LanSession.Active||!player.State.IsAlive||player.ControlsBlocked||player.IsStaggered||Time.timeScale<=0||
                Codice.UI.RunNavigation.Loading||(Codice.UI.RunHUD.Instance&&(Codice.UI.RunHUD.Instance.Paused||Codice.UI.RunHUD.Instance.ResultsVisible)))return false;
            if(!player.State.ConsumeRelic(slot))return false;
            var kind=player.State.GetRelic(slot);var origin=transform.position+Vector3.up;
            GameAudio.Play(SoundCue.Power);
            if(kind==ActiveRelic.BloodLance)
            {
                var direction=player.view.transform.forward;origin=player.view.transform.position;float length=12;
                foreach(var hit in Physics.RaycastAll(origin,direction,length,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.collider.GetComponentInParent<PlayerCharacter>()&&!CombatTargets.IsEnemy(hit.collider.GetComponentInParent<IDamageReceiver>() as Component))length=Mathf.Min(length,hit.distance);
                foreach(var target in CombatTargets.InRadius(origin,12))
                {
                    var col=target.GetComponent<Collider>();var delta=col.bounds.center-origin;float along=Vector3.Dot(delta,direction);
                    if(along>=0&&along<=length&&Vector3.Distance(col.ClosestPoint(origin+direction*along),origin+direction*along)<.8f)CombatTargets.Hit(target,player,85,false,0,true);
                }
                SpecialVisuals.Beam(origin+direction*.6f,origin+direction*length,.16f,true);
            }
            else if(kind!=ActiveRelic.AshenWard)
            {
                bool frost=kind==ActiveRelic.FrostBell;float radius=frost?5:4;
                foreach(var target in CombatTargets.InRadius(origin,radius))
                {
                    CombatTargets.Hit(target,player,frost?24:70,false,frost?0:3,true);
                    if(frost&&target)CombatTargets.Stun(target,target is Codice.Bosses.BossCharacter?.35f:1.4f);
                }
                RelicPulse.Spawn(transform.position+Vector3.up*.15f,radius,frost?new Color(.55f,.75f,.83f):new Color(.9f,.36f,.12f));
            }
            else RelicPulse.Spawn(transform.position+Vector3.up*.2f,1.3f,new Color(.75f,.66f,.4f));
            Codice.UI.RunHUD.Instance?.Announce(RelicCatalog.Name(kind));
            return true;
        }
    }
    public sealed class RelicPulse:MonoBehaviour
    {
        LineRenderer line;Material material;float age,radius;Color tint;
        public static void Spawn(Vector3 point,float radius,Color color)
        {
            var fx=new GameObject("Relic pulse").AddComponent<RelicPulse>();fx.transform.position=point;fx.radius=radius;fx.tint=color;
            fx.line=fx.gameObject.AddComponent<LineRenderer>();fx.line.loop=true;fx.line.useWorldSpace=false;fx.line.positionCount=64;
            fx.material=new Material(Shader.Find("Sprites/Default"));fx.line.sharedMaterial=fx.material;fx.line.startWidth=fx.line.endWidth=.07f;
        }
        void Update(){age+=Time.deltaTime;float r=radius*Mathf.SmoothStep(.1f,1,age/.35f);var c=tint;c.a=Mathf.Clamp01(1-age/.65f);line.startColor=line.endColor=c;for(int i=0;i<64;i++){float a=i*Mathf.PI/32;line.SetPosition(i,new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}if(age>=.65f)Destroy(gameObject);}
        void OnDestroy(){if(material)Destroy(material);}
    }
}
