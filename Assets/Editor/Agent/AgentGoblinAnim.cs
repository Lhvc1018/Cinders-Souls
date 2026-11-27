using System.Collections.Generic;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.Animations;using UnityEngine;
// Rey Trasgo: the Puglin model ships without animation. This authors a full clip set on its UE-style rig by posing
// bones with world-space rotations (character frame, +Z forward) and baking the resulting local rotations.
public static class AgentGoblinAnim{
 const string Prefab="Assets/Resources/WorldFive/Bosses/Rey Trasgo.prefab";const string Dir="Assets/Resources/WorldFive/Bosses/ReyTrasgoAnim/";
 delegate void Pose(float t,Dictionary<string,Quaternion> rot,ref Vector3 lift);
 static Quaternion X(float a)=>Quaternion.AngleAxis(a,Vector3.right);static Quaternion Y(float a)=>Quaternion.AngleAxis(a,Vector3.up);static Quaternion Z(float a)=>Quaternion.AngleAxis(a,Vector3.forward);
 static float S(float t,float c=1)=>Mathf.Sin(t*Mathf.PI*2*c);static float E(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
 static readonly string[] Order={"root","pelvis","spine_01","spine_02","spine_03","neck_01","Head","clavicle_l","upperarm_l","lowerarm_l","hand_l","clavicle_r","upperarm_r","lowerarm_r","hand_r","thigh_l","calf_l","foot_l","thigh_r","calf_r","foot_r"};
 // Arms come down from the T-pose and elbows bend forward; every pose starts from this stance.
 static void Stance(Dictionary<string,Quaternion> r,float armSwing=0,float crouch=0){
  r["upperarm_l"]=X(-armSwing)*Z(68);r["upperarm_r"]=X(armSwing)*Z(-68);r["lowerarm_l"]=X(-35);r["lowerarm_r"]=X(-45);
  if(crouch>0){r["thigh_l"]=X(-crouch);r["thigh_r"]=X(-crouch);r["calf_l"]=X(crouch*1.8f);r["calf_r"]=X(crouch*1.8f);r["foot_l"]=X(-crouch*.8f);r["foot_r"]=X(-crouch*.8f);}}
 static AnimationClip Bake(string name,float len,bool loop,Pose pose,Transform model,Dictionary<string,Transform> bones,Dictionary<Transform,Quaternion> rest,Vector3 rootRest,int samples=30){
  string path=Dir+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.ClearCurves();
  var keys=bones.Values.ToDictionary(b=>b,b=>new List<Keyframe>[4]{new List<Keyframe>(),new List<Keyframe>(),new List<Keyframe>(),new List<Keyframe>()});var pos=new List<Keyframe>[3]{new List<Keyframe>(),new List<Keyframe>(),new List<Keyframe>()};
  var root=bones["root"];
  for(int i=0;i<=samples;i++){float t=i/(float)samples,time=len*t;foreach(var kv in rest)kv.Key.localRotation=kv.Value;root.localPosition=rootRest;
   var r=new Dictionary<string,Quaternion>();Vector3 lift=Vector3.zero;pose(t,r,ref lift);
   foreach(var n in Order)if(r.TryGetValue(n,out var q)&&bones.TryGetValue(n,out var b))b.rotation=q*b.rotation;
   root.localPosition=rootRest+root.parent.InverseTransformVector(lift);
   foreach(var kv in keys){var q=kv.Key.localRotation;kv.Value[0].Add(new Keyframe(time,q.x));kv.Value[1].Add(new Keyframe(time,q.y));kv.Value[2].Add(new Keyframe(time,q.z));kv.Value[3].Add(new Keyframe(time,q.w));}
   var lp=root.localPosition;pos[0].Add(new Keyframe(time,lp.x));pos[1].Add(new Keyframe(time,lp.y));pos[2].Add(new Keyframe(time,lp.z));}
  string[] c={"x","y","z","w"};
  foreach(var kv in keys){string p=AnimationUtility.CalculateTransformPath(kv.Key,model);for(int a=0;a<4;a++)clip.SetCurve(p,typeof(Transform),"m_LocalRotation."+c[a],new AnimationCurve(kv.Value[a].ToArray()));}
  string rp=AnimationUtility.CalculateTransformPath(root,model);for(int a=0;a<3;a++)clip.SetCurve(rp,typeof(Transform),"m_LocalPosition."+c[a],new AnimationCurve(pos[a].ToArray()));
  clip.EnsureQuaternionContinuity();var s=AnimationUtility.GetAnimationClipSettings(clip);s.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,s);EditorUtility.SetDirty(clip);
  foreach(var kv in rest)kv.Key.localRotation=kv.Value;root.localPosition=rootRest;return clip;}
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";System.IO.Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
  var go=PrefabUtility.LoadPrefabContents(Prefab);var sb=new StringBuilder();
  try{
   var model=go.GetComponentsInChildren<Transform>(true).First(t=>t.Find("Armature"));
   var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>Order.Contains(t.name)).ToDictionary(t=>t.name,t=>t);
   var rest=bones.Values.ToDictionary(b=>b,b=>b.localRotation);var rootRest=bones["root"].localPosition;
   var clips=new Dictionary<string,AnimationClip>();
   void Add(string n,float len,bool loop,Pose p)=>clips[n]=Bake(n,len,loop,p,model,bones,rest,rootRest);
   Add("Idle",2.6f,true,(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{Stance(r,S(t)*4,4);r["spine_02"]=X(S(t)*3);r["Head"]=Y(S(t,.5f)*14)*X(S(t)*4);r["pelvis"]=Z(S(t,.5f)*2);lift=Vector3.up*(-.02f+.025f*S(t));});
   Pose waddle(float pace)=>(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{float s=S(t);Stance(r,-s*22*pace,6);
    r["thigh_l"]=X(-6-s*26*pace);r["thigh_r"]=X(-6+s*26*pace);r["calf_l"]=X(10+Mathf.Max(0,-s)*35*pace);r["calf_r"]=X(10+Mathf.Max(0,s)*35*pace);
    r["pelvis"]=Z(s*9*pace)*Y(s*6);r["spine_02"]=Z(-s*6*pace)*X(6*pace);r["Head"]=Z(-s*5);lift=Vector3.up*(.06f*Mathf.Abs(S(t,1))*pace-.03f);};
   Add("Patrol",1.3f,true,waddle(.7f));Add("Chase",.85f,true,waddle(1f));Add("Run",.7f,true,waddle(1.25f));Add("Movement",.85f,true,waddle(1f));
   // Rock / mud throw: right arm wound back overhead, hurled forward with the whole belly.
   Pose throwing(float heavy)=>(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{Stance(r,0,6+8*E(t,.45f,.6f));float wind=E(t,0,.45f)*(1-E(t,.45f,.6f)),hurl=E(t,.45f,.6f)*(1-E(t,.75f,1));
    r["upperarm_r"]=X(150*wind-70*hurl)*Z(-68);r["lowerarm_r"]=X(-45-30*wind);r["upperarm_l"]=X(-30*wind+20*hurl)*Z(60);
    r["spine_01"]=X(-12*wind+22*hurl*heavy)*Y(-20*wind+25*hurl);r["Head"]=X(-10*wind+8*hurl);lift=Vector3.up*(.05f*wind-.08f*hurl);};
   Add("Shoot",1.1f,false,throwing(1));Add("ShootMagic",1.1f,false,throwing(1));Add("Melee",1.2f,false,throwing(1.4f));Add("HeavyAttack",1.4f,false,throwing(1.6f));
   Add("Slam",1.5f,false,(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{float up=E(t,0,.45f)*(1-E(t,.5f,.62f)),down=E(t,.5f,.62f)*(1-E(t,.8f,1));Stance(r,0,8+22*down);
    r["upperarm_l"]=X(-(160*up-40*down))*Z(60);r["upperarm_r"]=X(160*up-40*down)*Z(-60);r["spine_01"]=X(-14*up+30*down);r["Head"]=X(-12*up+10*down);lift=Vector3.up*(.12f*up-.18f*down);});
   Add("Summon",1.6f,false,(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{float k=E(t,0,.35f)*(1-E(t,.8f,1));Stance(r,0,4);r["upperarm_l"]=X(-120*k)*Z(68-40*k);r["upperarm_r"]=X(120*k)*Z(-68+40*k);r["spine_02"]=X(-14*k);r["Head"]=X(-25*k)*Z(S(t,6)*6*k);lift=Vector3.up*.04f*k;});
   Add("Hit",.45f,false,(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{float k=Mathf.Sin(t*Mathf.PI)*(1-t*.3f);Stance(r,-15*k,4);r["spine_01"]=X(-16*k)*Z(6*k);r["Head"]=X(-22*k);lift=new Vector3(0,.02f*k,-.15f*k);});
   Add("Stun",1.4f,true,(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{Stance(r,S(t,2)*8,10);r["spine_01"]=X(18)*Z(S(t)*8);r["Head"]=X(25)*Z(S(t)*14)*Y(S(t,.5f)*12);r["lowerarm_l"]=X(-10);r["lowerarm_r"]=X(-10);lift=Vector3.down*.06f;});
   Add("Death",1.6f,false,(float t,Dictionary<string,Quaternion> r,ref Vector3 lift)=>{float k=E(t,0,.75f);Stance(r,0,14*(1-k));r["root"]=X(-82*k);r["upperarm_l"]=X(-60*k)*Z(68-30*k);r["upperarm_r"]=X(60*k)*Z(-68+30*k);r["Head"]=X(-20*k);r["thigh_l"]=X(-30*k);r["thigh_r"]=X(-20*k);lift=new Vector3(0,.05f*Mathf.Sin(k*Mathf.PI),-.4f*k);});
   foreach(var alias in new[]{"Ready","BlockIdle","BowIdle","RifleIdle"})clips[alias]=clips["Idle"];clips["Dash"]=clips["Run"];clips["Thrust"]=clips["Melee"];clips["ShootBow"]=clips["Shoot"];
   // controller: same state names the goblin controller exposes, crossfaded by ActorPresentation
   string cp=Dir+"Rey Trasgo.controller";var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp)??AnimatorController.CreateAnimatorControllerAtPath(cp);
   var sm=ctrl.layers[0].stateMachine;foreach(var st in sm.states.ToArray())sm.RemoveState(st.state);
   foreach(var p in new[]{"Speed","ActionSpeed"})if(!ctrl.parameters.Any(x=>x.name==p))ctrl.AddParameter(p,AnimatorControllerParameterType.Float);
   foreach(var kv in clips){var st=sm.AddState(kv.Key);st.motion=kv.Value;if(kv.Key=="Idle")sm.defaultState=st;}
   EditorUtility.SetDirty(ctrl);
   var animator=model.GetComponent<Animator>();if(!animator)animator=model.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=ctrl;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var boss=go.GetComponent<Codice.Bosses.BossCharacter>();boss.useNativeAnimator=true;
   PrefabUtility.SaveAsPrefabAsset(go,Prefab);sb.Append("clips="+clips.Values.Distinct().Count()+" states="+clips.Count+" bones="+bones.Count);
  }finally{PrefabUtility.UnloadPrefabContents(go);}
  AssetDatabase.SaveAssets();return sb.ToString();}
}
