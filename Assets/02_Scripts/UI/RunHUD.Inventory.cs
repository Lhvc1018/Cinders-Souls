using Codice.Characters;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Codice.Presentation;
namespace Codice.UI
{
 public sealed partial class RunHUD
 {
  int inventorySelection;
  const int InventorySlots=16;
  readonly Image[] inventoryIcons=new Image[InventorySlots];
  readonly TextMeshProUGUI[] inventoryCaptions=new TextMeshProUGUI[InventorySlots];
  TextMeshProUGUI itemTitle,itemDescription,inventoryStatus;
  Image inventoryPreview;
  Button equipInventory;
  void BuildInventory()
  {
   pausePanel=Panel("Inventory",-810,-430,1620,860);Anchor(pausePanel,.5f,.5f);
   pausePanel.GetComponent<Image>().color=new Color(.045f,.049f,.044f,.94f);
   UIWidgets.Ornament(pausePanel,42,140,1534);
   UIWidgets.Label(pausePanel,"Heading",42,30,1100,65,38,ProceduralUISprites.Gold).text="INVENTARIO DEL PEREGRINO";
   UIWidgets.Label(pausePanel,"Subtitle",44,104,1400,36,20).text="ARMAS  ·  FRASCO  ·  RELIQUIAS  ·  PASIVAS  ·  HECHIZOS  ·  TALISMANES";
   UIWidgets.Image(pausePanel,"Divider",555,165,1,570,new Color(.48f,.41f,.27f,.5f));
   UIWidgets.Image(pausePanel,"Divider",1110,165,1,570,new Color(.48f,.41f,.27f,.5f));
   // 4 x 4 grid: row 1 weapons + flask, row 2 active relics + passives, row 3 learned spells, row 4 talismans.
   for(int i=0;i<InventorySlots;i++)
   {
    int slot=i;float x=44+(i%4)*126,y=180+(i/4)*140;
    var cell=UIWidgets.Button(pausePanel,"",x,y,118,132,()=>{inventorySelection=slot;RefreshInventory();});
    cell.GetComponent<RelicButtonFace>().primary=i==inventorySelection;
    inventoryIcons[i]=UIWidgets.Image(cell.transform,"Item",31,10,56,56,ProceduralUISprites.Gold,"hex");
    inventoryCaptions[i]=UIWidgets.Label(cell.transform,"Caption",4,70,110,58,15);inventoryCaptions[i].alignment=TextAlignmentOptions.Center;
   }
   itemTitle=UIWidgets.Label(pausePanel,"Item name",592,185,480,85,30,ProceduralUISprites.Gold);
   inventoryPreview=UIWidgets.Image(pausePanel,"Item preview",760,285,120,120,ProceduralUISprites.Gold,"hex");
   itemDescription=UIWidgets.Label(pausePanel,"Item effects",592,450,480,250,23);
   inventoryStatus=UIWidgets.Label(pausePanel,"Character status",1150,190,420,525,23);
   equipInventory=UIWidgets.Button(pausePanel,"EQUIPAR",592,710,480,50,EquipSelected);
   UIWidgets.Button(pausePanel,"REANUDAR [I / Escape]",44,780,470,48,()=>SetPaused(false));
   UIWidgets.Button(pausePanel,"MENÚ PRINCIPAL",1145,780,430,48,RunNavigation.MainMenu);
   UIWidgets.Button(pausePanel,"CONTROLES",592,780,480,48,()=>{controlsSheet.gameObject.SetActive(true);controlsSheet.GetComponentInChildren<Button>()?.Select();});
   controlsSheet=ControlsSheet.Build(transform,()=>{controlsSheet.gameObject.SetActive(false);pausePanel.GetComponentInChildren<Button>()?.Select();});controlsSheet.gameObject.SetActive(false);
  }
  /// What each inventory cell shows: (name, detail, icon, tint, can be equipped/selected).
  (string name,string detail,Sprite icon,Color tint,bool equip) InventoryEntry(int i)
  {
   var s=Player.State;var gold=ProceduralUISprites.Gold;
   switch(i)
   {
    case 0:case 1:{var w=s.GetWeapon(i);return w!=null?(UIWidgets.WeaponName(w),$"ARMA · TIER {w.tier}\n\n{w.Damage:0} daño\n{UIWidgets.Special(w.kind)}\n\nMunición: {w.ammunition}\n{(s.ActiveSlot==i?"Equipada actualmente":"Pulsa EQUIPAR para empuñarla")}",ProceduralUISprites.Weapon(w),gold,s.ActiveSlot!=i):("Ranura vacía","Encuentra armas en cofres o en el calabozo.",ProceduralUISprites.Shape("diamond"),gold,false);}
    case 2:return ("Frasco de ceniza",$"CONSUMIBLE · {s.EstusCharges}/{CharacterState.EstusCapacity}\n\nRestaura 40% de vida. El daño interrumpe la bebida.\nUsar: [H]\nSe rellena al bajar de piso, en fuentes y con mercaderes.",Resources.Load<Sprite>("CindersIcons/EstusBottle")??ProceduralUISprites.Shape("diamond"),Color.white,false);
    case 3:return ("","",null,Color.clear,false);
    case 4:case 5:{var relic=s.GetRelic(i-4);return (RelicCatalog.Name(relic),$"RELIQUIA ACTIVA [{(i==4?"F":"G")}]\n\n{RelicCatalog.ManaCost(relic)} maná · {RelicCatalog.Cooldown(relic):0.#} s de recarga\n\n{RelicCatalog.Description(relic)}",Resources.Load<Sprite>(i==4?"CindersIcons/RelicToken":"CindersIcons/ManaShard")??ProceduralUISprites.Shape("hex"),Color.white,false);}
    case 6:case 7:{int slot=i-6;bool present=slot<s.PassiveCount;return present?(UIWidgets.Passive(s.GetPassive(slot)),"BENDICIÓN PASIVA · actúa sola\n\n"+RelicCatalog.PassiveDescription(s.GetPassive(slot)),Resources.Load<Sprite>("CindersIcons/Passive"+(int)s.GetPassive(slot))??ProceduralUISprites.Shape("hex"),Color.white,false):("Pasiva vacía","Acepta una bendición en un relicario.",ProceduralUISprites.Shape("hex"),gold,false);}
   }
   if(i>=8&&i<12)
   {
    // Learned spells (scrolls found in the world). Selecting one makes it the spell cast with [T].
    var book=Player.GetComponent<RunSpellbook>();int k=i-8;
    if(book&&k<book.Known.Count){var spell=book.Known[k];bool sel=book.Selected==k;return (RunSpellbook.Names[(int)spell],$"HECHIZO APRENDIDO · {RunSpellbook.Costs[(int)spell]} maná\n\n{RunSpellbook.Describe(spell)}\n\nLanzar: [T]  ·  Cambiar: [C]\n{(sel?"Preparado":"Pulsa EQUIPAR para prepararlo")}",ProceduralUISprites.Shape("diamond"),RunSpellbook.Tint(spell),!sel);}
    return ("Hechizo vacío","Las escrituras que encuentres se guardan aquí.",ProceduralUISprites.Shape("diamond"),new Color(.35f,.35f,.4f),false);
   }
   if(i>=12)return TalismanEntry(i-12);
   return ("","",null,Color.clear,false);
  }
  /// Talisman slot k: the equipped talisman, or an empty slot. EQUIPAR cycles through owned talismans that are not worn.
  (string name,string detail,Sprite icon,Color tint,bool equip) TalismanEntry(int k)
  {
   var st=Player.State;int t=st.TalismanInSlot(k);bool spare=NextSpareTalisman(t)>=0||t>=0;
   if(t<0)return ("Talismán vacío",$"RANURA DE TALISMÁN {k+1}/{TalismanCatalog.Slots}\n\nAnillos y amuletos se encuentran en cofres.\nTienes {st.OwnedTalismanCount} talismán(es).",ProceduralUISprites.Shape("hex"),new Color(.35f,.35f,.4f),spare);
   var e=TalismanCatalog.All[t];
   return (e.name,$"{e.kind.ToUpper()} · PASIVO\n\n{TalismanCatalog.Describe((Talisman)t)}\n\nCAMBIAR pasa al siguiente talismán que tengas; QUITAR libera la ranura.",TalismanIcons.Get((Talisman)t),Color.white,spare);
  }
  int NextSpareTalisman(int after)
  {
   var st=Player.State;int n=TalismanCatalog.Count;
   for(int step=1;step<=n;step++){int c=((after<0?-1:after)+step+n)%n;if(st.OwnsTalisman((Talisman)c)&&!st.IsTalismanEquipped((Talisman)c))return c;}
   return -1;
  }
  void EquipTalismanFromInventory(int k)
  {
   int next=NextSpareTalisman(Player.State.TalismanInSlot(k));
   if(Codice.Multiplayer.LanSession.Client){Codice.Multiplayer.LanSession.PendingTalismanSlot=k;Codice.Multiplayer.LanSession.PendingTalisman=next;}   /* the host owns the state; it echoes back */
   Player.State.EquipTalisman(k,next);GameAudio.Play(SoundCue.Orb);
  }
  void EquipSelected()
  {
   int i=inventorySelection;
   if(i<2){if(Codice.Multiplayer.LanSession.Client)Codice.Multiplayer.LanSession.PendingWeaponSlot=i;Player.State.SelectWeapon(i);}
   else if(i>=8&&i<12){var book=Player.GetComponent<RunSpellbook>();if(book&&i-8<book.Known.Count)book.Selected=i-8;}
   else if(i>=12)EquipTalismanFromInventory(i-12);
   RefreshInventory();
  }
  void RefreshInventory()
  {
   var s=Player.State;
   string title="",description="";bool canEquip=false;
   for(int i=0;i<InventorySlots;i++)
   {
    var e=InventoryEntry(i);var cell=inventoryIcons[i].transform.parent.gameObject;
    cell.SetActive(e.icon!=null);if(e.icon==null)continue;
    var face=cell.GetComponent<RelicButtonFace>();face.primary=i==inventorySelection;face.SetVerticesDirty();
    inventoryIcons[i].sprite=e.icon;inventoryIcons[i].color=e.tint;inventoryCaptions[i].text=(i==inventorySelection?"• ":"")+e.name;
    if(i==inventorySelection){title=e.name;description=e.detail;inventoryPreview.sprite=e.icon;inventoryPreview.color=e.tint;canEquip=e.equip;}
   }
   itemTitle.text=title;itemDescription.text=description;equipInventory.gameObject.SetActive(canEquip);
   // Button verb follows what it will do: weapons EQUIPAR, spells PREPARAR, talismans EQUIPAR / CAMBIAR / QUITAR.
   string verb="EQUIPAR";int sel=inventorySelection;
   if(sel>=8&&sel<12)verb="PREPARAR";
   else if(sel>=12){int worn=s.TalismanInSlot(sel-12);verb=worn<0?"EQUIPAR":NextSpareTalisman(worn)>=0?"CAMBIAR":"QUITAR";}
   var verbLabel=equipInventory.GetComponentInChildren<TMPro.TMP_Text>();if(verbLabel)verbLabel.text=verb;
   inventoryStatus.text=$"<color=#C3A273>ESTADO DEL PERSONAJE</color>\n\n{new[]{"Caballero","Tanque","Arquero","Mago","Shinobi","Assasin"}[(int)s.Class]}\n\nVida       {s.Health:0} / {s.MaxHealth:0}\nArmadura   {s.Armor:0} / {s.MaxArmor:0}\nManá       {s.Mana:0} / {s.MaxMana:0}\n\nORO        {s.Gold:N0}\n\n<color=#C3A273>MEJORAS DEL DESCENSO</color>\n";
   bool any=false;foreach(StatueBlessing b in System.Enum.GetValues(typeof(StatueBlessing)))if(s.HasBlessing(b)){any=true;inventoryStatus.text+=CharacterState.BlessingDescription(b)+"\n";}if(!any)inventoryStatus.text+="Sin pactos de estatua";
  }
 }
}
