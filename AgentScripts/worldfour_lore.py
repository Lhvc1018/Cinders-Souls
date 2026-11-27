from pathlib import Path
p=Path('Assets/02_Scripts/Expedition/DestinyEncounter.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('readonly string[] dialogue = {','string[] dialogue = {');s=s.replace('bool complete;','bool complete;\n        string orbName="MEMORIA";int world=2;');s=s.replace('void Start()\n        {','''void Start()
        {
            int floor=GetComponentInParent<ExpeditionFloor>().FloorNumber;
            world=floor/3+1;
            if(floor==6){orbName="VOLUNTAD";dialogue=new[]{
                "La Lanza llamaba obediencia a este silencio. Has recuperado el Orbe de la Voluntad: el derecho a negarse.",
                "Nereo vendió los nombres de quienes se negaron. Yo los entregué a la campana. Ninguno de los dos conserva el suyo.",
                "La Memoria recuerda. La Voluntad elige. Falta el Duelo, porque nadie puede volver a vivir mientras sus muertos sigan trabajando.",
                "Busca al nigromante en los Campos del Último Suspiro. No resucita cuerpos: retiene despedidas. Rompe su coro."};}
            else if(floor==9){orbName="DUELO";dialogue=new[]{
                "El último muerto ha pronunciado su nombre. Este es el Orbe del Duelo. No devuelve a nadie: permite dejarlos partir.",
                "Memoria, Voluntad y Duelo. Los llamaron destino para convencernos de que no podíamos elegir. Fueron fabricados.",
                "Bajo estas tumbas está la Fundición de los Juramentos. Sus hornos devoran nombres; sus campanas devuelven soldados.",
                "Nereo conserva el registro que falta. Cruza los hornos, lee el archivo y alcanza la última campana. Esta vez, no obedezcas su voz."};}
''');s=s.replace('orb.name="Orbe de la Memoria";','orb.name="Orbe de "+orbName;').replace('text.text="ORBE DE LA MEMORIA\\n[E] Recuperar";','text.text="ORBE DE "+orbName+"\\n[E] Recuperar";').replace('text.text="[E] Cruzar al Mundo 2"','text.text="[E] Cruzar al Mundo "+world');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Characters/Merchant.cs');s=p.read_text(encoding='utf-8-sig').replace('customer = player; player.BeginShop(this);','customer = player; player.BeginShop(this);\n            Codice.UI.RunHUD.Instance?.Announce(Codice.Mechanics.RunDirector.Instance&&Codice.Mechanics.RunDirector.Instance.FloorNumber>=10?"NEREO · Yo vendí sus nombres. Tú puedes devolvérselos.":"NEREO · Cobro oro. Ya pagué demasiado con nombres.");');p.write_text(s,encoding='utf-8')
for fn in ['Assets/02_Scripts/UI/RunHUD.Menus.cs','Assets/02_Scripts/UI/RunHUD.World.cs']:
 p=Path(fn);s=p.read_text(encoding='utf-8-sig').replace('EL MERCADER ERRANTE','NEREO · TASADOR DE NOMBRES').replace('MERCADER ERRANTE','NEREO · TASADOR DE NOMBRES');p.write_text(s,encoding='utf-8')
