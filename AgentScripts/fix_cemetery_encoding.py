from pathlib import Path
p=Path('Assets/02_Scripts/Expedition/ExpeditionFloor.Cemetery.cs');s=p.read_text(encoding='utf-8').encode('cp1252').decode('utf-8');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Expedition/ExpeditionFloor.cs');s=p.read_text(encoding='utf-8').replace('if(surface)surface.RemoveData();if(profile)Destroy(profile);','if(surface)surface.RemoveData();if(profile)Destroy(profile);if(cemeterySky)Destroy(cemeterySky);');p.write_text(s,encoding='utf-8')
