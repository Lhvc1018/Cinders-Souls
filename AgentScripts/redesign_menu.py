from pathlib import Path
p=Path(r'D:/67/Last67Seconds-Maps/Assets/02_Scripts/UI/MainMenuUI.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('new Color(.88f,.71f,.43f), Cream=new Color(.95f,.92f,.85f), Muted=new Color(.57f,.61f,.66f)','new Color(.74f,.65f,.44f), Cream=new Color(.91f,.88f,.77f), Muted=new Color(.65f,.63f,.55f)')
s=s.replace('new Color(.72f,.72f,.72f,1)','new Color(.32f,.32f,.29f,1)')
a=s.index('            Label(home,"Edition"');b=s.index('            Label(options,"Eyebrow"',a)
s=s[:a]+'''            var sigil=UIWidgets.Rect(home,"Ashen vow",740,22,200,200).gameObject.AddComponent<AshenSigil>();sigil.color=new Color(.65f,.53f,.32f,.66f);sigil.raycastTarget=false;
            var edition=Label(home,"Edition",440,225,800,35,17,Gold,"U N   R E I N O   B A J O   C E N I Z A S");edition.alignment=TextAlignmentOptions.Center;
            var title=Label(home,"Title",260,280,1160,120,88,Cream,"CINDERS SOULS");title.characterSpacing=8;title.alignment=TextAlignmentOptions.Center;
            var titleEffect=title.gameObject.AddComponent<Coffee.UIEffects.UIEffect>();titleEffect.shadowMode=Coffee.UIEffects.ShadowMode.Shadow;titleEffect.shadowDistance=new Vector2(1,-3);titleEffect.shadowColor=Color.black;
            Line(home,510,433,660,new Color(.5f,.43f,.29f,.6f));
            var motto=Label(home,"Subtitle",390,451,900,45,24,Muted,"Toda llama deja una deuda.");motto.alignment=TextAlignmentOptions.Center;
            var first=Button(home,"INICIAR DESCENSO",560,545,560,57,()=>ShowClasses(true),true);
            Button(home,"COOPERATIVO LAN",560,615,560,51,Codice.Multiplayer.LanSession.Open);
            Button(home,"OPCIONES",560,679,560,51,()=>ShowOptions(true));
            Button(home,"SALIR",560,743,560,51,RunNavigation.Quit);
            Button(home,"TRUCOS",1420,838,190,40,()=>ShowCheats(true));BuildCheats(root.transform);
            var foot=Label(home,"Footer",350,851,980,30,17,Muted,"↑ ↓  Seleccionar      Enter  Confirmar      Esc  Volver");foot.alignment=TextAlignmentOptions.Center;
''' +s[b:]
s=s.replace('"A tu manera."','"Ajustes"')
s=s.replace('"Class reading panel",1150,270,470,530,new Color(.025f,.025f,.03f,.92f)','"Class reading panel",80,270,450,500,new Color(.035f,.034f,.028f,.86f)')
s=s.replace('"Class panel spine",1169,302,2,365','"Class panel spine",99,302,1,365')
s=s.replace('"Class panel seal",1162,280,16,16','"Class panel seal",94,280,10,10')
s=s.replace('"Title",80,135,1110,90,64,Cream,"Elige tu legado."','"Title",80,116,1480,80,48,Cream,"Elige tu legado"')
s=s.replace('"Hint",80,230,1520,45,21','"Hint",80,211,1520,45,21')
s=s.replace('"Retrato de clase",520,270,620,540','"Retrato de clase",540,260,620,560')
s=s.replace('"Legacy number",1190,278,390,25','"Legacy number",115,290,390,28')
s=s.replace('"Class name",1190,310,390,65,34','"Class name",115,336,390,65,32')
s=s.replace('"Class lore",1190,365,390,40','"Class lore",115,403,390,40')
s=s.replace('"Class details",1190,415,390,285,24','"Class details",115,468,375,280,23')
s=s.replace('Line(classes,1190,395,390','Line(classes,115,451,375')
s=s.replace('classNames[i],80,310+i*90,400,72','classNames[i],1200,285+i*90,400,64')
s=s.replace('"DESCENDER",1190,715,390,65','"DESCENDER",1200,810,400,55')
s=s.replace('"Controls",385,827,1200,50,18','"Controls",385,831,780,35,17')
s=s.replace('"Selecciona una clase   •   Confirma para comenzar   •   Esc para volver"','"Selecciona un legado. Confirma para comenzar."')
s=s.replace('"CindersFonts/UncialAntiqua"','"CindersMenu/Cinzel"')
s=s.replace('var label=Label(rect,"Label",66,0,w-112,h,w<350?20:24','var label=Label(rect,"Label",48,0,w-76,h,w<350?18:22')
s=s.replace('label.fontStyle=FontStyles.Bold','label.fontStyle=FontStyles.Normal')
p.write_text(s,encoding='utf-8')
