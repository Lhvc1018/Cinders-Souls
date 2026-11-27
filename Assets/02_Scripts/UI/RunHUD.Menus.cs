using TMPro;
using Text = TMPro.TextMeshProUGUI;
using System.Linq;
using Codice.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        RectTransform shopPanel,pausePanel,defeatPanel,victoryPanel,modalShade;
        readonly Button[] buyButtons=new Button[4],replaceButtons=new Button[3];
        readonly Text[] offerTexts=new Text[4];
        readonly Image[] offerIcons=new Image[4];
        Text shopGold,shopMessage,pauseClass,victoryStats,inventoryItems,victoryHeading,victoryDescription;
        Merchant merchant;
        int replacement=-1;
        float previousTimeScale=1;
        GameObject selectedModal;
        void BuildMenus()
        {
            modalShade=UIWidgets.Image(transform,"Modal shade",0,0,1920,1080,new Color(.025f,.025f,.045f,.82f)).rectTransform;Stretch(modalShade);modalShade.GetComponent<Image>().raycastTarget=true;
            shopPanel=Panel("Mercader Errante",-560,-340,1120,680);Anchor(shopPanel,.5f,.5f);
            UIWidgets.Label(shopPanel,"Heading",32,24,800,45,32,ProceduralUISprites.Gold).text="NEREO · TASADOR DE NOMBRES";
            shopGold=UIWidgets.Label(shopPanel,"Balance",820,29,270,35,23,ProceduralUISprites.Gold);
            UIWidgets.Label(shopPanel,"Subheading",33,75,990,30,18).text="Un respiro entre cenizas. Tu elección puede cambiar el siguiente encuentro.";
            for(int i=0;i<4;i++)
            {
                int index=i;var card=UIWidgets.Image(shopPanel,"Offer "+i,30+i*265,126,250,300,ProceduralUISprites.Ink,"frame");
                offerTexts[i]=UIWidgets.Label(card.transform,"Description",18,82,214,150,21);
                offerIcons[i]=UIWidgets.Image(card.transform,"Offer icon",100,16,50,50,ProceduralUISprites.Gold,"hex");
                buyButtons[i]=UIWidgets.Button(card.transform,"COMPRAR ["+(i+1)+"]",15,244,220,45,()=>Purchase(index));
            }
            UIWidgets.Label(shopPanel,"Replace label",33,450,1050,30,17).text="REEMPLAZAR PASIVA (si ya tienes 3)  •  Shift+3 / Ctrl+3 / Alt+3";
            for(int i=0;i<3;i++){int slot=i;replaceButtons[i]=UIWidgets.Button(shopPanel,"Ranura "+(i+1),30+i*353,492,340,45,()=>{replacement=slot;});}
            shopMessage=UIWidgets.Label(shopPanel,"Transaction result",32,555,620,88,18,ProceduralUISprites.Gold);
            UIWidgets.Button(shopPanel,"CERRAR TIENDA [E]",700,595,390,48,()=>{if(merchant){merchant.CloseShop();}});
            BuildInventory();
            defeatPanel=Panel("Defeat",-350,-190,700,380);Anchor(defeatPanel,.5f,.5f);
            UIWidgets.Label(defeatPanel,"Heading",35,38,630,60,32,ProceduralUISprites.Crimson).text="LAS CENIZAS TE RECLAMAN";
            UIWidgets.Label(defeatPanel,"Description",35,125,630,85,22).text="Tu viaje termina aquí.\nDesciende de nuevo o regresa al menú principal.";
            UIWidgets.Button(defeatPanel,Codice.Multiplayer.LanSession.Client?"ESPERANDO AL ANFITRIÓN":"VOLVER A JUGAR",35,230,630,50,()=>{SetPaused(false);if(director)director.RestartLevel();}).interactable=!Codice.Multiplayer.LanSession.Client;
            UIWidgets.Button(defeatPanel,"IR AL MENÚ PRINCIPAL",35,300,630,50,RunNavigation.MainMenu);
            victoryPanel=Panel("Victory",-350,-250,700,500);Anchor(victoryPanel,.5f,.5f);
            (victoryHeading=UIWidgets.Label(victoryPanel,"Heading",35,32,630,80,32,ProceduralUISprites.Gold)).text=Context.Expedition?"EL SELLO HA SIDO QUEBRADO":"EL CÓDICE HA SIDO CONQUISTADO";
            (victoryDescription=UIWidgets.Label(victoryPanel,"Description",35,115,630,70,21)).text=Context.Expedition?"El inquisidor, el ángel y el guardián han caído.":"El Ork Berserker ha caído en la arena final.";
            victoryStats=UIWidgets.Label(victoryPanel,"Final statistics",35,190,630,125,21,Color.white);
            UIWidgets.Button(victoryPanel,Codice.Multiplayer.LanSession.Client?"ESPERANDO AL ANFITRIÓN":"VOLVER A JUGAR",35,320,630,55,()=>{if(director)director.RestartRun();}).interactable=!Codice.Multiplayer.LanSession.Client;
            UIWidgets.Button(victoryPanel,"IR AL MENÚ PRINCIPAL",35,395,630,55,RunNavigation.MainMenu);
            victoryPanel.gameObject.SetActive(false);
            modalShade.gameObject.SetActive(false);shopPanel.gameObject.SetActive(false);pausePanel.gameObject.SetActive(false);defeatPanel.gameObject.SetActive(false);
            BuildRelicMenu();
        }
        public void SetPaused(bool paused)
        {
            if(Paused==paused)return;
            // A click on Resume must not also fire a weapon in the same frame.
            escapeFrame=Time.frameCount;
            Paused=paused;
            if(!paused&&relicPanel)relicPanel.gameObject.SetActive(false);
            if(!paused&&weaponChoicePanel)weaponChoicePanel.gameObject.SetActive(false);
            if(paused){previousTimeScale=Time.timeScale;if(!Codice.Multiplayer.LanSession.Active)Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            else{Time.timeScale=previousTimeScale;if(Player&&Player.State.IsAlive&&!Player.ControlsBlocked&&!ResultsVisible&&!RunNavigation.Loading){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}}
        }
        public bool Purchase(int index)
        {
            bool success=merchant&&merchant.TryPurchase(Player,index,replacement);
            shopMessage.text=Codice.Multiplayer.LanSession.Client?"Solicitud enviada al anfitrión.":success?"Compra realizada.":"No se pudo comprar: revisa oro, salud, stock o reemplazo.";
            return success;
        }
        void UpdateMenus()
        {
            bool dead=!Player.State.IsAlive&&(!Codice.Multiplayer.LanSession.Active||director&&director.Phase==Codice.Mechanics.RunPhase.Defeated);
            bool won=director&&director.Phase==Codice.Mechanics.RunPhase.Victory&&!dead;
            victoryPanel.gameObject.SetActive(won&&!Paused);
            if(won&&!string.IsNullOrEmpty(director.EndingTitle)){victoryHeading.text=director.EndingTitle;victoryDescription.text=director.EndingText;}
            var current=Context.dungeon?Context.dungeon.merchant:null;
            bool shopping=current&&current.Customer==Player;
            if(merchant!=current){merchant=current;replacement=-1;}
            modalShade.gameObject.SetActive(Paused||dead||shopping||won);shopPanel.gameObject.SetActive(shopping&&!dead&&!Paused);pausePanel.gameObject.SetActive(Paused&&!dead&&!RelicSelectionVisible&&!WeaponSelectionVisible);defeatPanel.gameObject.SetActive(dead);
            if(dead||won){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(won&&victoryStats)victoryStats.text=director.Expedition?$"SALAS EXPLORADAS   {director.Expedition.ClearedRooms} / {director.Expedition.RoomCount}\nENEMIGOS DERROTADOS  {director.TotalKills}\nORO REUNIDO  {Player.State.Gold:N0}\nTIEMPO TOTAL  {UIWidgets.Clock(director.Elapsed)}":$"RONDAS COMPLETADAS   {director.Level} / {Codice.Mechanics.RoundRules.FinalRound}\nENEMIGOS DERROTADOS  {director.TotalKills}\nORO REUNIDO          {Player.State.Gold:N0}\nTIEMPO TOTAL         {UIWidgets.Clock(director.Elapsed)}";
            var activeModal=dead?defeatPanel.gameObject:WeaponSelectionVisible?weaponChoicePanel.gameObject:RelicSelectionVisible?relicPanel.gameObject:Paused?pausePanel.gameObject:won?victoryPanel.gameObject:shopping?shopPanel.gameObject:null;
            if(activeModal!=selectedModal)
            {
                selectedModal=activeModal;
                if(activeModal)activeModal.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.interactable)?.Select();
                else if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
            if(Paused)RefreshInventory();
            if(!shopping)return;
            shopGold.text=$"• {Player.State.Gold} ORO";
            for(int i=0;i<4;i++)
            {
                var offer=merchant.GetOffer(i);if(offer==null){buyButtons[i].interactable=false;continue;}
                offerIcons[i].sprite=offer.kind==OfferKind.Talisman&&offer.talisman>=0?TalismanIcons.Get((Talisman)offer.talisman):offer.kind==OfferKind.Weapon?ProceduralUISprites.Weapon(new WeaponInstance(offer.weapon,offer.tier)):ProceduralUISprites.Shape(offer.kind==OfferKind.Passive?"hex":"diamond");
                string title=offer.kind==OfferKind.Weapon?UIWidgets.WeaponName(offer.weapon)+"  T"+offer.tier:offer.kind==OfferKind.Passive?UIWidgets.Passive(offer.passive):offer.kind==OfferKind.Talisman&&offer.talisman>=0?TalismanCatalog.All[offer.talisman].name:offer.kind==OfferKind.Mana?"Esencia de maná":offer.kind==OfferKind.Flask?"Recarga de frasco":offer.kind==OfferKind.Statue?CharacterState.BlessingName(offer.blessing):"Poción de vida";
                string details=offer.kind==OfferKind.Weapon?$"{new WeaponInstance(offer.weapon,offer.tier).Damage:0} DAÑO\n{UIWidgets.Special(offer.weapon)}":offer.kind==OfferKind.Passive?"Sello pasivo\nHasta 3 equipados":offer.kind==OfferKind.Talisman&&offer.talisman>=0?TalismanCatalog.All[offer.talisman].kind+" · "+TalismanCatalog.Describe((Talisman)offer.talisman).Split('\n')[0]:offer.kind==OfferKind.Mana?"Recupera 40 maná":offer.kind==OfferKind.Flask?"Repone las 3 cargas":offer.kind==OfferKind.Statue?CharacterState.BlessingDescription(offer.blessing):"Recupera +50% HP";
                offerTexts[i].text=title+"\n<size=17>"+details+"</size>\n<color=#F5D76E>"+(offer.sold?"VENDIDO":offer.price+" ORO")+"</color>";
                offerTexts[i].transform.parent.GetComponent<Image>().color=offer.kind==OfferKind.Weapon?ProceduralUISprites.Tier(offer.tier):ProceduralUISprites.Cyan;
                buyButtons[i].interactable=!offer.sold&&Player.State.Gold>=offer.price&&(offer.kind!=OfferKind.Healing||Player.State.Health<Player.State.MaxHealth)&&(offer.kind!=OfferKind.Passive||Player.State.CanEquipPassive(offer.passive,replacement))&&(offer.kind!=OfferKind.Talisman||offer.talisman>=0&&!Player.State.OwnsTalisman((Talisman)offer.talisman));
            }
            for(int i=0;i<3;i++){replaceButtons[i].GetComponentInChildren<Text>().text=(replacement==i?"• ":"")+(i+1)+" / "+(i<Player.State.PassiveCount?UIWidgets.Passive(Player.State.GetPassive(i)):"Vacío");}
        }
    }
}

