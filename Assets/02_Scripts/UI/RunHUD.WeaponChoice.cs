using Codice.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
 public sealed partial class RunHUD
 {
  RectTransform weaponChoicePanel;
  WeaponPickup pendingWeapon;
  TextMeshProUGUI incomingWeaponText;
  readonly TextMeshProUGUI[] replacementWeaponText=new TextMeshProUGUI[2];
  readonly Image[] replacementWeaponIcons=new Image[2];Image incomingWeaponIcon;
  public bool WeaponSelectionVisible=>weaponChoicePanel&&weaponChoicePanel.gameObject.activeSelf;
  void BuildWeaponChoice()
  {
   weaponChoicePanel=Panel("Weapon comparison",-650,-390,1300,780);Anchor(weaponChoicePanel,.5f,.5f);
   UIWidgets.Label(weaponChoicePanel,"Title",40,25,1200,65,34,ProceduralUISprites.Gold).text="UN NUEVO INSTRUMENTO DE CONDENA";
   var candidate=UIWidgets.Surface(weaponChoicePanel,"Found weapon",340,105,620,235);
   incomingWeaponIcon=UIWidgets.Image(candidate,"Icon",20,26,100,100,Color.white);
   incomingWeaponText=UIWidgets.Label(candidate,"Comparison",145,25,440,200,23);
   for(int i=0;i<2;i++)
   {
    int slot=i;var card=UIWidgets.Surface(weaponChoicePanel,"Current weapon "+i,40+i*630,370,590,300);
    replacementWeaponIcons[i]=UIWidgets.Image(card,"Icon",22,22,80,80,Color.white);
    replacementWeaponText[i]=UIWidgets.Label(card,"Stats",125,20,440,195,22);
    UIWidgets.Button(card,"REEMPLAZAR ESTA ARMA",18,236,550,48,()=>ConfirmWeapon(slot));
   }
   UIWidgets.Button(weaponChoicePanel,"CONSERVAR MI EQUIPO",415,708,470,48,()=>SetPaused(false));weaponChoicePanel.gameObject.SetActive(false);
  }
  public void OpenWeaponChoice(WeaponPickup pickup)
  {
   if(!pickup||ShopVisible||ResultsVisible||Paused)return;
   if(!weaponChoicePanel)BuildWeaponChoice();pendingWeapon=pickup;weaponChoicePanel.gameObject.SetActive(true);SetPaused(true);
   var w=pickup.weapon;incomingWeaponIcon.sprite=ProceduralUISprites.Weapon(w);incomingWeaponIcon.color=ProceduralUISprites.Tier(w.tier);
   incomingWeaponText.text=$"<color=#D6B77A>{UIWidgets.WeaponName(w)} · T{w.tier}</color>\n{w.Damage:0} DAÑO\n\n{UIWidgets.Special(w.kind)}\n{(w.Ranged?"Ataque a distancia":"Ataque cuerpo a cuerpo")}\n\n{Codice.Characters.ClassArsenal.Describe(Player.State,w)}";
   for(int i=0;i<2;i++)
   {
    var old=Player.State.GetWeapon(i);replacementWeaponIcons[i].sprite=old!=null?ProceduralUISprites.Weapon(old):ProceduralUISprites.Shape("diamond");
    replacementWeaponText[i].text=old==null?"RANURA VACÍA\n\nGuardar sin perder equipo.":$"{UIWidgets.WeaponName(old)} · T{old.tier}\n{old.Damage:0} DAÑO · <color={Codice.Characters.ClassArsenal.Color(Codice.Characters.ClassArsenal.Affinity(Player.State.Class,old))}>{Codice.Characters.ClassArsenal.Label(Codice.Characters.ClassArsenal.Affinity(Player.State.Class,old))}</color>\n{UIWidgets.Special(old.kind)}\n\n<color={(w.Damage>=old.Damage?"#8FC9A2":"#DE8D83")}>{w.Damage-old.Damage:+0;-0;0} daño al cambiar</color>\nEl arma sustituida queda en el suelo.";
   }
  }
  void ConfirmWeapon(int slot)
  {
   if(!pendingWeapon||!Player.State.IsAlive){SetPaused(false);return;}
   if(Vector3.Distance(Player.transform.position,pendingWeapon.transform.position)>4){SetPaused(false);return;}
   Player.State.SelectWeapon(slot);var pickup=pendingWeapon;SetPaused(false);pickup.EquipImmediately(Player,slot);pendingWeapon=null;
  }
 }
}
