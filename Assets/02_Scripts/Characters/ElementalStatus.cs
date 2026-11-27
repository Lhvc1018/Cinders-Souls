using UnityEngine;
using Codice.Mechanics;
namespace Codice.Characters
{
 public enum Element {None,Burn,Frost,Poison,Shock}
 public sealed class ElementalStatus:MonoBehaviour
 {
  public readonly float[] Buildup=new float[5],Until=new float[5];
  GameObject source;float tick;
  public bool Frozen=>Time.time<Until[(int)Element.Frost]||Time.time<Until[(int)Element.Shock];
  public void Add(Element element,float amount,GameObject attacker)
  {
   int i=(int)element;if(i==0||amount<=0||!float.IsFinite(amount)||Time.time<Until[i])return;source=attacker;Buildup[i]=Mathf.Clamp(Buildup[i]+amount,0,100);
   bool npc=!GetComponent<PlayerCharacter>();if(npc)Codice.Presentation.StatusAura.Ensure(this);
   if(Buildup[i]<100)return;Buildup[i]=0;Until[i]=Time.time+(element==Element.Poison?8:element==Element.Burn?5:element==Element.Frost?2:1);
   if(npc)Codice.Presentation.StatusAura.Proc(this,element);
   if(!GetComponent<PlayerCharacter>()&&(element==Element.Frost||element==Element.Shock))CombatTargets.Stun(this.GetComponent<IDamageReceiver>() as Component,GetComponent<Codice.Bosses.BossCharacter>()?.35f:element==Element.Frost?2:1);
  }
  void Update()
  {
   var p=GetComponent<PlayerCharacter>();if(p&&!p.State.IsAlive){System.Array.Clear(Buildup,0,5);System.Array.Clear(Until,0,5);return;}
   var enemy=GetComponent<Codice.Enemies.BasicEnemy>();var boss=GetComponent<Codice.Bosses.BossCharacter>();if(enemy&&!enemy.IsAlive||boss&&!boss.IsAlive)return;
   for(int i=1;i<5;i++)Buildup[i]=Mathf.MoveTowards(Buildup[i],0,Time.deltaTime*5);
   if(Time.time<tick||Time.timeScale<=0)return;tick=Time.time+1;
   for(int i=1;i<5;i++)if(Time.time<Until[i]&&(i==1||i==3))
   {float damage=i==1?4:3;if(p)p.State.Damage(damage,true);else GetComponent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo{amount=damage,source=source,secondary=true,poison=true});}
  }
 }
}
