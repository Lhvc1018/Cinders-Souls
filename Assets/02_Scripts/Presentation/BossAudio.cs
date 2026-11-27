using UnityEngine;
using Codice.Bosses;
namespace Codice.Presentation
{
    // State-driven cues cover native and shared animation controllers without duplicate animation events.
    public sealed class BossAudio:MonoBehaviour
    {
        BossCharacter boss;BossState previous;BossAttack prepared;bool entered,phaseCue,lowCue,dead;
        public bool LowCuePlayed=>lowCue;
        void Awake(){boss=GetComponent<BossCharacter>();boss.Died+=OnDeath;}
        void OnDeath(BossCharacter actor){if(dead)return;dead=true;GameAudio.PlayAt(SoundCue.BossDeath,transform.position);}
        void OnDestroy(){if(boss)boss.Died-=OnDeath;}
        void LateUpdate()
        {
            if(!boss||boss.Paused||Time.timeScale<=0)return;
            if(!entered){if(!boss.IsAlive||!boss.Target)return;entered=true;GameAudio.PlayAt(SoundCue.Boss,transform.position);previous=boss.State;}
            if(!boss.IsAlive){if(!dead){dead=true;GameAudio.PlayAt(SoundCue.BossDeath,transform.position);}return;}
            if(boss.Health<=boss.MaxHealth*.3f&&!lowCue){lowCue=true;phaseCue=true;GameAudio.Play(SoundCue.BossLow);}
            else if(boss.Health<=boss.MaxHealth*.65f&&!phaseCue){phaseCue=true;GameAudio.Play(SoundCue.BossPhase);}
            if(boss.State!=previous){
                if(boss.State==BossState.Windup){prepared=boss.CurrentAttack;GameAudio.PlayAt(SoundCue.BossWindup,transform.position);}
                else if(previous==BossState.Windup&&(boss.State==BossState.Recovery||boss.State==BossState.Charging))
                    GameAudio.PlayAt(prepared==BossAttack.Bow||prepared==BossAttack.RadiantRain||prepared==BossAttack.BloodNova?SoundCue.BossCast:SoundCue.BossImpact,transform.position);
                previous=boss.State;
            }
        }
    }
}
