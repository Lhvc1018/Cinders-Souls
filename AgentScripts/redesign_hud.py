from pathlib import Path
p=Path(r'D:/67/Last67Seconds-Maps/Assets/02_Scripts/UI/RunHUD.cs')
s=p.read_text(encoding='utf-8-sig')
start=s.index('            healthPanel=Panel(')
end=s.index('            BuildMenus();BuildWorld();',start)
s=s[:start]+'''            healthPanel=UIWidgets.Rect(transform,"Vital signs",48,40,530,130);
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
''' + s[end:]
s=s.replace('=>UIWidgets.Image(transform,name,x,y,w,h,ProceduralUISprites.Ink,"frame").rectTransform','=>UIWidgets.Surface(transform,name,x,y,w,h)')
s=s.replace('new Vector2(32+Mathf.Sin(Time.unscaledTime*83)*damagePulse*7,-30)','new Vector2(48+Mathf.Sin(Time.unscaledTime*83)*damagePulse*4,-40)')
a=s.index('                slots[i].color=w==null?');b=s.index('            float charge=',a)
s=s[:a]+'''                slots[i].color=active?new Color(.38f,.32f,.20f,.9f):new Color(.12f,.115f,.09f,.85f);
                slots[i].transform.localScale=Vector3.one;
                icons[i].enabled=w!=null;if(w!=null)icons[i].sprite=ProceduralUISprites.Weapon(w);
                slotLabels[i].text=w==null?$"[{i+1}] Vacío":$"[{i+1}] {UIWidgets.WeaponName(w)}";
            }
            RefreshRelicHud();
''' + s[b:]
s=s.replace('healthTitle.text="CÓDICE DE CENIZA  /  "','healthTitle.text=""')
s=s.replace('dash.text="SALTO [Espacio] • DASH [Shift]\\n"','dash.text="[Espacio] Salto   [Shift] Dash   "')
p.write_text(s,encoding='utf-8')

p=Path(r'D:/67/Last67Seconds-Maps/Assets/02_Scripts/UI/RelicButtonFace.cs')
p.write_text('''using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RelicButtonFace : MaskableGraphic
    {
        public bool primary,highlighted,pressed;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Color edge=highlighted?new Color(.78f,.69f,.47f):new Color(.37f,.33f,.23f,.65f);
            void Quad(float x,float y,float w,float h,Color tint){int n=vh.currentVertCount;vh.AddVert(new Vector2(x,y),tint*color,Vector2.zero);vh.AddVert(new Vector2(x+w,y),tint*color,Vector2.zero);vh.AddVert(new Vector2(x+w,y+h),tint*color,Vector2.zero);vh.AddVert(new Vector2(x,y+h),tint*color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
            Quad(r.xMin,r.yMin,r.width,r.height,pressed?new Color(.28f,.24f,.15f,.9f):highlighted?new Color(.27f,.24f,.16f,.8f):primary?new Color(.15f,.14f,.10f,.7f):new Color(.035f,.035f,.03f,.45f));
            Quad(r.xMin,r.yMin,r.width,1,edge);
            if(highlighted||primary){Quad(r.xMin,r.yMax-1,r.width,1,edge);Quad(r.xMin+21,r.center.y-3,6,6,edge);}
        }
    }
}
''',encoding='utf-8')
