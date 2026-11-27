from pathlib import Path
root=Path("D:/67/Last67Seconds-Maps")
p=root/"Assets/02_Scripts/UI/RunHUD.cs"
s=p.read_text(encoding="utf-8-sig").replace("CUSTODIO DE LAS DOS HOJAS","DUENDE MUTANTE").replace("LANCERO DE CENIZA","LANZA DEL INFIERNO").replace("GUARDIÁN DE OBSIDIANA","GURDI DEL ABISMO")
p.write_text(s,encoding="utf-8-sig")
p=root/"Assets/02_Scripts/Presentation/ActorPresentation.cs"
s=p.read_text(encoding="utf-8-sig").replace("if(enemy&&enemy.miniboss){animator.enabled=false;corpse.AddComponent<CorpseCollapse>();}","if((enemy&&enemy.miniboss)||(boss&&boss.IsWarden)){animator.enabled=false;corpse.AddComponent<CorpseCollapse>();}")
p.write_text(s,encoding="utf-8-sig")
