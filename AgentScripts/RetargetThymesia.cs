using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class RetargetThymesia
{
 const string Source="D:/67/Analysis/Thymesia/Exports";
 const string Output="Assets/07_Animacion/Thymesia";
 class Bone { public string name; public int parent; public Vector3 pos; public Quaternion rot; }
 class Psa { public Bone[] bones; public Vector3[] pos; public Quaternion[] rot; public int frames; public float fps; }
 static string Text(BinaryReader r,int n)=>Encoding.ASCII.GetString(r.ReadBytes(n)).TrimEnd('\0');
 static Vector3 V(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 static Quaternion Q(BinaryReader r)=>new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 static Vector3 Convert(Vector3 p)=>new Vector3(-p.x,p.z,p.y)*.01f;
 // CUE4Parse BONENAMES use raw UE transforms; ANIMKEYS mirror Y and negate root W.
 // Undo that export convention before the cyclic UE -> Unity axis conversion.
 static Quaternion Convert(Quaternion q,bool root){return new Quaternion(-q.x,q.z,q.y,q.w).normalized;}
 static Psa Read(string path){
  var p=new Psa();using(var r=new BinaryReader(File.OpenRead(path))){while(r.BaseStream.Position<r.BaseStream.Length){
   string id=Text(r,20);r.ReadInt32();int size=r.ReadInt32(),count=r.ReadInt32();long end=r.BaseStream.Position+(long)size*count;
   if(id=="BONENAMES"){p.bones=new Bone[count];for(int i=0;i<count;i++){long next=r.BaseStream.Position+size;var b=new Bone{name=Text(r,64)};r.ReadInt32();r.ReadInt32();b.parent=r.ReadInt32();b.rot=Convert(Q(r),i==0);b.pos=Convert(V(r));p.bones[i]=b;r.BaseStream.Position=next;}}
   if(id=="ANIMINFO"){r.BaseStream.Position+=128+16+8;p.fps=r.ReadSingle();r.ReadInt32();r.ReadInt32();p.frames=r.ReadInt32();}
   if(id=="ANIMKEYS"){p.pos=new Vector3[count];p.rot=new Quaternion[count];for(int i=0;i<count;i++){var pos=V(r);pos.y=-pos.y;p.pos[i]=Convert(pos);var q=Q(r);q.y=-q.y;if(i%p.bones.Length==0)q.w=-q.w;p.rot[i]=Convert(q,false);r.ReadSingle();}}
   r.BaseStream.Position=end;
  }}return p;
 }
 static Dictionary<string,string> Map(){
  var m=new Dictionary<string,string>{{"Hips","Bip001-Pelvis"},{"Spine","Bip001-Spine"},{"Chest","Bip001-Spine1"},{"UpperChest","Bip001-Spine2"},{"Neck","Bip001-Neck"},{"Head","Bip001-Head"}};
  foreach(var pair in new[]{("Left","L"),("Right","R")}){
   string side=pair.Item1,p="Bip001-"+pair.Item2+"-";
   foreach(var bone in new[]{("UpperLeg","Thigh"),("LowerLeg","Calf"),("Foot","Foot"),("Toes","Toe0"),("Shoulder","Clavicle"),("UpperArm","UpperArm"),("LowerArm","Forearm"),("Hand","Hand")})m[side+bone.Item1]=p+bone.Item2;
   var fingers=new[]{"Thumb","Index","Middle","Ring","Little"};
   for(int f=0;f<5;f++)for(int j=0;j<3;j++)m[side+fingers[f]+new[]{"Proximal","Intermediate","Distal"}[j]]=p+"Finger"+f+(j==0?"":j.ToString());
  }return m;
 }
 static GameObject Skeleton(Psa p,out Transform[] bones){
  var go=new GameObject("LordsSource");bones=new Transform[p.bones.Length];
  for(int i=0;i<bones.Length;i++){var b=p.bones[i];var t=new GameObject(b.name).transform;t.SetParent(b.parent>=0&&b.parent<i?bones[b.parent]:go.transform,false);t.localPosition=b.pos;t.localRotation=b.rot;bones[i]=t;}return go;
 }
 static void Align(Transform a,Transform b,Vector3 direction){a.rotation=Quaternion.FromToRotation(b.position-a.position,direction)*a.rotation;}
 static Avatar Avatar(GameObject root,Transform[] bones){
  var map=Map();var lookup=bones.ToDictionary(t=>t.name);Transform B(string key)=>lookup[map[key]];
  foreach(string side in new[]{"Left","Right"}){
   var dir=side=="Left"?Vector3.left:Vector3.right;
   Align(B(side+"UpperArm"),B(side+"LowerArm"),dir);Align(B(side+"LowerArm"),B(side+"Hand"),dir);
   Align(B(side+"Hand"),B(side+"MiddleProximal"),dir);
   Align(B(side+"UpperLeg"),B(side+"LowerLeg"),Vector3.down);Align(B(side+"LowerLeg"),B(side+"Foot"),Vector3.down);
   var toe=B(side+"Toes");Align(B(side+"Foot"),toe,new Vector3(0,-.15f,1));
  }
  var description=new HumanDescription{
   human=map.Select(kv=>new HumanBone{humanName=HumanTrait.BoneName[(int)Enum.Parse(typeof(HumanBodyBones),kv.Key)],boneName=kv.Value,limit=new HumanLimit{useDefaultValues=true}}).ToArray(),
   skeleton=root.GetComponentsInChildren<Transform>().Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
   upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=0,legStretch=0,feetSpacing=0,hasTranslationDoF=false};
  var avatar=AvatarBuilder.BuildHumanAvatar(root,description);if(!avatar.isValid||!avatar.isHuman)throw new Exception("Invalid source humanoid avatar");return avatar;
 }
 static string MuscleName(string name){foreach(string side in new[]{"Left","Right"})foreach(string digit in new[]{"Thumb","Index","Middle","Ring","Little"}){string prefix=side+" "+digit+" ";if(name.StartsWith(prefix))return side+"Hand."+digit+"."+name.Substring(prefix.Length);}return name;}
 static void Curve(AnimationClip clip,string name,Keyframe[] keys){
  // Simplify sampled linear curves to 0.001 muscle units / 0.0002 quaternion units.
  float tolerance=name.StartsWith("RootQ")?.0002f:.001f;
  var keep=new bool[keys.Length];keep[0]=keep[keys.Length-1]=true;
  var ranges=new Stack<(int,int)>();ranges.Push((0,keys.Length-1));
  while(ranges.Count>0){var range=ranges.Pop();int a=range.Item1,b=range.Item2,index=-1;float max=tolerance,dt=keys[b].time-keys[a].time;
   if(dt<=0)continue;for(int i=a+1;i<b;i++){float expected=Mathf.Lerp(keys[a].value,keys[b].value,(keys[i].time-keys[a].time)/dt);float error=Mathf.Abs(keys[i].value-expected);if(error>max){max=error;index=i;}}
   if(index>=0){keep[index]=true;ranges.Push((a,index));ranges.Push((index,b));}
  }
  var reduced=keys.Where((k,i)=>keep[i]).ToArray();
  for(int i=0;i<reduced.Length;i++){var k=reduced[i];k.inTangent=i>0?(k.value-reduced[i-1].value)/(k.time-reduced[i-1].time):0;k.outTangent=i+1<reduced.Length?(reduced[i+1].value-k.value)/(reduced[i+1].time-k.time):0;reduced[i]=k;}
  AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),new AnimationCurve(reduced));
 }
 public static string Diagnose(){var p=Read(Directory.GetFiles(Source,"Corvus_SD_Defense_Loop.psa",SearchOption.AllDirectories)[0]);var go=Skeleton(p,out var bones);try{string output="";foreach(bool animated in new[]{false,true}){if(animated)for(int i=0;i<bones.Length;i++){bones[i].localPosition=p.pos[i];bones[i].localRotation=p.rot[i];}output+=(animated?"ANIM ":"BIND ")+string.Join(";",Map().Where(kv=>!kv.Key.Contains("Thumb")&&!kv.Key.Contains("Index")&&!kv.Key.Contains("Middle")&&!kv.Key.Contains("Ring")&&!kv.Key.Contains("Little")).Select(kv=>kv.Key+"="+bones.First(t=>t.name==kv.Value).position))+"\n";}return output;}finally{UnityEngine.Object.DestroyImmediate(go);}}
 public static System.Threading.Tasks.Task<string> Selected()=>Build(true);
 public static System.Threading.Tasks.Task<string> All()=>Build(false);
 public static async System.Threading.Tasks.Task<string> Build(bool selected=false){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before retargeting");
  Directory.CreateDirectory(Output);AssetDatabase.Refresh();int total=0;var report=new StringBuilder();
  foreach(string path in Directory.GetFiles(Source,"*.psa",SearchOption.AllDirectories).OrderBy(x=>x)){
   if(selected&&!File.ReadAllText("AgentScripts/InstallThymesia.cs").Contains(Path.GetFileNameWithoutExtension(path))&&!Path.GetFileNameWithoutExtension(path).StartsWith("Corvus_SD_Run"))continue;
   var p=Read(path);if(p.frames<2||!Map().Values.All(n=>p.bones.Any(b=>b.name==n))){report.AppendLine("SKIP incompatible skeleton: "+path);continue;}var go=Skeleton(p,out var bones);Avatar avatar=null;GameObject target=null;
   try{
    // Preserve the authored A-pose. A T-pose offset applied to A-pose animation raises both arms incorrectly.
    target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Presentation/Classes/Warrior.prefab"));
    target.transform.position=Vector3.zero;target.transform.rotation=Quaternion.identity;target.transform.localScale=Vector3.one;
    var animator=target.GetComponentInChildren<Animator>();animator.enabled=false;animator.applyRootMotion=false;
    var targetBones=target.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
    foreach(var bone in animator.avatar.humanDescription.skeleton)if(targetBones.TryGetValue(bone.name,out var t)){t.localPosition=bone.position;t.localRotation=bone.rotation;t.localScale=bone.scale;}
    var sourceMap=Map();
    Transform Src(string key)=>bones.First(t=>t.name==sourceMap[key]);
    Transform Dst(string key)=>animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),key));
    foreach(string side in new[]{"Left","Right"})foreach(var chain in new[]{("UpperArm","LowerArm"),("LowerArm","Hand"),("Hand","MiddleProximal"),("UpperLeg","LowerLeg"),("LowerLeg","Foot"),("Foot","Toes")}){
     var a=Dst(side+chain.Item1);var b=Dst(side+chain.Item2);
     if(a&&b)Align(a,b,Src(side+chain.Item2).position-Src(side+chain.Item1).position);
    }
    var map=Map().Where(kv=>animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),kv.Key))!=null).OrderBy(kv=>Array.FindIndex(bones,t=>t.name==kv.Value)).ToArray();
    var sourceBones=map.Select(kv=>bones.First(t=>t.name==kv.Value)).ToArray();
    var destinationBones=map.Select(kv=>animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),kv.Key))).ToArray();
    var offsets=sourceBones.Select((t,i)=>Quaternion.Inverse(t.rotation)*destinationBones[i].rotation).ToArray();
    Vector3 sourceHips=sourceBones[0].position,targetHips=destinationBones[0].position;float scale=targetHips.y/sourceHips.y;
    var leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
    float groundFoot=Mathf.Min(leftFoot.position.y,rightFoot.position.y);
    var clip=new AnimationClip{name=Path.GetFileNameWithoutExtension(path),frameRate=60};
    int samples=p.frames;var curves=new Keyframe[HumanTrait.MuscleCount+7][];for(int j=0;j<curves.Length;j++)curves[j]=new Keyframe[samples];
    using(var handler=new HumanPoseHandler(animator.avatar,animator.transform)){
     var pose=new HumanPose();Quaternion last=Quaternion.identity;float maxMuscle=0;
     for(int f=0;f<samples;f++){
      for(int b=0;b<bones.Length;b++){int k=f*bones.Length+b;bones[b].localPosition=p.pos[k];bones[b].localRotation=p.rot[k];}
      for(int b=0;b<map.Length;b++)destinationBones[b].rotation=sourceBones[b].rotation*offsets[b];
      if(f==0&&path.Contains("Defense_Loop"))Debug.Log("[BindDiagnostic] " + string.Join(";",map.Select((kv,i)=>kv.Key+" src="+sourceBones[i].position+" dst="+destinationBones[i].position).Where(x=>x.Contains("Arm")||x.Contains("Hand")||x.Contains("Head"))));
      destinationBones[0].position=targetHips+(sourceBones[0].position-sourceHips-bones[0].position)*scale;
      handler.GetHumanPose(ref pose);float time=f/p.fps;
      bool roll=path.Contains("Avoid")||path.Contains("Fall")||path.Contains("Death");
      // Retarget the performance to our shorter armoured rig without transferring extreme spine compression.
      for(int j=0;j<pose.muscles.Length;j++){
       string muscle=HumanTrait.MuscleName[j];float limit=1;
       if(!roll&&(muscle.StartsWith("Spine ")||muscle.StartsWith("Chest ")||muscle.StartsWith("UpperChest "))&&!muscle.Contains("Twist"))limit=.28f;
       pose.muscles[j]=Mathf.Clamp(pose.muscles[j],-limit,limit);
      }
      if(!roll){
       Vector3 forward=Vector3.ProjectOnPlane(pose.bodyRotation*Vector3.forward,Vector3.up);
       Quaternion yaw=forward.sqrMagnitude>.01f?Quaternion.LookRotation(forward):Quaternion.identity;
       pose.bodyRotation=yaw*Quaternion.RotateTowards(Quaternion.identity,Quaternion.Inverse(yaw)*pose.bodyRotation,16);
       handler.SetHumanPose(ref pose);
       if(f==0&&path.Contains("Defense_Loop"))Debug.Log("[PoseDiagnostic] " + string.Join(";",map.Select((kv,i)=>kv.Key+"="+destinationBones[i].position).Where(x=>x.Contains("Arm")||x.Contains("Hand")||x.Contains("Head")))+" muscles="+string.Join(";",pose.muscles.Select((v,i)=>HumanTrait.MuscleName[i]+"="+v).Where(x=>x.Contains("Arm")||x.Contains("Forearm"))));
       pose.bodyPosition.y+=(groundFoot-Mathf.Min(leftFoot.position.y,rightFoot.position.y))/animator.humanScale;
      }
      for(int j=0;j<pose.muscles.Length;j++){float value=pose.muscles[j];if(float.IsNaN(value)||float.IsInfinity(value))throw new Exception("Non-finite muscle");maxMuscle=Mathf.Max(maxMuscle,Mathf.Abs(value));curves[j][f]=new Keyframe(time,value);}
      // Keep pelvis sway, but remove authored traversal. The CharacterController owns world motion.
      Vector3 body=pose.bodyPosition;
      Quaternion q=pose.bodyRotation;if(f>0&&Quaternion.Dot(last,q)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);last=q;
      float[] vals={body.x,body.y,body.z,q.x,q.y,q.z,q.w};for(int j=0;j<7;j++)curves[HumanTrait.MuscleCount+j][f]=new Keyframe(time,vals[j]);
     }
     report.AppendLine(clip.name+" frames="+samples+" duration="+((samples-1)/p.fps)+" maxMuscle="+maxMuscle);
    }
    for(int j=0;j<HumanTrait.MuscleCount;j++)Curve(clip,MuscleName(HumanTrait.MuscleName[j]),curves[j]);
    string[] roots={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};for(int j=0;j<7;j++)Curve(clip,roots[j],curves[HumanTrait.MuscleCount+j]);
    bool loop=clip.name.Contains("Run")||clip.name.EndsWith("Idle")||clip.name.EndsWith("Loop")||clip.name.EndsWith("Sprint");var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;settings.keepOriginalOrientation=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;settings.startTime=0;settings.stopTime=(p.frames-1)/p.fps;AnimationUtility.SetAnimationClipSettings(clip,settings);
    string dest=Output+"/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,dest);
    total++;if(total%10==0){AssetDatabase.SaveAssets();Debug.Log("[ThymesiaRetarget] "+total+" clips");}
   }finally{if(target)UnityEngine.Object.DestroyImmediate(target);if(avatar)UnityEngine.Object.DestroyImmediate(avatar);UnityEngine.Object.DestroyImmediate(go);}
   await System.Threading.Tasks.Task.Delay(1);
  }
  AssetDatabase.SaveAssets();File.WriteAllText("D:/67/Analysis/Thymesia/unity-retarget.txt",report.ToString());return "Converted "+total+" full-body humanoid clips. See D:/67/Analysis/Thymesia/unity-retarget.txt";
 }
}








