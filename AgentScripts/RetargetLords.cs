using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class RetargetLords
{
 const string Source="D:/67/Analysis/LordsOfTheFallen/Exports";
 const string Output="Assets/Resources/Presentation/Lords";
 class Bone { public string name; public int parent; public Vector3 pos; public Quaternion rot; }
 class Psa { public Bone[] bones; public Vector3[] pos; public Quaternion[] rot; public int frames; public float fps; }
 static string Text(BinaryReader r,int n)=>Encoding.ASCII.GetString(r.ReadBytes(n)).TrimEnd('\0');
 static Vector3 V(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 static Quaternion Q(BinaryReader r)=>new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 static Vector3 Convert(Vector3 p)=>new Vector3(p.y,p.z,p.x)*.01f;
 // CUE4Parse BONENAMES use raw UE transforms; ANIMKEYS mirror Y and negate root W.
 // Undo that export convention before the cyclic UE -> Unity axis conversion.
 static Quaternion Convert(Quaternion q,bool root){return new Quaternion(q.y,q.z,q.x,q.w).normalized;}
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
  var m=new Dictionary<string,string>{{"Hips","spine_pelvis_jnt"},{"Spine","spine_b_jnt"},{"Chest","spine_d_jnt"},{"UpperChest","spine_e_jnt"},{"Neck","neck_a_jnt"},{"Head","head_jnt"}};
  foreach(var pair in new[]{("Left","l"),("Right","r")}){
   string side=pair.Item1,p=pair.Item2;
   foreach(var b in new[]{("UpperLeg","leg_hip"),("LowerLeg","leg_knee"),("Foot","leg_ankle"),("Toes","leg_tarsalA"),("Shoulder","arm_clavicle"),("UpperArm","arm_shoulder"),("LowerArm","arm_elbow"),("Hand","arm_wrist")})m[side+b.Item1]=p+"_"+b.Item2+"_jnt";
   foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Little"})for(int j=0;j<3;j++)m[side+finger+new[]{"Proximal","Intermediate","Distal"}[j]]=p+"_arm_finger"+(finger=="Little"?"Pinky":finger)+(char)('A'+j)+"_jnt";
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
 public static string Probe(){var p=Read(Directory.GetFiles(Source,"AS_GENE_Run_Fnt.psa",SearchOption.AllDirectories)[0]);var go=Skeleton(p,out var bones);Avatar avatar=null;try{
  string result=string.Join("; ",new[]{"spine_pelvis_jnt","head_jnt","l_arm_wrist_jnt","r_arm_wrist_jnt","l_leg_ankle_jnt"}.Select(n=>n+"="+bones.First(t=>t.name==n).position));
  avatar=Avatar(go,bones);using(var handler=new HumanPoseHandler(avatar,go.transform)){var pose=new HumanPose();for(int i=0;i<bones.Length;i++){bones[i].localPosition=p.pos[i];bones[i].localRotation=p.rot[i];}result+=" animated="+string.Join(";",new[]{"head_jnt","l_leg_ankle_jnt","r_leg_ankle_jnt","l_arm_wrist_jnt","r_arm_wrist_jnt"}.Select(n=>n+"="+bones.First(t=>t.name==n).position));handler.GetHumanPose(ref pose);result+=" large="+string.Join(";",pose.muscles.Select((v,i)=>HumanTrait.MuscleName[i]+"="+v).Where((s,i)=>Mathf.Abs(pose.muscles[i])>1||s.Contains("Arm ")||s.Contains("Forearm")));result+=" valid="+avatar.isValid+" scale="+avatar.humanDescription.human.Length+" root="+pose.bodyPosition+" muscles="+pose.muscles.Min()+".."+pose.muscles.Max();}return result;
 }finally{if(avatar)UnityEngine.Object.DestroyImmediate(avatar);UnityEngine.Object.DestroyImmediate(go);}}
 static string MuscleName(string name){foreach(string side in new[]{"Left","Right"})foreach(string digit in new[]{"Thumb","Index","Middle","Ring","Little"}){string prefix=side+" "+digit+" ";if(name.StartsWith(prefix))return side+"Hand."+digit+"."+name.Substring(prefix.Length);}return name;}
 static void Curve(AnimationClip clip,string name,Keyframe[] keys){var c=new AnimationCurve(keys);for(int i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(c,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(c,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),c);}
 public static string Build(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before retargeting");
  Directory.CreateDirectory(Output);AssetDatabase.Refresh();int total=0;var report=new StringBuilder();
  foreach(string path in Directory.GetFiles(Source,"*.psa",SearchOption.AllDirectories)){
   var p=Read(path);var go=Skeleton(p,out var bones);Avatar avatar=null;GameObject target=null;
   try{
    avatar=Avatar(go,bones);
    target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Presentation/Classes/Warrior.prefab"));
    target.transform.position=Vector3.zero;target.transform.rotation=Quaternion.identity;target.transform.localScale=Vector3.one;
    var animator=target.GetComponentInChildren<Animator>();animator.enabled=false;animator.applyRootMotion=false;
    var targetBones=target.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
    foreach(var bone in animator.avatar.humanDescription.skeleton)if(targetBones.TryGetValue(bone.name,out var t)){t.localPosition=bone.position;t.localRotation=bone.rotation;t.localScale=bone.scale;}
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
      destinationBones[0].position=targetHips+(sourceBones[0].position-sourceHips-bones[0].position)*scale;
      handler.GetHumanPose(ref pose);float time=f/p.fps;
      bool roll=path.Contains("DiveRoll");
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
    bool loop=clip.name.StartsWith("AS_GENE_Run_");var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;settings.keepOriginalOrientation=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;settings.startTime=0;settings.stopTime=(p.frames-1)/p.fps;AnimationUtility.SetAnimationClipSettings(clip,settings);
    string dest=Output+"/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,dest);
    total++;
   }finally{if(target)UnityEngine.Object.DestroyImmediate(target);if(avatar)UnityEngine.Object.DestroyImmediate(avatar);UnityEngine.Object.DestroyImmediate(go);}
  }
  AssetDatabase.SaveAssets();File.WriteAllText("D:/67/Analysis/LordsOfTheFallen/unity-retarget.txt",report.ToString());return "Converted "+total+" full-body humanoid clips. "+report;
 }
}








