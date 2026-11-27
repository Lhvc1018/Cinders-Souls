using Codice.Characters;
using Codice.Mechanics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        RectTransform relicPanel;RelicAltar relicAltar;LootPickup pendingPassive;int selectedOffer=-1;
        TextMeshProUGUI relicExplanation,relicComparison;
        readonly Button[] relicChoices=new Button[3],relicReplace=new Button[3];
        readonly TextMeshProUGUI[] relicOfferTexts=new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] activeLabels=new TextMeshProUGUI[2];
        readonly Image[] activeCooldowns=new Image[2];
        public bool RelicSelectionVisible=>relicPanel&&relicPanel.gameObject.activeSelf;
        void BuildRelicMenu()
        {
            relicPanel=Panel("Relic choice",-710,-385,1420,770);Anchor(relicPanel,.5f,.5f);
            UIWidgets.Label(relicPanel,"Eyebrow",45,30,1200,35,18,ProceduralUISprites.Gold).text="RELICARIO  /  UN PACTO PARA ESTE DESCENSO";
            UIWidgets.Label(relicPanel,"Heading",42,84,1330,70,46).text="Elige qué conservar";
            UIWidgets.Label(relicPanel,"Rules",45,164,1330,40,22).text="Dos activas [F / G]. Tres pasivas siempre presentes. Solo puedes tomar una ofrenda.";
            for(int i=0;i<3;i++)
            {
                int index=i;var card=UIWidgets.Surface(relicPanel,"Relic offer "+i,45+i*450,235,430,265);
                relicOfferTexts[i]=UIWidgets.Label(card,"Description",22,22,386,170,23);
                relicChoices[i]=UIWidgets.Button(card,"ELEGIR",16,205,398,46,()=>SelectRelicOffer(index));
            }
            relicExplanation=UIWidgets.Label(relicPanel,"Selection help",45,530,1320,50,23,ProceduralUISprites.Gold);
            relicComparison=UIWidgets.Label(relicPanel,"Loadout",45,585,1320,46,21);
            for(int i=0;i<3;i++){int slot=i;relicReplace[i]=UIWidgets.Button(relicPanel,"",45+i*450,635,430,100,()=>ConfirmRelic(slot));}
            UIWidgets.Button(relicPanel,"VOLVER SIN ELEGIR",1000,738,375,30,CloseRelicMenu);
            relicPanel.gameObject.SetActive(false);
        }
        public void OpenRelicAltar(RelicAltar altar)
        {
            if(!altar||ShopVisible||ResultsVisible)return;
            pendingPassive=null;relicAltar=altar;selectedOffer=-1;relicPanel.gameObject.SetActive(true);SetPaused(true);
            for(int i=0;i<3;i++)
            {
                bool exists=i<altar.Offers.Length;relicChoices[i].transform.parent.gameObject.SetActive(exists);if(!exists)continue;
                var offer=altar.Offers[i];relicOfferTexts[i].text=$"<color=#C3A273><size=16>{(offer.active?"HABILIDAD ACTIVA":"BENDICIÓN PASIVA")}</size></color>\n{offer.Name}\n<size=20>{offer.Description}</size>";
            }
            relicExplanation.text="Selecciona una ofrenda para ver qué ranura ocupará.";
            relicComparison.text="Las pasivas duran hasta terminar la partida. El especial de arma sigue en [Q].";
            foreach(var button in relicReplace)button.gameObject.SetActive(false);
            relicChoices[0].Select();
        }
        public void OpenPassiveChoice(LootPickup pickup)
        {
            if(!pickup||Paused||ShopVisible||ResultsVisible)return;
            pendingPassive=pickup;relicAltar=null;selectedOffer=0;relicPanel.gameObject.SetActive(true);SetPaused(true);
            for(int i=0;i<3;i++){relicChoices[i].transform.parent.gameObject.SetActive(i==0);if(i==0)relicOfferTexts[i].text="BENDICIÓN ENCONTRADA\n"+UIWidgets.Passive(pickup.passive)+"\n"+RelicCatalog.PassiveDescription(pickup.passive);}
            SelectRelicOffer(0);
        }
        void SelectRelicOffer(int index)
        {
            if(!pendingPassive&&(!relicAltar||index<0||index>=relicAltar.Offers.Length))return;
            selectedOffer=index;var offer=pendingPassive?new RelicOffer{kind=(int)pendingPassive.passive}:relicAltar.Offers[index];var state=Player.State;
            bool needsSlot=offer.active||state.PassiveCount==3;
            relicExplanation.text=offer.Name+" — "+(needsSlot?"Elige qué reemplazar.":"Ocupará una ranura pasiva vacía.");
            relicComparison.text=offer.Description;
            for(int i=0;i<3;i++)
            {
                bool visible=needsSlot?(offer.active?i<2:true):i==0;relicReplace[i].gameObject.SetActive(visible);if(!visible)continue;
                relicReplace[i].GetComponentInChildren<TextMeshProUGUI>().fontSize=17;
                relicReplace[i].GetComponentInChildren<TextMeshProUGUI>().text=!needsSlot?"ACEPTAR BENDICIÓN":offer.active?$"[{(i==0?"F":"G")}] {RelicCatalog.Name(state.GetRelic(i))}":UIWidgets.Passive(state.GetPassive(i))+"\n"+RelicCatalog.PassiveDescription(state.GetPassive(i));
            }
            relicReplace[0].Select();
        }
        void ConfirmRelic(int slot)
        {
            if(pendingPassive)
            {
                var pickup=pendingPassive;SetPaused(false);
                if(pickup.TryCollect(Player,slot)){pendingPassive=null;relicPanel.gameObject.SetActive(false);return;}
                relicPanel.gameObject.SetActive(true);SetPaused(true);relicExplanation.text="No puedes equipar una pasiva duplicada.";return;
            }
            if(!relicAltar||selectedOffer<0)return;
            if(!relicAltar.Claim(Player,selectedOffer,slot)){relicExplanation.text="No se pudo equipar. Esa bendición ya está equipada.";return;}
            Announce("Pacto sellado · "+relicAltar.Offers[selectedOffer].Name);CloseRelicMenu();
        }
        public void CloseRelicMenu(){if(relicPanel)relicPanel.gameObject.SetActive(false);relicAltar=null;pendingPassive=null;SetPaused(false);}
        void RefreshRelicHud()
        {
            for(int i=0;i<2;i++)
            {
                var kind=Player.State.GetRelic(i);float wait=Player.State.RelicWait(i);
                activeCooldowns[i].fillAmount=wait/RelicCatalog.Cooldown(kind);
                activeLabels[i].text=$"<color=#C3A273>[{(i==0?"F":"G")}]</color>\n{RelicCatalog.ShortName(kind)}\n<size=17>{(wait>0?$"{wait:0.0} s":"LISTA")}</size>";
            }
            for(int i=0;i<3;i++)
            {
                bool exists=i<Player.State.PassiveCount;
                passiveLabels[i].text=exists?UIWidgets.Passive(Player.State.GetPassive(i)):"Pasiva vacía";
                passiveLabels[i].color=exists?new Color(.88f,.82f,.68f):new Color(.52f,.51f,.46f);
                passiveFrames[i].color=exists?new Color(.28f,.25f,.17f,.85f):new Color(.08f,.08f,.065f,.7f);
            }
        }
    }
}

