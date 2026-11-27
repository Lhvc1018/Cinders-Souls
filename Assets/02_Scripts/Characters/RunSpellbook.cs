using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Codice.Presentation;
using Codice.Mechanics;
namespace Codice.Characters
{
 public enum RunSpell {Fire,Frost,Poison,Lightning,InnerFlame,FlamePower,FrostBlade,StormBlade,VenomBlade,IronSkin,WindStep,EmberMend}
 public sealed class RunSpellbook:MonoBehaviour,IBuffSource
 {
  public readonly List<RunSpell> Known=new List<RunSpell>();public int Selected;public float MildUntil,PowerUntil,Ready;
  public float DamageFactor=>(Time.time<PowerUntil?1.8f:1)*(Time.time<MildUntil?1.15f:1);
  public static readonly string[] Names={"Bola de fuego","Escarcha inmóvil","Veneno de la cripta","Rayo del exiliado","Arde llama en mí","Llama dame poder","Filo de escarcha","Filo de tormenta","Filo de la cripta","Piel de hierro","Paso del viento","Brasa que cierra heridas"};
  public static readonly float[] Costs={18,25,22,28,25,35,22,26,20,24,20,30};
  /// Personal buffs (self only): duration in seconds. Effects are pushed into CharacterState.Spell* factors each frame.
  public static float BuffSeconds(RunSpell s)=>s switch{RunSpell.IronSkin=>15,RunSpell.WindStep=>12,RunSpell.EmberMend=>10,_=>0};
  readonly float[] buffUntil=new float[16];
  public float BuffLeft(RunSpell s)=>Mathf.Max(0,buffUntil[(int)s]-Time.time);
  /// Imbue spells: element put on the held weapon and for how long.
  public static Element ImbueOf(RunSpell s)=>s switch{RunSpell.InnerFlame=>Element.Burn,RunSpell.FrostBlade=>Element.Frost,RunSpell.StormBlade=>Element.Shock,RunSpell.VenomBlade=>Element.Poison,_=>Element.None};
  public void CollectBuffs(List<BuffInfo> into)
  {
   if(Time.time<MildUntil)into.Add(new BuffInfo("+15%",Codice.UI.HudSymbol.Censer,new Color(1f,.55f,.2f),MildUntil-Time.time,20));
   if(Time.time<PowerUntil)into.Add(new BuffInfo("+80%",Codice.UI.HudSymbol.Special,new Color(1f,.3f,.15f),PowerUntil-Time.time,12));
   if(BuffLeft(RunSpell.IronSkin)>0)into.Add(new BuffInfo("-30%",Codice.UI.HudSymbol.Ward,new Color(.7f,.72f,.78f),BuffLeft(RunSpell.IronSkin),15));
   if(BuffLeft(RunSpell.WindStep)>0)into.Add(new BuffInfo("+20%",Codice.UI.HudSymbol.Entrance,new Color(.6f,1f,.85f),BuffLeft(RunSpell.WindStep),12));
   if(BuffLeft(RunSpell.EmberMend)>0)into.Add(new BuffInfo("+vida",Codice.UI.HudSymbol.Flask,new Color(1f,.5f,.4f),BuffLeft(RunSpell.EmberMend),10));
  }
  float castUntil;public bool Casting=>Time.time<castUntil;
  public void CancelForDodge(){StopAllCoroutines();castUntil=0;}
  PlayerCharacter player;
  void Awake(){player=GetComponent<PlayerCharacter>();if(!GetComponent<ElementalStatus>())gameObject.AddComponent<ElementalStatus>();}
  public const int Capacity=4;
  public static string Describe(RunSpell s)=>s switch{RunSpell.Fire=>"Proyectil que estalla y QUEMA (daño cada segundo).",RunSpell.Frost=>"Proyectil helado que CONGELA al enemigo unos segundos.",RunSpell.Poison=>"Proyectil que ENVENENA durante 8 s.",RunSpell.Lightning=>"Rayo veloz que ELECTROCUTA y aturde.",RunSpell.InnerFlame=>"Te envuelve en llamas: +15 % de daño y ARMA ÍGNEA (quema) durante 20 s.",RunSpell.FrostBlade=>"Escarcha tu arma 25 s: cada golpe CONGELA poco a poco.",RunSpell.StormBlade=>"Electrifica tu arma 20 s: +18 % de daño y ELECTROCUTA.",RunSpell.VenomBlade=>"Unge tu arma con veneno 30 s: cada golpe ENVENENA.",RunSpell.IronSkin=>"Tu piel se vuelve hierro: −30 % de daño recibido durante 15 s.",RunSpell.WindStep=>"+20 % de velocidad de movimiento y de ataque durante 12 s.",RunSpell.EmberMend=>"Una brasa cierra tus heridas: recuperas 3 % de vida por segundo durante 10 s.",RunSpell.FlamePower=>"+80 % de daño durante 12 s a cambio de consumir tu vida.",_=>""};
  public static Color Tint(RunSpell s)=>s switch{RunSpell.Fire=>new Color(1f,.55f,.2f),RunSpell.Frost=>new Color(.55f,.85f,1f),RunSpell.Poison=>new Color(.55f,.9f,.3f),RunSpell.Lightning=>new Color(.8f,.85f,1f),RunSpell.FrostBlade=>new Color(.55f,.85f,1f),RunSpell.StormBlade=>new Color(.75f,.8f,1f),RunSpell.VenomBlade=>new Color(.55f,.9f,.3f),RunSpell.IronSkin=>new Color(.7f,.72f,.78f),RunSpell.WindStep=>new Color(.6f,1f,.85f),RunSpell.EmberMend=>new Color(1f,.5f,.4f),_=>new Color(1f,.4f,.25f)};
  /// Learning stores the spell in the inventory (max 4, the oldest is replaced) and prepares it.
  public bool Learn(RunSpell spell)
  {
   bool fresh=!Known.Contains(spell);
   if(fresh){if(Known.Count>=Capacity)Known.RemoveAt(0);Known.Add(spell);}
   Selected=Known.IndexOf(spell);
   Codice.UI.RunHUD.Instance?.Announce((fresh?"HECHIZO APRENDIDO · ":"HECHIZO PREPARADO · ")+Names[(int)spell]+" · en tu inventario · Lanzar [T]");
   GameAudio.Play(SoundCue.Orb);return fresh;
  }
  void Update()
  {
   // Personal buff effects: pushed every frame so they end exactly with the timer (and on death).
   bool alive=player.State.IsAlive;
   player.State.SpellGuard=alive&&BuffLeft(RunSpell.IronSkin)>0?.7f:1;player.State.SpellSpeed=alive&&BuffLeft(RunSpell.WindStep)>0?1.2f:1;
   if(alive&&BuffLeft(RunSpell.EmberMend)>0&&Time.timeScale>0)player.State.Heal(player.State.MaxHealth*.03f*Time.deltaTime);
   if(!player.State.IsAlive){MildUntil=PowerUntil=0;System.Array.Clear(buffUntil,0,buffUntil.Length);return;}if(Time.timeScale<=0||player.ControlsBlocked)return;
   if(Time.time<PowerUntil){float floor=player.State.MaxHealth*.2f;player.State.Damage(Mathf.Min(Mathf.Max(0,player.State.Health-floor),player.State.MaxHealth*.07f*Time.deltaTime),true);}
   if(Codice.Multiplayer.LanAvatar.IsRemote(player))return;   /* guest avatars on the host: driven by LanSession (spellPick / castSpell) */
   if(Codice.Multiplayer.LanSession.Client)
   {
    // Guest: cycle locally, ask the host to cast; the host echoes Known/Selected back.
    if((Codice.Mechanics.GameInput.KeyPressed(Codice.Mechanics.GameAction.NextSpell)||Codice.Mechanics.GameInput.PadPressed(Codice.Mechanics.GameAction.NextSpell))&&Known.Count>0)Selected=(Selected+1)%Known.Count;
    Codice.Multiplayer.LanSession.PendingSpellPick=Selected;
    if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.CastSpell)&&Known.Count>0)Codice.Multiplayer.LanSession.PendingCast=true;
    return;
   }
   var aw=GetComponent<PlayerCharacter>()?.State.ActiveWeapon;if((Codice.Mechanics.GameInput.KeyPressed(Codice.Mechanics.GameAction.NextSpell)||Codice.Mechanics.GameInput.PadPressed(Codice.Mechanics.GameAction.NextSpell)&&!(aw!=null&&aw.kind==WeaponKind.Staff))&&Known.Count>0)Selected=(Selected+1)%Known.Count;if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.CastSpell))Cast();
  }
  internal bool PredictCast()
  {
   if(Known.Count==0||Selected<0||Selected>=Known.Count||Time.time<Ready||player.ControlsBlocked||player.IsStaggered||
      player.IsDashing||!player.State.IsAlive||GetComponent<PlayerCombat>().Blocking)return false;
   if(player.State.Mana<Costs[(int)Known[Selected]]*(player.State.Class==PlayerClass.Mage?.8f:1f))return false;
   Ready=Time.time+1;castUntil=Time.time+.65f;GetComponent<ActorPresentation>()?.Play("Conjure",.65f,true);
   return true;
  }
  public bool Cast()
  {
   if(Known.Count==0||!player.State.IsAlive||Time.time<Ready||Time.timeScale<=0||player.ControlsBlocked||player.IsStaggered||player.IsDashing||Codice.Multiplayer.LanSession.Client||GetComponent<PlayerCombat>().Blocking||GetComponent<PlayerCombat>().IsSwinging)return false;
   var spell=Known[Selected];if(!player.State.SpendMana(Costs[(int)spell]))return false;Ready=Time.time+1;GetComponent<ActorPresentation>()?.Play("Conjure",.65f,true);
   castUntil=Time.time+.65f;StartCoroutine(Release(spell));return true;
  }
  System.Collections.IEnumerator Release(RunSpell spell)
  {
   yield return new WaitForSeconds(.65f*AnimationReleaseTiming.Hand);
   if(!player.State.IsAlive||player.ControlsBlocked||player.IsDashing||player.IsStaggered||Codice.UI.RunNavigation.Loading)yield break;
   GameAudio.Play(spell==RunSpell.Frost||spell==RunSpell.FrostBlade?SoundCue.SpellFrost:spell==RunSpell.Poison||spell==RunSpell.VenomBlade?SoundCue.SpellPoison:spell==RunSpell.Lightning||spell==RunSpell.StormBlade?SoundCue.SpellLightning:SoundCue.SpellFire);
   if(spell==RunSpell.InnerFlame){MildUntil=Time.time+20;GetComponent<WeaponImbue>()?.Apply(Element.Burn,20);ImportedAttackVfx.Burst(transform.position+Vector3.up,true,.3f);yield break;}
   if(BuffSeconds(spell)>0){buffUntil[(int)spell]=Time.time+BuffSeconds(spell);ImportedAttackVfx.Burst(transform.position+Vector3.up,spell==RunSpell.WindStep,.35f);yield break;}
   if(ImbueOf(spell)!=Element.None){GetComponent<WeaponImbue>()?.Apply(ImbueOf(spell),spell==RunSpell.VenomBlade?30:spell==RunSpell.FrostBlade?25:20);ImportedAttackVfx.Burst(transform.position+Vector3.up,spell==RunSpell.StormBlade,.3f);yield break;}
   if(spell==RunSpell.FlamePower){PowerUntil=Time.time+12;ImportedAttackVfx.Burst(transform.position+Vector3.up,true,.5f);yield break;}
   // A guest's avatar on the host has no humanoid avatar (no hand bone): cast from the chest along the guest's camera yaw / pitch.
   var avatar=GetComponent<Codice.Multiplayer.LanAvatar>();Vector3 origin,direction;
   if(avatar&&avatar.remote){direction=Quaternion.Euler(avatar.pitch,avatar.yaw,0)*Vector3.forward;origin=transform.position+Vector3.up*1.4f+direction*.7f;}
   else{origin=HumanPreviewCamera.ReleaseOrigin(player,true);direction=HumanPreviewCamera.AimFrom(player,origin);}
   var go=new GameObject(Names[(int)spell]);go.transform.position=origin;go.AddComponent<RunSpellProjectile>().Launch(player,spell,direction,DamageFactor);
  }
 }
 public sealed class RunSpellProjectile:MonoBehaviour
 {
  PlayerCharacter owner;RunSpell spell;Vector3 direction;public Vector3 NetVelocity=>direction*24;float age,factor;
  public void Launch(PlayerCharacter p,RunSpell s,Vector3 d,float f){owner=p;spell=s;direction=d;factor=f;SpellFlightVisual.Add(gameObject,s==RunSpell.Fire?ProjectileStyle.Flame:s==RunSpell.Poison?ProjectileStyle.Curse:ProjectileStyle.Arcane);Codice.Multiplayer.LanSession.RecordLaunch(this);}
  void Update()
  {
   if(!owner||!owner.State.IsAlive||(age+=Time.deltaTime)>3){Destroy(gameObject);return;}
   var hits=Physics.SphereCastAll(transform.position,.12f,direction,24*Time.deltaTime,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
   foreach(var hit in hits){if(hit.transform.IsChildOf(owner.transform)||hit.transform.IsChildOf(transform))continue;
    var target=hit.collider.GetComponentInParent<IDamageReceiver>() as Component;
    if(CombatTargets.IsEnemy(target)){CombatTargets.Hit(target,owner,(spell==RunSpell.Lightning?32:24)*factor);var status=target.GetComponent<ElementalStatus>()??target.gameObject.AddComponent<ElementalStatus>();status.Add(spell==RunSpell.Fire?Element.Burn:spell==RunSpell.Frost?Element.Frost:spell==RunSpell.Poison?Element.Poison:Element.Shock,100,owner.gameObject);}
    GetComponent<SpellFlightVisual>()?.Impact(hit.point);if(spell==RunSpell.Lightning)ImportedAttackVfx.Burst(hit.point,false,.5f);Destroy(gameObject);return;}
   transform.position+=direction*24*Time.deltaTime;
  }
 }
 public sealed class SpellPickup:MonoBehaviour,IPlayerInteractable
 {
  public RunSpell spell;
  public void Interact(PlayerCharacter p){if(!p||!p.State.IsAlive||p.ControlsBlocked||Vector3.Distance(transform.position,p.transform.position)>3.5f||Codice.Multiplayer.LanSession.Client)return;   /* host learns for itself and for guest avatars */p.GetComponent<RunSpellbook>().Learn(spell);Destroy(gameObject);}
  public static void Create(Vector3 position,RunSpell spell,Transform parent)
  {
   var go=new GameObject("Escritura: "+RunSpellbook.Names[(int)spell]);go.transform.SetParent(parent);go.transform.position=position;var pickup=go.AddComponent<SpellPickup>();pickup.spell=spell;
   var model=Resources.Load<GameObject>("CindersDungeon/RelicToken");if(model){var visual=Instantiate(model,go.transform,false);PickupScale.Fit(visual,.4f);}
   var col=go.AddComponent<SphereCollider>();col.radius=.4f;col.isTrigger=true;
  }
 }
}

