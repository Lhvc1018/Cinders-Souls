from pathlib import Path
r=Path('D:/67/Last67Seconds-Maps')
def edit(p,a,b):
 f=r/p;s=f.read_text(encoding='utf-8-sig');assert a in s,p;f.write_text(s.replace(a,b),encoding='utf-8')
edit('Assets/02_Scripts/UI/MainMenuUI.Cheats.cs','{if(code!="Jam2026")return false;cheatsUnlocked=true;return true;}','{if(Codice.Mechanics.SessionCheats.Execute(code))return true;if(code!="Jam2026")return false;cheatsUnlocked=true;return true;}')
edit('Assets/02_Scripts/UI/MainMenuUI.Cheats.cs','Introduce un código para desbloquear las rondas de prueba durante esta sesión.','Good!: modo dios y daño letal. Macpato: oro infinito. Repite el código para desactivarlo.')
edit('Assets/02_Scripts/UI/MainMenuUI.Cheats.cs','"Selector desbloqueado. Elige ronda y luego inicia el juego."','"Código aceptado. " + Codice.Mechanics.SessionCheats.Status')
edit('Assets/02_Scripts/Characters/CharacterState.cs','public float Mana => mana;','public float Mana => Codice.Mechanics.SessionCheats.God ? MaxMana : mana;')
edit('Assets/02_Scripts/Characters/CharacterState.cs','if(!IsAlive||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount)||mana<amount)return false;','if(!IsAlive||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return false;\n            if(Codice.Mechanics.SessionCheats.God)return true;\n            if(mana<amount)return false;')
edit('Assets/02_Scripts/Characters/CharacterState.cs','public int Gold => gold;','public int Gold => Codice.Mechanics.SessionCheats.Rich ? int.MaxValue : gold;')
edit('Assets/02_Scripts/Characters/CharacterState.cs','armorDelay=ArmorDelay;','if(Codice.Mechanics.SessionCheats.God)return false;\n            armorDelay=ArmorDelay;')
edit('Assets/02_Scripts/Characters/CharacterState.cs','if (amount < 0 || amount > gold || !IsAlive) return false;','if (amount < 0 || !IsAlive) return false;\n            if(Codice.Mechanics.SessionCheats.Rich)return true;\n            if(amount>gold)return false;')
for p in ['Assets/02_Scripts/Enemies/BasicEnemy.cs','Assets/02_Scripts/Bosses/BossCharacter.cs']:
 edit(p,'public void ReceiveDamage(DamageInfo hit)\n        {','public void ReceiveDamage(DamageInfo hit)\n        {\n            if(Codice.Mechanics.SessionCheats.God && hit.source && hit.source.GetComponentInParent<PlayerCharacter>()){hit.amount=MaxHealth*10;hit.ignoreGuard=true;}')
edit('Assets/02_Scripts/Mechanics/RunDirector.cs','if(Codice.UI.RunNavigation.EndlessMode){','if(context.GetComponent<CombatTestRoom>()){transition=false;SetPhase(RunPhase.Combat);context.GetComponent<CombatTestRoom>().Begin(context);return;}\n            if(Codice.UI.RunNavigation.EndlessMode){')
edit('Assets/02_Scripts/UI/MainMenuUI.cs','Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;','if(Application.isPlaying){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}')
edit('Assets/02_Scripts/UI/MainMenuUI.cs','classShowcase.Initialize(portrait,"Class_Warrior");','if(Application.isPlaying)classShowcase.Initialize(portrait,"Class_Warrior");')
edit('Assets/02_Scripts/UI/MainMenuUI.cs','FadeIn(home);\n        }','if(Application.isPlaying){ApplyAuthoredLayout(root.transform);FadeIn(home);}\n        }')
edit('Assets/02_Scripts/UI/MainMenuUI.cs','static void FadeIn(RectTransform panel){','static void FadeIn(RectTransform panel){if(!Application.isPlaying)return;')
