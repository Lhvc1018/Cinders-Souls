using System.Collections.Generic;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.Animations;using UnityEngine;
// World 3 enemy animation audit: every state ActorPresentation cross-fades into must exist, carry a clip that
// actually moves the rig, and loop (locomotion/stun) or play once (attacks/hit/death). Fix() repairs what it can.
public static class AgentCemeteryAudit{
 static string[] Enemies={"Cemetery/HeadSpider","Cemetery/Huntsman","Cemetery/Executioner","Cemetery/StrawReaper","Cemetery/Watcher","HorrorInquisitor","Gurdi del Abismo","Necromancer"};
 static readonly string[] Looping={"Idle","Patrol","Chase","Run","Stun"};
 static readonly string[] OneShot={"Melee","Shoot","Slam","Hit","Death","Summon"};
 static readonly Dictionary<string,string[]> Fallback=new Dictionary<string,string[]>{{"Run",new[]{"Chase"}},{"Patrol",new[]{"Chase","Idle"}},{"Chase",new[]{"Run","Patrol"}},{"Shoot",new[]{"Melee"}},{"Slam",new[]{"Melee"}},{"Summon",new[]{"Shoot","Melee"}},{"Stun",new[]{"Hit","Idle"}},{"Hit",new[]{"Stun"}}};
 public static string Run()=>Go(false);
 public static string Fix()=>Go(true);
 static string Go(bool fix){var sb=new StringBuilder();int problems=0,fixedCount=0;
  foreach(var name in Enemies){var prefab=Resources.Load<GameObject>(name.StartsWith("WorldFive/")?name:"ExpeditionEnemies/"+name);if(!prefab){sb.AppendLine(name+": PREFAB MISSING");problems++;continue;}
   var animator=prefab.GetComponentInChildren<Animator>(true);var ctrl=animator?animator.runtimeAnimatorController as AnimatorController:null;
   if(!ctrl){sb.AppendLine(name+": no AnimatorController ("+(animator?animator.runtimeAnimatorController?.GetType().Name:"no animator")+")");problems++;continue;}
   var sm=ctrl.layers[0].stateMachine;var states=sm.states.ToDictionary(s=>s.state.name,s=>s.state);
   var inst=(GameObject)Object.Instantiate(prefab);inst.hideFlags=HideFlags.HideAndDontSave;var anim=inst.GetComponentInChildren<Animator>(true);var bones=anim.GetComponentsInChildren<Transform>(true);
   var line=new StringBuilder(name+" ["+ctrl.name+"] ");
   foreach(var key in Looping.Concat(OneShot)){
    states.TryGetValue(key,out var st);var clip=st?st.motion as AnimationClip:null;
    if(!st||!clip){
     string alt=Fallback.TryGetValue(key,out var opts)?opts.FirstOrDefault(o=>states.TryGetValue(o,out var s2)&&s2.motion):null;
     if(fix&&alt!=null){if(!st)st=sm.AddState(key);st.motion=states[alt].motion;states[key]=st;clip=st.motion as AnimationClip;fixedCount++;line.Append(key+"←"+alt+" ");}
     else{line.Append(key+":MISSING ");problems++;continue;}}
    if((key=="Slam"||key=="Summon")&&states.TryGetValue("Run",out var runSt)&&clip==runSt.motion){string alt=key=="Slam"?"Melee":"Shoot";if(fix&&states.TryGetValue(alt,out var altSt)&&altSt.motion){st.motion=altSt.motion;clip=st.motion as AnimationClip;fixedCount++;line.Append(key+"←"+alt+"(era Run) ");}else{line.Append(key+"!usesRunClip ");problems++;}}
    // does it move the rig?
    float move=0;var rest=bones.Select(b=>b.localRotation).ToArray();var restPos=bones.Select(b=>b.localPosition).ToArray();
    clip.SampleAnimation(anim.gameObject,0);var a=bones.Select(b=>b.localRotation).ToArray();foreach(float f in new[]{.17f,.33f,.5f,.67f,.83f}){clip.SampleAnimation(anim.gameObject,clip.length*f);
    for(int i=0;i<bones.Length;i++)move=Mathf.Max(move,Quaternion.Angle(a[i],bones[i].localRotation));}float shift=0;var ap=bones.Select(b=>b.localPosition).ToArray();clip.SampleAnimation(anim.gameObject,clip.length*.25f);for(int i=0;i<bones.Length;i++)shift=Mathf.Max(shift,(ap[i]-bones[i].localPosition).magnitude);if(shift>.03f)move=Mathf.Max(move,shift*100);
    for(int i=0;i<bones.Length;i++){bones[i].localRotation=rest[i];bones[i].localPosition=restPos[i];}
    var settings=AnimationUtility.GetAnimationClipSettings(clip);bool wantLoop=Looping.Contains(key);string flag="";
    if(settings.loopTime!=wantLoop&&!(key=="Hit"&&clip==(states.TryGetValue("Stun",out var stun)?stun.motion:null))){
     if(fix&&!AssetDatabase.IsSubAsset(clip)&&AssetDatabase.GetAssetPath(clip).EndsWith(".anim")){settings.loopTime=wantLoop;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);fixedCount++;flag=wantLoop?"→loop":"→once";}
     else{flag=wantLoop?"!noLoop":"!loops";problems++;}}
    if(move<1.5f&&key!="Idle"){flag+="!STATIC";problems++;}
    if(key=="Death"&&st.transitions.Length>0){flag+="!exitTransition";if(fix){foreach(var t in st.transitions.ToArray())st.RemoveTransition(t);fixedCount++;}else problems++;}
    line.Append($"{key}={clip.name}:{clip.length:0.0}s/{move:0}{flag} ");}
   bool speedParam=ctrl.parameters.Any(p=>p.name=="Speed");line.Append(speedParam?"":"(no Speed param) ");
   Object.DestroyImmediate(inst);sb.AppendLine(line.ToString());if(fix)EditorUtility.SetDirty(ctrl);}
  if(fix)AssetDatabase.SaveAssets();
  sb.Insert(0,(fix?"FIX applied="+fixedCount+" ":"")+"problems="+problems+"\n");return sb.ToString();}
 // Slam/Summon on the mixamo residents were copies of the 0.5 s run cycle: point them at real attack clips.
 public static string HumanoidAttacks(){var sb=new StringBuilder();foreach(var n in new[]{"Executioner","StrawReaper","Watcher"}){var c=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/ExpeditionEnemies/Cemetery/"+n+".controller");var st=c.layers[0].stateMachine.states.ToDictionary(x=>x.state.name,x=>x.state);
  st["Slam"].motion=st["Melee"].motion;st["Summon"].motion=st["Shoot"].motion;EditorUtility.SetDirty(c);sb.Append(n+" Slam→Melee Summon→Shoot; ");}AssetDatabase.SaveAssets();return sb.ToString();}
 public static string WorldFive(){var keep=Enemies;Enemies=new[]{"WorldFive/Bosses/Ogro del Foso","WorldFive/Bosses/Rey Trasgo","WorldFive/Bosses/Berserker Orco","WorldFive/Enemies/Bruto","WorldFive/Enemies/Saqueador","WorldFive/Enemies/Chaman","WorldFive/Enemies/Huargo"};try{return Go(true)+"\n---recheck---\n"+Go(false);}finally{Enemies=keep;}}
}
