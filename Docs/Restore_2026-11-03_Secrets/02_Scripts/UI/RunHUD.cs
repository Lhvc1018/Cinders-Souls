using TMPro;
using Text = TMPro.TextMeshProUGUI;
using System.Collections.Generic;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Bosses;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using DG.Tweening;

namespace Codice.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed partial class RunHUD : MonoBehaviour
    {
        public static RunHUD Instance {get;private set;}
        public static bool HandlesEscapeThisFrame => Instance && escapeFrame==Time.frameCount;
        static int escapeFrame=-1;
        public RunScene Context {get;private set;}
        public PlayerCharacter Player => Context ? Context.player : null;
        public GhostBar PlayerHealth {get;private set;}
        public GhostBar PlayerMana {get;private set;}
        GhostBar posture; Image postureLeft,postureRight;
        public GhostBar BossHealth {get;private set;}
        public Image UltimateMeter {get;private set;}
        public bool Paused {get;private set;}
        public bool ShopVisible => shopPanel && shopPanel.gameObject.activeSelf;
        public bool ResultsVisible => (victoryPanel && victoryPanel.gameObject.activeSelf) || (defeatPanel && defeatPanel.gameObject.activeSelf);
        public bool TooltipVisible => tooltip && tooltip.gameObject.activeSelf;
        public int WorldBarCount => worldBars.Count;
        public Canvas Canvas {get;private set;}
        RunDirector director;
        PlayerMechanics mechanics;
        Text spellChoices;
        Text healthTitle,status,gold,timers,level,progressText,bossTitle,bossAttack,ultimateText,ammo,dash,banner,crosshair,hitmarker,cursorHint;
        Image progress,vignette,reload,ammoReload;
        int displayedSlot=-1;float switchTime;
        readonly Image[] slots=new Image[2], icons=new Image[2];
        readonly Text[] slotLabels=new Text[2], passiveLabels=new Text[3];
        readonly Image[] passiveFrames=new Image[3];
        readonly Image[] roomMarks=new Image[6];
        CanvasGroup roomHeadingFade;
        int lastPresentedRoom=-1;
        RectTransform spellPanel;
        RectTransform healthPanel,hotbar,bossPanel,bannerPanel;
        readonly Queue<string> notices=new Queue<string>();
        float bannerEnds,oldHealth,damagePulse,goldPulse,hitEnds;
        int oldGold; bool ready;
        public static RunHUD Install(RunScene scene)
        {
            if(Instance && Instance.Context==scene)return Instance;
            if(Instance){Instance.gameObject.SetActive(false);Destroy(Instance.gameObject);}
            var go=new GameObject("Códice • Interfaz",typeof(RectTransform));var hud=go.AddComponent<RunHUD>();hud.Context=scene;hud.Build();return hud;
        }
        void Awake(){Instance=this;}
        void Build()
        {
            Canvas=gameObject.AddComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=40;
            UIWidgets.ConfigureCanvas(Canvas);
            gameObject.AddComponent<GraphicRaycaster>();
            if(!FindFirstObjectByType<EventSystem>())new GameObject("UI Event System",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            vignette=UIWidgets.Image(transform,"Damage vignette",0,0,1920,1080,Color.clear,"vignette");Stretch(vignette.rectTransform);
            healthPanel=UIWidgets.Rect(transform,"Vital signs",48,40,530,130);
            healthTitle=UIWidgets.Label(healthPanel,"Class",14,0,460,28,18,ProceduralUISprites.Gold);
            PlayerHealth=GhostBar.Create(healthPanel,"Player HP",14,38,450,22);PlayerHealth.Caption.enabled=false;
            status=UIWidgets.Label(healthPanel,"Status",14,96,510,32,18);
            var goldRoot=UIWidgets.Rect(transform,"Ash currency",-330,-65,280,40);Anchor(goldRoot,1,0);
            gold=UIWidgets.Label(goldRoot,"Gold",0,0,280,40,24,ProceduralUISprites.Gold);gold.alignment=TextAlignmentOptions.MidlineRight;
            PlayerMana=GhostBar.Create(healthPanel,"Maná",14,69,360,15);PlayerMana.Fill.color=new Color(.29f,.43f,.55f);PlayerMana.Caption.enabled=false;
            spellPanel=Panel("Spell book",48,196,350,150);
            spellChoices=UIWidgets.Label(spellPanel,"Spell choices",16,12,320,130,18);
            posture=GhostBar.Create(transform,"Postura",-170,95,340,22);Anchor((RectTransform)posture.transform,.5f,.5f);posture.Caption.fontSize=14;
            postureLeft=UIWidgets.Filled(posture.transform,"Left posture",3,3,165,16,ProceduralUISprites.Gold);postureLeft.fillOrigin=1;
            postureRight=UIWidgets.Filled(posture.transform,"Right posture",172,3,165,16,ProceduralUISprites.Gold);postureRight.fillOrigin=0;
            UIWidgets.Image(posture.transform,"Center crest",166,-2,8,26,ProceduralUISprites.Gold,"diamond");posture.Caption.transform.SetAsLastSibling();
            var timePanel=UIWidgets.Rect(transform,"Run clock",-330,42,280,90);Anchor(timePanel,1,1);
            timers=UIWidgets.Label(timePanel,"Clocks",0,0,280,90,18,new Color(.68f,.65f,.55f));timers.alignment=TextAlignmentOptions.TopRight;
            var round=UIWidgets.Rect(transform,"Round",-260,40,520,90);Anchor(round,.5f,1);
            level=UIWidgets.Label(round,"Level",0,0,520,38,22,ProceduralUISprites.Gold);level.alignment=TextAlignmentOptions.Center;
            roomHeadingFade=level.gameObject.AddComponent<CanvasGroup>();roomHeadingFade.blocksRaycasts=false;
            var headingFont=Resources.Load<TMP_FontAsset>("CindersMenu/Cinzel");if(headingFont)level.font=headingFont;
            level.enableAutoSizing=true;level.fontSizeMin=16;level.fontSizeMax=22;
            progress=UIWidgets.Filled(round,"Round progress",90,44,340,1,ProceduralUISprites.Gold);
            for(int i=0;i<roomMarks.Length;i++){roomMarks[i]=UIWidgets.Image(round,"Room sigil "+(i+1),87+i*68,41,6,6,ProceduralUISprites.Gold,"diamond");roomMarks[i].gameObject.SetActive(false);}
            progressText=UIWidgets.Label(round,"Progress",0,58,520,35,18,new Color(.76f,.73f,.65f));progressText.alignment=TextAlignmentOptions.Center;
            hotbar=UIWidgets.Rect(transform,"Equipment and relics",48,-310,690,270);Anchor(hotbar,0,0);
            UIWidgets.Label(hotbar,"Loadout heading",0,0,510,26,16,ProceduralUISprites.Gold).text="E Q U I P O   /   R E L I Q U I A S";
            for(int i=0;i<2;i++)
            {
                slots[i]=UIWidgets.Image(hotbar,"Weapon slot "+i,i*110,38,100,98,new Color(.22f,.2f,.15f,.9f));
                icons[i]=UIWidgets.Image(slots[i].transform,"Silhouette",30,7,40,46,new Color(.85f,.80f,.65f));
                slotLabels[i]=UIWidgets.Label(slots[i].transform,"Weapon",4,60,92,36,17);slotLabels[i].alignment=TextAlignmentOptions.Center;
                var active=UIWidgets.Surface(hotbar,"Active relic "+i,232+i*138,38,130,98);
                activeCooldowns[i]=UIWidgets.Filled(active,"Cooldown",2,2,126,94,new Color(.27f,.25f,.19f,.65f));
                activeCooldowns[i].fillMethod=Image.FillMethod.Vertical;activeCooldowns[i].fillOrigin=0;
                activeLabels[i]=UIWidgets.Label(active,"Relic label",7,4,116,92,18);activeLabels[i].alignment=TextAlignmentOptions.Center;
            }
            ammo=UIWidgets.Label(hotbar,"Ammunition",0,142,510,28,17);ammo.alignment=TextAlignmentOptions.Left;
            for(int i=0;i<3;i++)
            {
                passiveFrames[i]=UIWidgets.Image(hotbar,"Passive "+i,i*173,180,164,40,new Color(.08f,.08f,.065f,.7f));
                passiveLabels[i]=UIWidgets.Label(passiveFrames[i].transform,"Passive name",6,3,152,34,17);passiveLabels[i].alignment=TextAlignmentOptions.Center;
            }
            dash=UIWidgets.Label(hotbar,"Dash",0,238,690,28,16,new Color(.72f,.70f,.61f));
            var medallion=UIWidgets.Image(hotbar,"Weapon art",548,38,88,88,new Color(.40f,.34f,.22f),"ring");
            UltimateMeter=UIWidgets.Filled(medallion.transform,"Ultimate charge",4,4,80,80,ProceduralUISprites.Gold,true);
            ultimateText=UIWidgets.Label(medallion.transform,"Ultimate percentage",10,16,68,65,20);ultimateText.alignment=TextAlignmentOptions.Center;
            specialName=UIWidgets.Label(hotbar,"Special name",520,142,140,70,17,ProceduralUISprites.Gold);specialName.alignment=TextAlignmentOptions.Center;
            ammoReload=UIWidgets.Filled(hotbar,"Ammo regeneration",0,171,510,2,ProceduralUISprites.Gold);
            bossPanel=UIWidgets.Rect(transform,"Boss",-300,-155,900,100);Anchor(bossPanel,.5f,0);
            bossTitle=UIWidgets.Label(bossPanel,"Boss name",0,0,900,34,24,new Color(.90f,.86f,.75f));
            BossHealth=GhostBar.Create(bossPanel,"Boss HP",0,41,900,18);BossHealth.Caption.enabled=false;
            foreach(float f in new[]{.3f,.65f})UIWidgets.Image(BossHealth.transform,"Phase threshold",898*f,2,1,14,ProceduralUISprites.Gold);
            bossAttack=UIWidgets.Label(bossPanel,"Telegraph",0,67,900,26,19,ProceduralUISprites.Gold);
            bannerPanel=Panel("Announcement",-360,165,720,62);Anchor(bannerPanel,.5f,1);
            banner=UIWidgets.Label(bannerPanel,"Notice",18,12,684,40,25,ProceduralUISprites.Gold);banner.alignment=TextAlignmentOptions.Center;bannerPanel.gameObject.SetActive(false);
            crosshair=UIWidgets.Label(transform,"Crosshair",-30,-30,60,60,26);Anchor(crosshair.rectTransform,.5f,.5f);crosshair.alignment=TextAlignmentOptions.Center;
            reload=UIWidgets.Filled(transform,"Reload arc",-34,-34,68,68,ProceduralUISprites.Gold,true);Anchor(reload.rectTransform,.5f,.5f);
            hitmarker=UIWidgets.Label(transform,"Hitmarker",-100,35,200,45,23,ProceduralUISprites.Gold);Anchor(hitmarker.rectTransform,.5f,.5f);hitmarker.alignment=TextAlignmentOptions.Center;
            cursorHint=UIWidgets.Label(transform,"Cursor help",-410,-34,820,26,16);Anchor(cursorHint.rectTransform,.5f,0);cursorHint.alignment=TextAlignmentOptions.Center;
            BuildDungeonHud();BuildCleanHud();BuildMenus();BuildWorld();
            mechanics=Player.GetComponent<PlayerMechanics>();oldHealth=Player.State.Health;oldGold=Player.State.Gold;
            Player.State.Changed+=StateChanged;Player.State.Died+=Died;
            if(mechanics)mechanics.Changed+=MechanicsChanged;
            director=RunDirector.Instance;if(director){director.Notice+=Announce;director.Changed+=RunChanged;}
            CombatFeedback.Resolved+=CombatResolved;
            StateChanged();Refresh();
        }
        Text specialName;
        RectTransform Panel(string name,float x,float y,float w,float h)=>UIWidgets.Surface(transform,name,x,y,w,h);
        static void Anchor(RectTransform r,float x,float y){r.anchorMin=r.anchorMax=new Vector2(x,y);}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        public void Announce(string message){if(notices.Count<8)notices.Enqueue(message);}
        void RunChanged(){Refresh();}
        void MechanicsChanged(){if(!mechanics)return;bool now=mechanics.Charge>=100;if(now&&!ready)Announce("¡Especial listo [Q]!");ready=now;}
        void StateChanged()
        {
            if(!Player)return;
            float hp=Player.State.Health;
            if(hp<oldHealth)damagePulse=1;
            else if(hp>oldHealth)SpawnNumber(Player.transform.position+Vector3.up*1.6f,"+"+(hp-oldHealth).ToString("0"),ProceduralUISprites.Cyan,true);
            if(Player.State.Gold>oldGold)goldPulse=1;
            oldHealth=hp;oldGold=Player.State.Gold;PlayerHealth.Set(hp,Player.State.MaxHealth);MechanicsChanged();
        }
        void Died(){SetPaused(false);}
        void Update()
        {
            if(!Context||!Player)return;
            var keys=Keyboard.current;
            if(keys!=null&&keys.escapeKey.wasPressedThisFrame&&Player.State.IsAlive&&!ShopVisible)
            {escapeFrame=Time.frameCount;SetPaused(!Paused);}
            Refresh();UpdateMenus();UpdateWorld();UpdateBossAnnouncement();RefreshCleanHud();
            float dt=Time.unscaledDeltaTime;
            damagePulse=Mathf.MoveTowards(damagePulse,0,dt*1.8f);goldPulse=Mathf.MoveTowards(goldPulse,0,dt*2.5f);
            vignette.color=new Color(.85f,.08f,.19f,damagePulse*.65f);
            healthPanel.anchoredPosition=new Vector2(48+Mathf.Sin(Time.unscaledTime*83)*damagePulse*4,-40);
            gold.transform.localScale=Vector3.one*(1+.16f*goldPulse);
            if(Time.unscaledTime>=bannerEnds&&notices.Count>0){banner.text=notices.Dequeue();bannerEnds=Time.unscaledTime+2.6f;}
            bannerPanel.gameObject.SetActive(Time.unscaledTime<bannerEnds);
            bannerPanel.localScale=Vector3.one*Mathf.Lerp(.95f,1,Mathf.Clamp01((bannerEnds-Time.unscaledTime)*3));
            hitmarker.enabled=Time.unscaledTime<hitEnds;
        }
        public void Refresh()
        {
            if(!Player||!PlayerHealth)return;
            var state=Player.State;var weapon=state.ActiveWeapon;
            healthTitle.text=""+new[]{"GUERRERO","TANQUE","ARQUERO","MAGO","SHINOBI"}[(int)state.Class];
            PlayerHealth.Set(state.Health,state.MaxHealth);
            PlayerMana.gameObject.SetActive(true);PlayerMana.Set(state.Mana,state.MaxMana);PlayerMana.Caption.text=$"MANÁ {state.Mana:0}/{state.MaxMana:0}";
            var guard=Player.GetComponent<ShinobiGuard>();posture.gameObject.SetActive(guard&&state.Class==PlayerClass.Shinobi&&guard.ShowPosture);if(guard){posture.Set(100-guard.Posture,100,guard.Broken?"POSTURA ROTA":guard.RecentPerfect?"DESVÍO PERFECTO":"POSTURA");posture.Fill.color=guard.Broken?ProceduralUISprites.Crimson:guard.RecentPerfect?Color.white:ProceduralUISprites.Gold;postureLeft.fillAmount=postureRight.fillAmount=(100-guard.Posture)/100;postureLeft.color=postureRight.color=posture.Fill.color;posture.Fill.enabled=false;posture.Ghost.enabled=false;}
            status.text=Player.IsStaggered?"DERRIBADO":Player.MovementMultiplier<1?"LODO • Ralentizado −35%":mechanics&&mechanics.Invulnerable?"INVULNERABLE":mechanics&&mechanics.ShadowStep?"PASO SOMBRÍO":mechanics&&mechanics.ShieldActive?"ESCUDO FRONTAL • −50%":"";
            gold.text=$"•  {state.Gold:N0}   ORO";
            if(displayedSlot!=state.ActiveSlot){displayedSlot=state.ActiveSlot;switchTime=Time.unscaledTime;}
            for(int i=0;i<2;i++)
            {
                var w=state.GetWeapon(i);bool active=i==state.ActiveSlot;
                slots[i].color=active?new Color(.38f,.32f,.20f,.9f):new Color(.12f,.115f,.09f,.85f);
                slots[i].transform.localScale=Vector3.one;
                icons[i].enabled=w!=null;if(w!=null)icons[i].sprite=ProceduralUISprites.Weapon(w);
                slotLabels[i].text=w==null?$"[{i+1}] Vacío":$"[{i+1}] {UIWidgets.WeaponName(w)}";
            }
            RefreshRelicHud();RefreshDungeonHud();
            float charge=mechanics?mechanics.Charge:0;UltimateMeter.fillAmount=charge/100;
            UltimateMeter.color=charge>=100?Color.Lerp(ProceduralUISprites.Cyan,ProceduralUISprites.Gold,(Mathf.Sin(Time.unscaledTime*6)+1)/2):ProceduralUISprites.Cyan;
            ultimateText.text=mechanics&&mechanics.UltimateActive?"ACTIVO":$"{Mathf.FloorToInt(charge)}%\n[Q]";
            specialName.text=weapon==null?"":(weapon.katana?"Tajos de Sangre":weapon.shuriken?"Tormenta de Shuriken":UIWidgets.Special(weapon.kind))+"\n[Q]";
            ammo.text=weapon!=null&&weapon.Capacity>0?$"{(weapon.shuriken?"SHURIKEN":weapon.kind==WeaponKind.Bow?"FLECHAS":"MUNICIÓN")}   {weapon.ammunition} / {weapon.Capacity}"+(weapon.kind==WeaponKind.Musket?$"  •  Reserva {weapon.reserveAmmunition}  [R]":"  •  Recoge suministros") : "";
            var combat=Player.GetComponent<PlayerCombat>();
            spellPanel.gameObject.SetActive(weapon!=null&&weapon.kind==WeaponKind.Staff);
            if(weapon!=null&&weapon.kind==WeaponKind.Staff)
            {
                spellChoices.text="HECHIZOS • Clic izquierdo para lanzar\n";
                for(int i=0;i<4;i++){var s=(StaffSpell)i;spellChoices.text+=(s==state.SelectedSpell?"<color=#FFD978>> ":"  ")+"["+(i+1)+"] "+StaffSpells.Label(s)+" • "+StaffSpells.Cost(s)+" maná"+(s==state.SelectedSpell?"</color>":"")+"\n";}
                ammo.text=$"{StaffSpells.Label(state.SelectedSpell)} • Espera {Player.GetComponent<StaffSpells>().Remaining:0.0}s • [Tab] Arma • [Q] Especial";
            }
            if(state.DualWield)ammo.text="DUALES • +30% velocidad / +25% daño";
            if(combat&&combat.IsReloading)ammo.text+="  •  RECARGANDO";
            reload.fillAmount=combat&&combat.IsReloading?combat.ReloadFraction:0;
            ammoReload.fillAmount=reload.fillAmount;
            crosshair.text=weapon==null?"·":weapon.kind==WeaponKind.Bow?"○":weapon.kind==WeaponKind.Musket?"┼":weapon.Ranged?"○":"·";
            dash.text="[Espacio] Salto   [Shift] Dash   "+(Player.DashCooldown<=0?"• LISTO":$"○ {Player.DashCooldown:0.0}s")+(state.HasPassive(PassiveKind.MercuryBoots)?(Player.SecondDashCooldown<=0?"   • LISTO":$"   ○ {Player.SecondDashCooldown:0.0}s"):"");
            if(director && director.Expedition)
            {
                var floor=director.Expedition;
                timers.text=$"DESCENSO   {UIWidgets.Clock(director.Elapsed)}\nSEMILLA   {floor.Seed}\n<size=17>UNA VIDA · UN INQUISIDOR</size>";
                level.text=$"SALA {floor.PlayerRoom+1:00} / {Codice.Expedition.ExpeditionFloor.RoomCount:00} · {floor.RoomTitle}";
                progress.fillAmount=(float)floor.ClearedRooms/Codice.Expedition.ExpeditionFloor.RoomCount;
                progressText.text=floor.Objective;
                for(int i=0;i<roomMarks.Length;i++)
                {
                    roomMarks[i].gameObject.SetActive(true);
                    roomMarks[i].color=i==floor.CurrentRoom?ProceduralUISprites.Gold:i<floor.ClearedRooms?new Color(.62f,.52f,.4f):new Color(.27f,.25f,.27f);
                }
                if(lastPresentedRoom!=floor.CurrentRoom)
                {
                    lastPresentedRoom=floor.CurrentRoom;
                    roomHeadingFade.DOKill();roomHeadingFade.alpha=.35f;roomHeadingFade.DOFade(1,.35f).SetUpdate(true).SetLink(level.gameObject);
                }
                var marker=level.transform.parent.Find("Merchant midpoint");if(marker)marker.gameObject.SetActive(false);
            }
            else if(director)
            {
                for(int i=0;i<roomMarks.Length;i++)roomMarks[i].gameObject.SetActive(false);
                timers.text=$"PARTIDA   {UIWidgets.Clock(director.Elapsed)}\nRONDA     {UIWidgets.Clock(director.RoundElapsed)}\n<size=16><color={(director.RoundElapsed<180?"#F5D76E":"#777C91")}>• RAPIDEZ +100 • MENOS DE 03:00</color></size>";
                level.text=$"RONDA {director.Level:00} / {RoundRules.FinalRound}   •   {RoundRules.MapTitle(director.Level)}";
                if(Context.UsesDungeonEncounter(director.Level)){int total=Context.dungeon.Total;progress.fillAmount=total>0?(float)Context.dungeon.Defeated/total:0;progressText.text=$"{total-Context.dungeon.Defeated} / {total} ENEMIGOS  •  MERCADER {(total+1)/2}/{total}  •  JEFE CADA 5";}
                else{progress.fillAmount=Context.arena&&Context.arena.Completed?1:0;progressText.text="ENCUENTRO DE JEFE";}
                if(director.Phase==RunPhase.Shop)progressText.text=$"MERCADER • [E] COMPRAR • [ENTER] CONTINUAR • {director.ShopRemaining:0}s";
                else if(director.Phase==RunPhase.Cleared)progressText.text=RoundRules.OpensPortalAfter(director.Level)?"RONDA COMPLETADA • ENTRA AL PORTAL [E]":"RONDA COMPLETADA • PREPARANDO SIGUIENTE RONDA";
            }
            var boss=Context.arena?Context.arena.ActiveBoss:null;bossPanel.gameObject.SetActive(boss);
            if(boss)
            {bossTitle.text=(director&&director.Expedition?"EL INQUISIDOR DEL HORROR":BossName(boss.kind))+(boss.Evolved?" • EVOLUCIONADO Nv.15+":"");BossHealth.Set(boss.Health,boss.MaxHealth);bossAttack.text=$"FASE {boss.Phase}   /   "+BossAttackLabel(boss.CurrentAttack);}
            cursorHint.text=Paused?"[Escape] Reanudar y capturar cursor":ShopVisible?"[E / Escape] Cerrar tienda   •   [Enter] Continuar ronda":Cursor.lockState==CursorLockMode.Locked?"[E] Interactuar   •   [Escape] Pausa / liberar cursor":"Haz clic para capturar el cursor   •   [Escape] Pausa";
        }
        static string BossAttackLabel(BossAttack attack)=>attack switch { BossAttack.Sweep=>"Barrido del incensario",BossAttack.Slam=>"Sangre tóxica · Sal del círculo",BossAttack.Bow=>"Proyectiles · Esquiva",BossAttack.Charge=>"Carga · Apártate",BossAttack.Rock=>"Roca entrante",BossAttack.Mud=>"Lodo",_=>"Acechando" };
        static string BossName(BossKind kind)=>kind==BossKind.GoblinKing?"REY GOBLIN DE LA INMUNDICIA":kind==BossKind.DemonLord?"SEÑOR DEMONIO DEL ABISMO":kind==BossKind.MonsterOrc?"MONSTER ORC":kind==BossKind.MonsterMutant?"MONSTER MUTANT":"ORK BERSERKER • JEFE FINAL";
        void CombatResolved(CombatFeedback.Result result)
        {
            if(!Player||!result.hit.source||result.hit.source.GetComponentInParent<PlayerCharacter>()!=Player)return;
            hitEnds=Time.unscaledTime+.22f;
            hitmarker.text=result.blocked?"×  BLOQUEADO −80%":result.hit.critical?"×  CRÍTICO ×2":"×";
            hitmarker.color=result.blocked?ProceduralUISprites.Ink:result.hit.critical?ProceduralUISprites.Gold:Color.white;
            SpawnNumber(result.position,$"{result.amount:0}"+(result.blocked?" • ESCUDO":result.hit.critical?" !":result.hit.reflected?" • ESPINAS":""),hitmarker.color,false);
        }
        void OnDisable()
        {
            SetPaused(false);
            if(Player){Player.State.Changed-=StateChanged;Player.State.Died-=Died;}
            if(mechanics)mechanics.Changed-=MechanicsChanged;
            if(director){director.Notice-=Announce;director.Changed-=RunChanged;}
            CombatFeedback.Resolved-=CombatResolved;
            foreach(var bar in worldBars.Values)if(bar)Destroy(bar.gameObject);
            worldBars.Clear();
            if(Instance==this)Instance=null;
        }
    }
}
