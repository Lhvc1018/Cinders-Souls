using UnityEngine;

namespace Codice.Multiplayer
{
    internal sealed class CoopPoseBuffer
    {
        const float Delay=.120f, Horizon=.150f;
        struct Sample { public float time;public Vector3 position;public Quaternion rotation; }
        readonly Sample[] samples=new Sample[12];
        int head,count;
        float playhead,arrival;
        Sample At(int i)=>samples[(head+i)%samples.Length];

        public bool Add(float time,Vector3 position,Quaternion rotation,out bool reset)
        {
            var previous=count>0?At(count-1):default;
            reset=count==0||time-previous.time>1f||(position-previous.position).sqrMagnitude>36f;
            if(count>0&&time<previous.time)return false;
            if(reset){head=count=0;playhead=time-Delay;}
            if(count>0&&previous.time==time)count--;
            if(count==samples.Length){head=(head+1)%samples.Length;count--;}
            samples[(head+count++)%samples.Length]=new Sample{time=time,position=position,rotation=rotation};
            arrival=Time.unscaledTime;
            return true;
        }

        public bool Read(out Vector3 position,out Quaternion rotation)
        {
            position=default;rotation=Quaternion.identity;
            if(count<2)return false;
            var latest=At(count-1);
            float expected=latest.time+Mathf.Min(Time.unscaledTime-arrival,Horizon)-Delay;
            float rate=Mathf.Clamp(1f+(expected-playhead)*2f,.90f,1.12f);
            playhead=Mathf.Min(latest.time+Horizon,playhead+Time.unscaledDeltaTime*rate);
            while(count>2&&At(1).time<=playhead){head=(head+1)%samples.Length;count--;}
            var a=At(0);var b=At(1);
            float t=Mathf.InverseLerp(a.time,b.time,playhead);
            position=Vector3.Lerp(a.position,b.position,t);rotation=Quaternion.Slerp(a.rotation,b.rotation,t);
            if(playhead>b.time&&b.time-a.time>.001f&&b.time-a.time<.25f)
                position+=Vector3.ClampMagnitude((b.position-a.position)/(b.time-a.time),35f)*Mathf.Min(Horizon,playhead-b.time);
            return true;
        }
    }
}
