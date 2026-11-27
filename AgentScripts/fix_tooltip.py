from pathlib import Path
p=Path('Assets/02_Scripts/UI/RunHUD.World.cs');s=p.read_text(encoding='utf-8-sig').replace('bool compare=focus is WeaponPickup;', 'bool compare=focus is WeaponPickup candidate&&candidate.weapon!=null;');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Presentation/ActorPresentation.cs');s=p.read_text(encoding='utf-8').replace('if(player&&player.IsDashing)state="Dash";', 'if(player&&player.IsDashing){Play("Dash",.55f,true);return;}');p.write_text(s,encoding='utf-8')
