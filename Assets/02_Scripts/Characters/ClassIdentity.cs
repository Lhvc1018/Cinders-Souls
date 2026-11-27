using UnityEngine;
using Codice.Mechanics;
namespace Codice.Characters {
 public sealed class ClassIdentity:MonoBehaviour {
  PlayerClass previousClass; PlayerCharacter player; float guardStarted=-10, riposteUntil, focus; bool guarded; Component marked; float markUntil; float firmness; int spellChain; StaffSpell lastSpell;
  void Awake(){player=GetComponent<PlayerCharacter>();previousClass=player.State.Class;}
  void Update(){
   if(previousClass!=player.State.Class){previousClass=player.State.Class;guardStarted=-10;riposteUntil=focus=firmness=markUntil=0;guarded=false;marked=null;spellChain=0;}
   if(!player.State.IsAlive){firmness=focus=0;marked=null;return;}
   bool block=GetComponent<PlayerCombat>().Blocking;
   if(block&&!guarded)guardStarted=Time.time;guarded=block;
   if(player.State.Class==PlayerClass.Archer&&player.State.ActiveWeapon?.kind==WeaponKind.Bow&&!player.IsDashing&&!player.ControlsBlocked&&player.MotorVelocity.sqrMagnitude<.1f)focus=Mathf.Min(1,focus+Time.deltaTime*ClassArsenal.FocusRate(player.State,player.State.ActiveWeapon)/.8f);else focus=0;
  }
  public string Status {get{switch(player.State.Class){
   case PlayerClass.Warrior:return Time.time<riposteUntil?"REPRESALIA LISTA":"CABALLERO · Bloqueo preciso: represalia";
   case PlayerClass.Tank:return "FIRMEZA "+Mathf.RoundToInt(firmness)+"/100 · Golpea para descargar";
   case PlayerClass.Archer:return "PRECISIÓN "+Mathf.RoundToInt(focus*100)+"% · Detente para apuntar";
   case PlayerClass.Mage:return "SOBRECARGA "+spellChain+"/3 · Alterna conjuros";
   case PlayerClass.Shinobi:return "SHINOBI · Parry 0.4 s · Contraataque";
   default:return marked&&Time.time<markUntil?"PRESA MARCADA · Flanquea para +60%":"ASSASIN · Golpea para marcar una presa";
  }}}
  public void AddFirmness(float amount){if(player.State.Class==PlayerClass.Tank)firmness=Mathf.Min(100,firmness+amount);}
  public string Arsenal{get{var w=player.State.ActiveWeapon;if(w==null)return "";var a=ClassArsenal.Effective(player.State,w);return " · <color="+ClassArsenal.Color(a)+">"+Codice.UI.UIWidgets.WeaponName(w)+" "+ClassArsenal.Label(a)+"</color>";}}
  /// Guard raised within the perfect window (Caballero: his riposte window; other shields: 0.22 s). Consumes it.
  internal bool InOwnedParryWindow=>guarded&&Time.time-guardStarted<=(player.State.Class==PlayerClass.Warrior?ClassArsenal.RiposteWindow(player.State):.22f);
  public bool ConsumeParryWindow(){
   if(!guarded)return false;float window=player.State.Class==PlayerClass.Warrior?ClassArsenal.RiposteWindow(player.State):.22f;
   if(Time.time-guardStarted>window)return false;guardStarted=-10;if(player.State.Class==PlayerClass.Warrior)riposteUntil=Time.time+3;return true;}
  internal void SetContactTiming(Codice.Multiplayer.DefenseTiming timing){
   guarded=timing>=Codice.Multiplayer.DefenseTiming.Guard;
   guardStarted=timing==Codice.Multiplayer.DefenseTiming.Perfect?Time.time:-10;
  }
  public float Incoming(DamageInfo hit,bool ownerFacing=false){
   if(player.State.Class==PlayerClass.Tank&&!hit.secondary){firmness=Mathf.Min(100,firmness+hit.amount);return hit.amount*.8f;}
   if(player.State.Class==PlayerClass.Warrior&&hit.melee&&!hit.secondary)hit.amount*=ClassArsenal.GuardFactor(player.State);
   if(hit.secondary||hit.ignoreGuard||hit.breaksGuard||!hit.melee||!guarded||(!ownerFacing&&Vector3.Dot(transform.forward,-hit.direction.normalized)<.5f))return hit.amount;
   // Blocking a frontal melee hit costs stamina in proportion to the blow; an empty bar breaks the guard (stagger, full damage).
   var stamina=GetComponent<PlayerStamina>();
   if(stamina&&!stamina.Infinite&&stamina.InCombat){float cost=PlayerStamina.Cost(PlayerStamina.Action.Block)*Mathf.Clamp(hit.amount/18f,.6f,2.5f);if(!stamina.TrySpendAmount(cost)){player.GuardBreak();return hit.amount;}}
   // Perfect guard (Caballero riposte window, or any shield): no damage, attacker stunned, full parry feedback.
   if(ConsumeParryWindow()){var point=transform.position+Vector3.up*1.2f+transform.forward*.6f;Parry.Feedback(player,point,hit.direction,false);
    if(hit.source){var enemy=hit.source.GetComponentInParent<IDamageReceiver>() as Component;if(CombatTargets.IsEnemy(enemy)){if(!EnemyPosture.Add(enemy,30))CombatTargets.Stun(enemy,enemy is Codice.Bosses.BossCharacter?.5f:.8f);}}
    return 0;}
   return hit.amount;
  }
  public float Outgoing(Component target,WeaponInstance weapon){
   if(!target)return 1;
   switch(player.State.Class){
    case PlayerClass.Warrior: if(!weapon.Ranged&&Time.time<riposteUntil){riposteUntil=0;return 1.75f;}break;
    case PlayerClass.Tank: if(!weapon.Ranged&&firmness>=60){firmness=0;CombatTargets.Stun(target,1.2f);return 1.8f;}break;
    case PlayerClass.Berserker:
     if(marked==target&&Time.time<markUntil&&Vector3.Dot(target.transform.forward,(transform.position-target.transform.position).normalized)<.25f){player.State.RestoreMana(5);return 1.6f;}
     marked=target;markUntil=Time.time+8;break;
   }return 1;
  }
  public float ConsumePrecision(){if(player.State.Class!=PlayerClass.Archer)return 1;float result=focus>=.99f?1.65f:1;focus=0;return result;}
  public float SpellPower(StaffSpell spell){if(player.State.Class!=PlayerClass.Mage)return 1;spellChain=spell!=lastSpell?spellChain+1:1;lastSpell=spell;if(spellChain>=3){spellChain=0;player.State.RestoreMana(15);return 1.65f;}return 1.15f;}
 }
}
