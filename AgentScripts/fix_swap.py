from pathlib import Path
p=Path('Assets/02_Scripts/Characters/CharacterState.cs');s=p.read_text(encoding='utf-8-sig').replace('        public bool CanEquipPassive(','''        public WeaponInstance EquipIntoSlot(WeaponInstance weapon,int slot)
        {
            if(weapon==null||slot<0||slot>1)return weapon;
            var previous=weapons[slot];weapons[slot]=weapon;activeSlot=slot;Changed?.Invoke();return previous;
        }
        public bool CanEquipPassive(''');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Characters/WeaponPickup.cs');s=p.read_text(encoding='utf-8').replace('EquipImmediately(PlayerCharacter player)','EquipImmediately(PlayerCharacter player,int slot=-1)').replace('var previous = player.State.Equip(weapon);','var previous = slot<0?player.State.Equip(weapon):player.State.EquipIntoSlot(weapon,slot);');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.WeaponChoice.cs');s=p.read_text(encoding='utf-8').replace('pickup.EquipImmediately(Player);','pickup.EquipImmediately(Player,slot);');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Presentation/ActorPresentation.cs');s=p.read_text(encoding='utf-8').replace('if(player&&player.GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShieldActive==true){Play("BlockIdle");return;}', 'if(player&&(player.GetComponent<PlayerCombat>().IsBlocking||player.GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShieldActive==true)){Play("BlockIdle");return;}');p.write_text(s,encoding='utf-8')
