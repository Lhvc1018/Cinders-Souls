using Codice.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        RectTransform cleanEquipment,cleanFlaskSlot;
        Image heldIcon;
        Image cleanCharge;
        GhostBar playerArmor;
        TextMeshProUGUI lowArmor;
        TextMeshProUGUI heldName,heldKey,cleanSpecial,cleanFlask,lowHealth,lowEnergy,compass;
        readonly HudGlyph[] relicGlyphs=new HudGlyph[2];
        float roomNameUntil;
        int cleanRoom=-1;
        static void Layout(RectTransform r,float x,float y,float width,float height){r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        void BuildCleanHud()
        {
            healthTitle.gameObject.SetActive(false);status.fontSize=16;
            Layout((RectTransform)PlayerHealth.transform,10,3,350,18);ResizeBar(PlayerHealth,350,18);
            playerArmor=GhostBar.Create(healthPanel,"Player armor",10,27,310,16);ResizeBar(playerArmor,310,16);playerArmor.Fill.color=new Color(.56f,.59f,.62f);
            Layout((RectTransform)PlayerMana.transform,10,51,270,15);ResizeBar(PlayerMana,270,15);
            PlayerHealth.Caption.enabled=true;PlayerMana.Caption.enabled=true;
            lowHealth=UIWidgets.Label(healthPanel,"Low health",367,-2,22,30,22,new Color(.96f,.30f,.25f));lowHealth.text="!";
            lowEnergy=UIWidgets.Label(healthPanel,"Low energy",286,44,22,30,22,new Color(.47f,.7f,.97f));lowEnergy.text="!";
            lowArmor=UIWidgets.Label(healthPanel,"Low armor",327,20,22,30,22,new Color(.78f,.8f,.82f));lowArmor.text="!";
            Layout(status.rectTransform,10,75,350,30);
            timers.transform.parent.gameObject.SetActive(false);
            progress.gameObject.SetActive(false);progressText.gameObject.SetActive(false);foreach(var mark in roomMarks)mark.gameObject.SetActive(false);
            var heading=(RectTransform)level.transform.parent;Layout(heading,-300,90,600,45);Layout(level.rectTransform,0,0,600,38);
            compass=UIWidgets.Label(transform,"Compass",-120,37,240,28,17,new Color(.75f,.72f,.6f));Anchor(compass.rectTransform,.5f,1);compass.alignment=TextAlignmentOptions.Center;
            hotbar.gameObject.SetActive(false);flaskPanel.gameObject.SetActive(false);spellPanel.gameObject.SetActive(false);
            cleanEquipment=UIWidgets.Rect(transform,"Quick equipment",42,-256,330,228);Anchor(cleanEquipment,0,0);
            // Cross-shaped quick slots: two actives, held weapon and flask.
            for(int i=0;i<2;i++)
            {
                var tile=UIWidgets.Image(cleanEquipment,"Quick active "+i,i==0?0:73,i==0?73:0,66,66,new Color(.035f,.033f,.026f,.65f));
                activeCooldowns[i].transform.SetParent(tile.transform,false);Layout(activeCooldowns[i].rectTransform,0,0,66,66);
                relicGlyphs[i]=HudGlyph.Create(tile.transform,i==0?HudSymbol.Censer:HudSymbol.Bell,14,5,38);
                activeLabels[i].transform.SetParent(tile.transform,false);Layout(activeLabels[i].rectTransform,0,44,66,22);activeLabels[i].fontSize=15;
            }
            var weapon=UIWidgets.Image(cleanEquipment,"Held weapon",146,73,66,66,new Color(.04f,.036f,.027f,.72f));
            heldIcon=UIWidgets.Image(weapon.transform,"Weapon icon",7,2,52,49,Color.white);heldIcon.preserveAspect=true;
            heldKey=UIWidgets.Label(weapon.transform,"Switch key",0,45,66,21,14);heldKey.alignment=TextAlignmentOptions.Center;heldKey.text="Tab";
            var potion=UIWidgets.Image(cleanEquipment,"Quick flask",73,146,66,66,new Color(.045f,.033f,.02f,.72f));HudGlyph.Create(potion.transform,HudSymbol.Flask,12,2,42);
            cleanFlaskSlot=potion.rectTransform;
            cleanFlask=UIWidgets.Label(potion.transform,"Flask count",0,44,66,22,16);cleanFlask.alignment=TextAlignmentOptions.Center;
            flaskProgress.transform.SetParent(potion.transform,false);Layout(flaskProgress.rectTransform,0,63,66,3);
            heldName=UIWidgets.Label(cleanEquipment,"Equipped name",0,217,235,29,17);heldName.alignment=TextAlignmentOptions.Center;
            cleanSpecial=UIWidgets.Label(cleanEquipment,"Weapon skill",0,-43,300,29,17,new Color(.88f,.83f,.67f));
            UIWidgets.Image(cleanEquipment,"Skill charge track",0,-12,146,2,new Color(.12f,.11f,.085f,.8f));
            cleanCharge=UIWidgets.Filled(cleanEquipment,"Skill charge",0,-12,146,2,ProceduralUISprites.Gold);
            ammo.transform.SetParent(cleanEquipment,false);Layout(ammo.rectTransform,218,85,140,55);ammo.fontSize=17;
            gold.fontSize=20;gold.rectTransform.sizeDelta=new Vector2(280,32);
            Layout(bossPanel,-350,-105,700,65);ResizeBar(BossHealth,700,14);Layout((RectTransform)BossHealth.transform,0,29,700,14);Layout(bossTitle.rectTransform,0,0,700,30);bossTitle.fontSize=20;Layout(bossAttack.rectTransform,0,47,700,25);bossAttack.fontSize=16;
            foreach(Transform child in BossHealth.transform)if(child.name=="Phase threshold")child.gameObject.SetActive(false);
        }
        static void ResizeBar(GhostBar bar,float width,float height)
        {
            foreach(Transform child in bar.transform)
            {
                if(child==bar.Caption.transform)continue;var rect=child as RectTransform;if(rect)Layout(rect,2,2,width-4,height-4);
            }
            Layout(bar.Caption.rectTransform,3,-2,width-6,24);bar.Caption.fontSize=16;
        }
        void RefreshCleanHud()
        {
            var state=Player.State;var w=state.ActiveWeapon;
            lowHealth.enabled=state.Health<state.MaxHealth*.5f;lowEnergy.enabled=state.Mana<state.MaxMana*.5f;lowArmor.enabled=state.Armor<=0;
            playerArmor.Set(state.Armor,state.MaxArmor);playerArmor.Caption.enabled=true;
            PlayerMana.gameObject.SetActive(true);
            PlayerMana.Caption.text=$"{Mathf.CeilToInt(state.Mana)} / {Mathf.CeilToInt(state.MaxMana)}";
            if(w!=null){heldIcon.sprite=ProceduralUISprites.Weapon(w);heldIcon.enabled=true;heldName.text=UIWidgets.WeaponName(w)+"   <color=#A79C82>"+new[]{"I","II","III","IV"}[Mathf.Clamp(w.tier-1,0,3)]+"</color>";}else{heldIcon.enabled=false;heldName.text="Sin arma";}
            for(int i=0;i<2;i++){relicGlyphs[i].Set((HudSymbol)((int)state.GetRelic(i)+1));float wait=state.RelicWait(i);activeLabels[i].text=wait>0?$"{wait:0.0}s":state.Mana<RelicCatalog.ManaCost(state.GetRelic(i))?"Sin PM":i==0?"F":"G";}
            cleanFlask.text="H  "+state.EstusCharges;
            cleanFlaskSlot.gameObject.SetActive(director&&director.Expedition&&!Codice.Multiplayer.LanSession.Active);
            cleanSpecial.text=w==null?"":$"[Q] {(w.katana?"Tajos de sangre":w.shuriken?"Tormenta de shuriken":UIWidgets.Special(w.kind))}"+(mechanics&&mechanics.Charge>=100?"  ·":"");
            cleanSpecial.color=mechanics&&mechanics.Charge>=100?ProceduralUISprites.Gold:new Color(.72f,.70f,.63f);
            cleanCharge.fillAmount=mechanics?mechanics.Charge/100f:0;
            ammo.text=w!=null&&w.Capacity>0?$"{w.ammunition}/{w.Capacity}"+(w.kind==WeaponKind.Musket?$"\nR · {w.reserveAmmunition}":""):w!=null&&w.kind==WeaponKind.Staff?$"{StaffSpells.Label(state.SelectedSpell)}\n[1–4]":"";
            spellPanel.gameObject.SetActive(false);progress.gameObject.SetActive(false);progressText.gameObject.SetActive(false);foreach(var mark in roomMarks)mark.gameObject.SetActive(false);
            int room=director&&director.Expedition?director.Expedition.PlayerRoom:-1;
            if(room!=cleanRoom){cleanRoom=room;roomNameUntil=Time.unscaledTime+3.5f;}
            level.gameObject.SetActive(Time.unscaledTime<roomNameUntil);
            float yaw=Player.view.transform.eulerAngles.y;string direction=yaw<22.5f||yaw>=337.5f?"N":yaw<67.5f?"NE":yaw<112.5f?"E":yaw<157.5f?"SE":yaw<202.5f?"S":yaw<247.5f?"SO":yaw<292.5f?"O":"NO";
            compass.text="—      "+direction+"      —";
            if(Cursor.lockState==CursorLockMode.Locked)cursorHint.text="";
            var poison=Player.GetComponent<Codice.Bosses.InquisitorPoison>();if(poison&&poison.Active)status.text="SANGRE TÓXICA";
        }
    }
}
