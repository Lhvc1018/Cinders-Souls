from pathlib import Path
p=Path('Assets/02_Scripts/Expedition/ExpeditionLayout.cs');s=p.read_text(encoding='utf-8-sig').replace('else if(i==Boss&&floor==9)','else if(i==Boss&&(floor==9||floor==12))');p.write_text(s,encoding='utf-8')
