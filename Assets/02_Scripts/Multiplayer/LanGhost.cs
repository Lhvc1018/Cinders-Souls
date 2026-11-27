using UnityEngine;
namespace Codice.Multiplayer
{
    public sealed class LanGhost : MonoBehaviour
    {
        public LanEntity snapshot;
        Animator animator;string state;float previousActionLeft;
        readonly CoopPoseBuffer motion=new CoopPoseBuffer();
        float receivedAt,receivedSample;
        public void Apply(LanEntity value,bool first,float sampleTime)
        {
            if(!motion.Add(sampleTime,value.position,value.rotation,out var reset))return;
            receivedAt=Time.unscaledTime;receivedSample=sampleTime;
            if(reset)transform.SetPositionAndRotation(value.position,value.rotation);
            snapshot=value;if(value.key.StartsWith("Player:")){var equipment=GetComponent<LanHeldWeapon>()??gameObject.AddComponent<LanHeldWeapon>();equipment.Apply(value.weapon);}if(value.key=="Line"){LanSession.ApplyLine(GetComponent<LineRenderer>(),value);}if(value.key=="Beam"){var line=GetComponent<LineRenderer>();line.SetPosition(0,value.position);line.SetPosition(1,value.end);}if(first)transform.SetPositionAndRotation(value.position,value.rotation);
            if(!animator)animator=GetComponentInChildren<Animator>();
            if(animator&&animator.runtimeAnimatorController&&!string.IsNullOrEmpty(value.animation)&&(state!=value.animation||value.animLeft>previousActionLeft+.08f)&&animator.HasState(0,Animator.StringToHash(value.animation)))
            {state=value.animation;float elapsed=value.animDur>0?Mathf.Clamp(value.animDur-value.animLeft,0,value.animDur):0;animator.CrossFadeInFixedTime(state,.05f,0,elapsed);}
            previousActionLeft=value.animLeft;
        }
        void Update()
        {
            if(snapshot!=null&&snapshot.flightTime>0&&snapshot.key!="Line"){
                float age=Mathf.Max(0,receivedSample-snapshot.flightTime+Time.unscaledTime-receivedAt);
                transform.SetPositionAndRotation(snapshot.position+snapshot.end*age,snapshot.rotation);
            }
            else if(motion.Read(out var position,out var rotation))transform.SetPositionAndRotation(position,rotation);
        }
        void OnGUI()
        {
            if(snapshot==null||snapshot.maxHealth<=0||snapshot.health>=snapshot.maxHealth||!Camera.main)return;
            var p=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*2.6f);
            if(p.z<=0||p.z>35)return;
            var old=GUI.color;GUI.color=new Color(.1f,.08f,.07f);GUI.DrawTexture(new Rect(p.x-30,Screen.height-p.y,60,4),Texture2D.whiteTexture);GUI.color=new Color(.6f,.13f,.18f);GUI.DrawTexture(new Rect(p.x-30,Screen.height-p.y,60*Mathf.Clamp01(snapshot.health/snapshot.maxHealth),4),Texture2D.whiteTexture);GUI.color=old;
        }
    }
}
