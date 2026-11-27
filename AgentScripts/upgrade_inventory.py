from pathlib import Path
p=Path('Assets/02_Scripts/UI/RunHUD.cs');s=p.read_text(encoding='utf-8');s=s.replace('keys.escapeKey.wasPressedThisFrame&&Player.State.IsAlive&&!ShopVisible','(keys.escapeKey.wasPressedThisFrame||keys.iKey.wasPressedThisFrame)&&Player.State.IsAlive&&!ShopVisible&&!ResultsVisible');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.Menus.cs');s=p.read_text(encoding='utf-8');s=s.replace('Text shopGold,shopMessage,pauseClass,victoryStats;', 'Text shopGold,shopMessage,pauseClass,victoryStats,inventoryItems;')
s=s.replace('UIWidgets.Label(pausePanel,"Left guidance",42,310,320,100,22).text="Las reliquias definen tu camino.\\nCambia tu elección en los altares.";', 'inventoryItems=UIWidgets.Label(pausePanel,"Inventory items",42,275,335,210,19);')
s=s.replace('REANUDAR [Escape]', 'REANUDAR [I / Escape]')
s=s.replace('if(!shopping)return;', '''if(Paused)
            {
                var state=Player.State;
                inventoryItems.text=$"<color=#C3A273>{state.Gold:N0} ORO</color>\\nFrasco de ceniza  {state.EstusCharges}/{CharacterState.EstusCapacity}\\nCura 40% de vida por carga\\n\\n";
                for(int slot=0;slot<2;slot++){var w=state.GetWeapon(slot);inventoryItems.text+=w==null?"— Sin arma\\n":$"{(slot==state.ActiveSlot?"• ":"")}{UIWidgets.WeaponName(w.kind)}  T{w.tier}\\n{w.Damage:0} daño\\n";}
            }
            if(!shopping)return;''')
p.write_text(s,encoding='utf-8')
