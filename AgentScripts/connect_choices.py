from pathlib import Path
p=Path('Assets/02_Scripts/Characters/WeaponPickup.cs');s=p.read_text(encoding='utf-8-sig').replace('            var previous = player.State.Equip(weapon);', '''            if(!Codice.Multiplayer.LanSession.Active&&Codice.UI.RunHUD.Instance){Codice.UI.RunHUD.Instance.OpenWeaponChoice(this);return;}
            EquipImmediately(player);
        }
        public void EquipImmediately(PlayerCharacter player)
        {
            if(weapon==null||!player||!player.State.IsAlive)return;
            var previous = player.State.Equip(weapon);''');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.Menus.cs');s=p.read_text(encoding='utf-8').replace('if(!paused&&relicPanel)relicPanel.gameObject.SetActive(false);','if(!paused&&relicPanel)relicPanel.gameObject.SetActive(false);\n            if(!paused&&weaponChoicePanel)weaponChoicePanel.gameObject.SetActive(false);').replace('Paused&&!dead&&!RelicSelectionVisible','Paused&&!dead&&!RelicSelectionVisible&&!WeaponSelectionVisible').replace('dead?defeatPanel.gameObject:RelicSelectionVisible?', 'dead?defeatPanel.gameObject:WeaponSelectionVisible?weaponChoicePanel.gameObject:RelicSelectionVisible?');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.Relics.cs');s=p.read_text(encoding='utf-8-sig').replace('645,430,48','635,430,100');s=s.replace('relicReplace[i].GetComponentInChildren<TextMeshProUGUI>().text=!needsSlot?', 'relicReplace[i].GetComponentInChildren<TextMeshProUGUI>().fontSize=17;\n                relicReplace[i].GetComponentInChildren<TextMeshProUGUI>().text=!needsSlot?').replace('UIWidgets.Passive(state.GetPassive(i));','UIWidgets.Passive(state.GetPassive(i))+"\\n"+RelicCatalog.PassiveDescription(state.GetPassive(i));');p.write_text(s,encoding='utf-8')
