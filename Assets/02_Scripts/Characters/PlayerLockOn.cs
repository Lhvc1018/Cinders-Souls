using UnityEngine;
using Codice.Mechanics;
using Codice.Enemies;
using Codice.Bosses;
namespace Codice.Characters
{
 public sealed class PlayerLockOn : MonoBehaviour
 {
  PlayerCharacter player; public Component Target {get;private set;}
  float hiddenSince=-1;
  public Vector3 Point { get { if(!Target)return transform.position;var col=Target.GetComponent<Collider>();return col?col.bounds.center:Target.transform.position+Vector3.up; } }
  void Awake(){player=GetComponent<PlayerCharacter>();}
  public static bool Alive(Component c)=>c&&c.gameObject.activeInHierarchy&&((c is BasicEnemy e&&e.IsAlive)||(c is BossCharacter b&&b.IsAlive)||(c is Codice.Multiplayer.LanGhost g&&g.snapshot!=null&&g.snapshot.key!=null&&(g.snapshot.key.StartsWith("Enemy:")||g.snapshot.key.StartsWith("Boss:"))&&g.snapshot.health>0));
  public bool Acquire(){
   Target=null;float best=float.MaxValue;
   System.Collections.Generic.IEnumerable<Component> pool=Codice.Multiplayer.LanSession.Client?(System.Collections.Generic.IEnumerable<Component>)FindObjectsByType<Codice.Multiplayer.LanGhost>(FindObjectsSortMode.None):CombatTargets.InRadius(transform.position+Vector3.up,18);   /* guests only have enemy ghosts */
   foreach(var c in pool){
    if(!Alive(c)||Vector3.Distance(transform.position,c.transform.position)>18)continue;var col=c.GetComponent<Collider>();var point=col?col.bounds.center:c.transform.position+Vector3.up;var screen=player.view.WorldToViewportPoint(point);
    if(screen.z<=0||screen.x<0||screen.x>1||screen.y<0||screen.y>1)continue;
    float score=Vector2.Distance(new Vector2(screen.x,screen.y),new Vector2(.5f,.5f))*20+Vector3.Distance(transform.position,c.transform.position)*.1f;
    if(score<best){best=score;Target=c;}
   }
   hiddenSince=-1;Debug.Log(Target?"[LockOn] Target acquired":"[LockOn] No visible target");return Target;
  }
  public void Toggle(){if(Target)Clear();else Acquire();}
  public void Clear(){Target=null;hiddenSince=-1;}
  public void Validate(){
   if(!Target)return;
   if(!player.State.IsAlive||!Alive(Target)||Vector3.Distance(transform.position,Point)>22){Clear();return;}
   if(Target is Codice.Multiplayer.LanGhost||CombatTargets.Visible(transform.position+Vector3.up,Target))hiddenSince=-1;
   else if(hiddenSince<0)hiddenSince=Time.time;else if(Time.time-hiddenSince>.6f)Clear();
  }
  void Update(){Validate();}
  void OnGUI(){
   if(!Target||!player.view||Time.timeScale==0||player.ControlsBlocked)return;
   var p=player.view.WorldToScreenPoint(Point);if(p.z<=0)return;
   var previous=GUI.color;GUI.color=new Color(1,.9f,.65f);float size=8;
   GUI.DrawTexture(new Rect(p.x-size/2,Screen.height-p.y-size/2,size,size),Texture2D.whiteTexture);GUI.color=previous;
  }
 }
}
