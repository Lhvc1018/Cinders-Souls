from pathlib import Path
p=Path('Assets/02_Scripts/UI/RunHUD.Relics.cs');s=p.read_text(encoding='utf-8');s=s.replace('RectTransform relicPanel;RelicAltar relicAltar;', 'RectTransform relicPanel;RelicAltar relicAltar;LootPickup pendingPassive;')
s=s.replace('            relicAltar=altar;', '            pendingPassive=null;relicAltar=altar;')
s=s.replace('        void SelectRelicOffer(int index)', '''        public void OpenPassiveChoice(LootPickup pickup)
        {
            if(!pickup||Paused||ShopVisible||ResultsVisible)return;
            pendingPassive=pickup;relicAltar=null;selectedOffer=0;relicPanel.gameObject.SetActive(true);SetPaused(true);
            for(int i=0;i<3;i++){relicChoices[i].transform.parent.gameObject.SetActive(i==0);if(i==0)relicOfferTexts[i].text="BENDICIÓN ENCONTRADA\\n"+UIWidgets.Passive(pickup.passive)+"\\n"+RelicCatalog.PassiveDescription(pickup.passive);}
            SelectRelicOffer(0);
        }
        void SelectRelicOffer(int index)''')
s=s.replace('if(!relicAltar||index<0||index>=relicAltar.Offers.Length)return;','if(!pendingPassive&&(!relicAltar||index<0||index>=relicAltar.Offers.Length))return;')
s=s.replace('selectedOffer=index;var offer=relicAltar.Offers[index];', 'selectedOffer=index;var offer=pendingPassive?new RelicOffer{kind=(int)pendingPassive.passive}:relicAltar.Offers[index];')
s=s.replace('            if(!relicAltar||selectedOffer<0)return;', '''            if(pendingPassive)
            {
                var pickup=pendingPassive;SetPaused(false);
                if(pickup.TryCollect(Player,slot)){pendingPassive=null;relicPanel.gameObject.SetActive(false);return;}
                relicPanel.gameObject.SetActive(true);SetPaused(true);relicExplanation.text="No puedes equipar una pasiva duplicada.";return;
            }
            if(!relicAltar||selectedOffer<0)return;''')
s=s.replace('relicAltar=null;SetPaused(false);','relicAltar=null;pendingPassive=null;SetPaused(false);');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Mechanics/LootPickup.cs');s=p.read_text(encoding='utf-8-sig').replace('if (player) TryCollect(player);','if (player&&IsAmmo) TryCollect(player);').replace('            var keys = Keyboard.current;', '            if(kind==LootKind.Passive&&!Codice.Multiplayer.LanSession.Active&&Codice.UI.RunHUD.Instance){Codice.UI.RunHUD.Instance.OpenPassiveChoice(this);return;}\n            var keys = Keyboard.current;');p.write_text(s,encoding='utf-8')
