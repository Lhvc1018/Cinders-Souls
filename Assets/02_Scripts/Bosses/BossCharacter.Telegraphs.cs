using UnityEngine;
namespace Codice.Bosses
{
    public sealed partial class BossCharacter
    {
        GameObject standardWarning;
        void ClearStandardTelegraph(){if(standardWarning){standardWarning.SetActive(false);Destroy(standardWarning);}standardWarning=null;}
        void ShowStandardTelegraph()
        {
            ClearStandardTelegraph();var tint=new Color(1,.36f,.12f);
            var floor=transform.position+Vector3.up*.08f;
            if(CurrentAttack==BossAttack.Sweep||CurrentAttack==BossAttack.Slam)
                standardWarning=BloodVisual.Arc(floor,3.6f,facing,CurrentAttack==BossAttack.Slam?73.74f:180,tint);
            else if(CurrentAttack==BossAttack.Mud)
            {
                standardWarning=BloodVisual.Ring(aim+Vector3.up*.08f,2.5f,tint,.07f);
                var path=BloodVisual.Beam(transform.position+Vector3.up*1.7f,aim,tint,.04f);path.transform.SetParent(standardWarning.transform,true);
            }
            else
            {
                standardWarning=BloodVisual.Beam(transform.position+Vector3.up*1.7f,aim,tint,.055f);
                int count=CurrentAttack==BossAttack.Rock?(Evolved?2:1):(Evolved?5:Phase==3?3:1);
                var start=transform.position+Vector3.up*1.7f+facing*1.1f;
                for(int i=0;i<count;i++){var end=start+Quaternion.Euler(0,(i-(count-1)*.5f)*10,0)*(aim-start);var ray=BloodVisual.Beam(start,end,tint,.035f);ray.transform.SetParent(standardWarning.transform,true);}
            }
            Track(standardWarning);
        }
    }
}
