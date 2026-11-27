from pathlib import Path
p=Path('Assets/02_Scripts/UI/RunHUD.Inventory.cs');s=p.read_text(encoding='utf-8');s=s.replace('inventoryIcons[i].sprite=icon;', '''if(i==2)icon=Resources.Load<Sprite>("CindersIcons/EstusBottle")??icon;
    else if(i==3)icon=Resources.Load<Sprite>("CindersIcons/RelicToken")??icon;
    else if(i==4)icon=Resources.Load<Sprite>("CindersIcons/ManaShard")??icon;
    else if(i>=5&&i-5<s.PassiveCount)icon=Resources.Load<Sprite>("CindersIcons/Passive"+(int)s.GetPassive(i-5))??icon;
    if(i>=2)tint=Color.white;
    inventoryIcons[i].sprite=icon;''');s=s.replace('{s.Class}', '{new[]{"Caballero","Tanque","Arquero","Mago","Shinobi"}[(int)s.Class]}');p.write_text(s,encoding='utf-8')
