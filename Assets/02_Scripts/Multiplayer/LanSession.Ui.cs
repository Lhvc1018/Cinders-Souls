using System.Linq;
using System.Net;
using System.Net.Sockets;
using Codice.Characters;
using Codice.Mechanics;
using UnityEngine;
namespace Codice.Multiplayer
{
    // Cooperative interface in the game's palette: charcoal panels, ivory text, aged gold and blood red.
    public sealed partial class LanSession
    {
        static readonly Color Charcoal=new Color(.055f,.05f,.055f,.96f),Panel=new Color(.11f,.10f,.105f,1),Ivory=new Color(.91f,.87f,.78f),Gold=new Color(.80f,.62f,.32f),Blood=new Color(.56f,.11f,.13f),Muted=new Color(.58f,.55f,.5f);
        static readonly string[] ClassNames={"Caballero","Tanque","Arquero","Mago","Shinobi"};
        static readonly Color[] ClassTints={new Color(.75f,.72f,.66f),new Color(.55f,.6f,.68f),new Color(.5f,.68f,.42f),new Color(.5f,.58f,.9f),new Color(.72f,.3f,.34f),new Color(.82f,.6f,.3f)};
        GUIStyle uiTitle,uiHeading,uiText,uiMuted,uiButton,uiPrimary,uiField,uiChip,uiChipOn;Texture2D pixel;
        Texture2D Solid(Color c){var t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();t.hideFlags=HideFlags.HideAndDontSave;return t;}
        void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,pixel);GUI.color=old;}
        void Frame(Rect r,Color c,float w=2){Fill(new Rect(r.x,r.y,r.width,w),c);Fill(new Rect(r.x,r.yMax-w,r.width,w),c);Fill(new Rect(r.x,r.y,w,r.height),c);Fill(new Rect(r.xMax-w,r.y,w,r.height),c);}
        void BuildStyles()
        {
            if(uiTitle!=null&&pixel)return;pixel=Solid(Color.white);
            GUIStyle Text(int size,Color color,FontStyle style=FontStyle.Normal,TextAnchor anchor=TextAnchor.MiddleLeft){var s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=style,alignment=anchor,wordWrap=true,richText=true};s.normal.textColor=color;return s;}
            GUIStyle Button(Color back,Color hover,Color text,int size){var s=new GUIStyle(GUI.skin.button){fontSize=size,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};s.normal.background=Solid(back);s.hover.background=Solid(hover);s.active.background=Solid(hover*1.15f);s.focused.background=s.normal.background;s.normal.textColor=s.hover.textColor=s.active.textColor=s.focused.textColor=text;s.border=new RectOffset(0,0,0,0);return s;}
            uiTitle=Text(34,Gold,FontStyle.Bold,TextAnchor.MiddleCenter);uiHeading=Text(17,Gold,FontStyle.Bold);uiText=Text(19,Ivory);uiMuted=Text(15,Muted);
            uiButton=Button(new Color(.17f,.16f,.16f),new Color(.26f,.23f,.2f),Ivory,18);uiPrimary=Button(Blood,new Color(.72f,.16f,.17f),Ivory,20);
            uiChip=Button(new Color(.14f,.13f,.135f),new Color(.22f,.2f,.19f),Muted,16);uiChipOn=Button(new Color(.3f,.23f,.12f),new Color(.36f,.28f,.14f),Ivory,16);
            uiField=new GUIStyle(GUI.skin.textField){fontSize=20,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,6,6)};uiField.normal.background=uiField.focused.background=uiField.hover.background=Solid(new Color(.03f,.03f,.035f));uiField.normal.textColor=uiField.focused.textColor=uiField.hover.textColor=Ivory;
        }
        void OnGUI()
        {
            BuildStyles();GUI.depth=-20;
            if(started&&context){DrawParty();if(floorHeld)GUI.Label(new Rect(0,Screen.height*.32f,Screen.width,60),"Esperando a que el grupo cargue el piso…",uiTitle);return;}
            float scale=Mathf.Min(Screen.width/1100f,Screen.height/760f);var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1000*scale)/2,(Screen.height-700*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            Fill(new Rect(-2000,-2000,5000,5000),new Color(0,0,0,.72f));
            var box=new Rect(0,0,1000,700);Fill(box,Charcoal);Frame(box,new Color(Gold.r,Gold.g,Gold.b,.55f));Fill(new Rect(40,92,920,1),new Color(Gold.r,Gold.g,Gold.b,.35f));
            GUI.Label(new Rect(0,22,1000,50),"PACTO DE CENIZA",uiTitle);
            GUI.Label(new Rect(0,62,1000,26),"Cooperativo · hasta 4 peregrinos",new GUIStyle(uiMuted){alignment=TextAnchor.MiddleCenter});
            bool lobby=host||client||connecting;
            if(!lobby)
            {
                GUI.Label(new Rect(60,112,400,24),"TU NOMBRE",uiHeading);nickname=GUI.TextField(new Rect(60,140,400,42),nickname,20,uiField);
                GUI.Label(new Rect(60,200,400,24),"CLASE",uiHeading);
                for(int i=0;i<ClassNames.Length;i++){var r=new Rect(60+i%3*136,228+i/3*50,128,42);if(GUI.Button(r,ClassNames[i],i==chosenClass?uiChipOn:uiChip))chosenClass=i;Fill(new Rect(r.x,r.yMax-3,r.width,3),i==chosenClass?ClassTints[i]:new Color(0,0,0,0));}
                GUI.Label(new Rect(60,346,400,24),"CONEXIÓN",uiHeading);
                if(GUI.Button(new Rect(60,374,196,40),"Por internet (código)",useFusion?uiChipOn:uiChip)){if(!useFusion)address="";useFusion=true;}
                if(GUI.Button(new Rect(264,374,196,40),"Misma red (código)",!useFusion?uiChipOn:uiChip)){if(useFusion)address="";useFusion=false;}
                GUI.Label(new Rect(60,420,400,44),useFusion?"Uno crea la partida y dicta su código de seis letras; el otro lo escribe y se une.":"Misma red wifi. Copia el código LAN del anfitrión.",uiMuted);
                var right=new Rect(540,112,400,352);Fill(right,Panel);
                GUI.Label(new Rect(564,126,360,24),"ABRIR UN PACTO",uiHeading);
                GUI.Label(new Rect(564,152,352,44),"Serás el anfitrión: tu partida decide enemigos, botín y portales.",uiMuted);
                if(GUI.Button(new Rect(564,204,352,52),"CREAR PARTIDA",uiPrimary))CreateHost();
                Fill(new Rect(564,276,352,1),new Color(1,1,1,.08f));
                GUI.Label(new Rect(564,288,360,24),useFusion?"CÓDIGO DE SALA (6 letras)":"CÓDIGO LAN",uiHeading);address=GUI.TextField(new Rect(564,316,352,42),address,40,uiField);
                if(GUI.Button(new Rect(564,368,352,48),"UNIRSE",uiButton))Join();
            }
            else
            {
                var left=new Rect(60,112,520,300);Fill(left,Panel);
                GUI.Label(new Rect(80,122,480,24),"PEREGRINOS · "+members.Length+" / 4",uiHeading);
                for(int i=0;i<4;i++)
                {
                    var row=new Rect(80,156+i*60,480,52);Fill(row,new Color(0,0,0,.35f));
                    if(i<members.Length){var m=members[i];int c=Mathf.Clamp(m.playerClass,0,ClassNames.Length-1);Fill(new Rect(row.x,row.y,6,row.height),ClassTints[c]);GUI.Label(new Rect(row.x+20,row.y,280,row.height),m.name+(m.id==0?"  <color=#CC9E52><size=14>ANFITRIÓN</size></color>":""),uiText);GUI.Label(new Rect(row.x+300,row.y,170,row.height),ClassNames[c],new GUIStyle(uiMuted){alignment=TextAnchor.MiddleRight,fontSize=17});}
                    else GUI.Label(new Rect(row.x+20,row.y,400,row.height),"— lugar libre —",uiMuted);
                }
                var side=new Rect(610,112,330,300);Fill(side,Panel);
                GUI.Label(new Rect(630,122,290,24),host?(useFusion?"CÓDIGO DE SALA":"CÓDIGO LAN"):connecting?"CONECTANDO AL SERVIDOR":"CONECTADO",uiHeading);
                string code=host?(useFusion?address:localCodes):(useFusion?"Servidor":"Red local");
                GUI.Label(new Rect(630,150,290,70),code,new GUIStyle(uiText){fontSize=host&&useFusion?30:20,fontStyle=FontStyle.Bold});
                if(host&&GUI.Button(new Rect(630,226,290,30),"Copiar",uiChip))GUIUtility.systemCopyBuffer=useFusion?address:code.Split('\n')[0];
                int party=Mathf.Max(1,members.Length);
                GUI.Label(new Rect(630,266,290,24),"DIFICULTAD DEL GRUPO",uiHeading);
                GUI.Label(new Rect(630,292,290,110),party<=1?"Un solo peregrino: dificultad normal.":$"Vida enemiga +{(party-1)*60}%\nDaño enemigo +{(party-1)*12}%\n+{Mathf.Min(3,party-1)} enemigos por sala\nOro +{(party-1)*25}%"+(party>=3?" · cofres +1 nivel":""),uiMuted);
                if(host){if(GUI.Button(new Rect(60,430,880,56),"INICIAR EL DESCENSO",uiPrimary))StartMatch();}
                else GUI.Label(new Rect(60,430,880,56),"El anfitrión inicia el descenso.",new GUIStyle(uiText){alignment=TextAnchor.MiddleCenter});
            }
            Fill(new Rect(60,500,880,44),new Color(0,0,0,.35f));GUI.Label(new Rect(76,500,848,44),string.IsNullOrEmpty(status)?"Todos los equipos necesitan la misma versión del juego.":status,new GUIStyle(uiText){fontSize=17});
            GUI.Label(new Rect(60,552,880,60),"El portal lleva a todo el grupo. Mantén Interactuar junto a un aliado caído para revivirlo. Si el anfitrión sale, termina la expedición.",uiMuted);
            if(GUI.Button(new Rect(60,630,220,46),"VOLVER",uiButton))Leave();
            GUI.matrix=old;
        }
        // In-run party list: class colour, name and health for every pilgrim.
        void DrawParty()
        {
            float scale=Screen.height/1080f;var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float x=30,y=350;var ghosts=host?null:this.ghosts.Values.Where(g=>g.snapshot!=null&&g.snapshot.key!=null&&g.snapshot.key.StartsWith("Player:")).ToArray();
            GUI.Label(new Rect(x,y-28,330,24),(useFusion?"ONLINE":"LAN")+" · "+members.Length+"/4"+(CoopScaling.Party>1?"  ·  dificultad x"+CoopScaling.Party:""),uiMuted);
            for(int i=0;i<members.Length;i++)
            {
                var m=members[i];int c=Mathf.Clamp(m.playerClass,0,ClassNames.Length-1);float health=1;bool alive=true,known=false;
                var hostPlayer=host?(m.id==0?context.player:peers.FirstOrDefault(p=>p.id==m.id)?.player):null;
                if(hostPlayer){health=hostPlayer.State.Health/Mathf.Max(1,hostPlayer.State.MaxHealth);alive=hostPlayer.State.IsAlive;known=true;}
                else if(!host&&m.id==localId&&context.player){health=context.player.State.Health/Mathf.Max(1,context.player.State.MaxHealth);alive=context.player.State.IsAlive;known=true;}
                else if(!host){var g=ghosts.FirstOrDefault(e=>e.snapshot.owner==m.id);if(g&&g.snapshot.maxHealth>0){health=g.snapshot.health/g.snapshot.maxHealth;alive=health>0;known=true;}}
                var actor=host?hostPlayer:m.id==localId?context.player:null;
                var replica=host?null:ghosts.FirstOrDefault(g=>g.snapshot.owner==m.id)?.snapshot;
                string vitals=actor?$"Arm {actor.State.Armor:0}  PM {actor.State.Mana:0}  Frascos {actor.State.EstusCharges}":
                    replica!=null?$"Arm {replica.armor:0}  PM {replica.mana:0}  Frascos {replica.flasks}":"";
                var row=new Rect(x,y+i*60,300,54);Fill(row,new Color(.04f,.04f,.045f,.78f));Fill(new Rect(row.x,row.y,5,row.height),alive?ClassTints[c]:Muted);
                string shown=string.IsNullOrWhiteSpace(m.name)||m.name=="Jugador"?(m.id==0?"Anfitrión":"Invitado "+m.id):m.name;if(m.id==localId)shown+=" (tú)";
                GUI.Label(new Rect(row.x+14,row.y,190,24),shown,new GUIStyle(uiText){fontSize=16,normal={textColor=alive?Ivory:Muted}});
                GUI.Label(new Rect(row.x+150,row.y,140,24),alive?ClassNames[c]:"CAÍDO",new GUIStyle(uiMuted){fontSize=13,alignment=TextAnchor.MiddleRight,normal={textColor=alive?Muted:Blood}});
                GUI.Label(new Rect(row.x+14,row.y+32,276,21),vitals,uiMuted);
                Fill(new Rect(row.x+14,row.y+27,276,6),new Color(0,0,0,.7f));if(known&&alive)Fill(new Rect(row.x+14,row.y+27,276*Mathf.Clamp01(health),6),Color.Lerp(Blood,new Color(.7f,.16f,.18f),health));
            }
            if(floorHeld)GUI.Label(new Rect(x,y+members.Length*60+36,420,28),"Esperando a los peregrinos que cargan el piso…",uiText);
            if(context.player&&!context.player.State.IsAlive&&RunDirector.Instance&&RunDirector.Instance.Phase!=RunPhase.Defeated)GUI.Label(new Rect(x,y+members.Length*60+6,420,26),"Has caído: un compañero puede revivirte (Interactuar) o vuelves en la siguiente sala.",uiText);
            if(context.player&&context.player.State.IsAlive){string fallen=null;
                if(host){var a=FallenAllyNear(context.player);if(a){var m=members.FirstOrDefault(x=>x.id==(a.GetComponent<LanAvatar>()?.id??-1));fallen=m!=null?m.name:"tu compañero";}}
                else foreach(var g in this.ghosts.Values)if(g.snapshot!=null&&g.snapshot.key!=null&&g.snapshot.key.StartsWith("Player:")&&g.snapshot.maxHealth>0&&g.snapshot.health<=0&&Vector3.Distance(g.transform.position,context.player.transform.position)<ReviveRange){fallen=g.snapshot.name;break;}
                if(fallen!=null)GUI.Label(new Rect(1920f/2-300,700,600,34),(GameInput.UsingGamepad?(GameInput.PlayStation?"□":"X"):"[E]")+" · Mantener 1 s para revivir a "+fallen,new GUIStyle(uiText){fontSize=22,alignment=TextAnchor.MiddleCenter,normal={textColor=Ivory}});}   /* coop revive prompt */
            GUI.matrix=old;
        }
    }
}
