using TMPro;
using Text = TMPro.TextMeshProUGUI;
using Codice.Mechanics;
using Codice.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        RectTransform bossAnnouncement;
        CanvasGroup bossAnnouncementFade;
        Text bossAnnouncementTitle, bossAnnouncementDetail;
        int announcedBossRound=-1;
        float bossAnnouncementTime;
        void UpdateBossAnnouncement()
        {
            if (!director) return;
            if ((director.Expedition?director.Expedition.IsBossRoom:RoundRules.IsBoss(director.Level)) && director.CombatActive && Context.arena && Context.arena.ActiveBoss && announcedBossRound!=director.Level)
            {
                announcedBossRound=director.Level;
                if (!bossAnnouncement)
                {
                    bossAnnouncement=UIWidgets.Rect(transform,"Boss announcement",-350,125,700,90);Anchor(bossAnnouncement,.5f,1);
                    bossAnnouncementFade=bossAnnouncement.gameObject.AddComponent<CanvasGroup>();
                    bossAnnouncementFade.blocksRaycasts=false;bossAnnouncementFade.interactable=false;
                    UIWidgets.Image(bossAnnouncement,"Gold line",100,2,500,1,ProceduralUISprites.Gold);
                    bossAnnouncementTitle=UIWidgets.Label(bossAnnouncement,"JEFE",20,10,660,42,32,ProceduralUISprites.Gold);
                    bossAnnouncementTitle.alignment=TextAlignmentOptions.Center;bossAnnouncementTitle.fontStyle=FontStyles.Bold;
                    bossAnnouncementTitle.enableAutoSizing=true;bossAnnouncementTitle.fontSizeMin=24;bossAnnouncementTitle.fontSizeMax=32;
                    bossAnnouncementDetail=UIWidgets.Label(bossAnnouncement,"Boss details",20,52,660,28,17,Color.white);
                    bossAnnouncementDetail.alignment=TextAlignmentOptions.Center;
                    UIWidgets.Image(bossAnnouncement,"Gold line bottom",100,84,500,1,ProceduralUISprites.Gold);
                }
                bossAnnouncementTitle.text=director.Expedition?"EL INQUISIDOR DEL HORROR":BossName(Context.arena.ActiveBoss.kind);
                bossAnnouncementDetail.text=director.Expedition?"GUARDIÁN DEL PRIMER DESCENSO":$"{(RoundRules.IsFinal(director.Level)?"JEFE FINAL":"JEFE")}  •  RONDA {director.Level:00}";
                bossAnnouncementTime=Time.unscaledTime;bossAnnouncement.gameObject.SetActive(true);
                GameAudio.Play(SoundCue.Boss);
            }
            if (!bossAnnouncement) return;
            float age=Time.unscaledTime-bossAnnouncementTime;
            bossAnnouncement.gameObject.SetActive(age<1.8f && director.CombatActive && !Paused);
            bossAnnouncementFade.alpha=Mathf.Clamp01(age/.2f)*Mathf.Clamp01((1.8f-age)/.4f);
            bossAnnouncement.localScale=Vector3.one;
        }
    }
}
