using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using UnityEngine.Animations;using UnityEngine.Playables;
public static class ApplyWeaponRoles{
 const string Root="Assets/Resources/Presentation/Human/",Human="Assets/Kevin Iglesias/Human Animations/Animations/Male/";
 static AnimatorController controller;
 static AnimationClip Clip(string name,string path,bool loop=false,bool mirror=false){
 var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));if(!source||!source.isHumanMotion)throw new Exception("No humanoid clip: "+path);
 var clip=UnityEngine.Object.Instantiate(source);clip.name=name;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.mirror=mirror;settings.loopBlendPositionXZ=true;settings.loopBlendOrientation=true;settings.heightFromFeet=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
 foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.propertyName=="RootT.x"||binding.propertyName=="RootT.z"){var c=AnimationUtility.GetEditorCurve(clip,binding);AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,c.Evaluate(0)));}
 var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+name+".anim");if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);clip=old;}else AssetDatabase.CreateAsset(clip,Root+name+".anim");EditorUtility.SetDirty(clip);return clip;
 }
 static void State(string name,AnimationClip clip,bool action=false,bool gait=false){var sm=controller.layers[0].stateMachine;var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??sm.AddState(name);state.motion=clip;state.speedParameter=action?"ActionSpeed":"LocomotionRate";state.speedParameterActive=action||gait;state.speed=1;EditorUtility.SetDirty(state);}
 static string Bundle(int n,string folder,string character,string action)=>"Assets/ExplosiveLLC/Warrior Pack Bundle "+n+" FREE/"+folder+" Warrior Mecanim Animation Pack/Animations/"+character+"@"+(character=="Archer"&&action=="Idle"?"Idle1":action)+".FBX";
 static float ReleaseFraction(string path,HumanBodyBones hand){
 var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var sample=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"HumanPlayer.prefab"));sample.hideFlags=HideFlags.HideAndDontSave;var animator=sample.GetComponent<Animator>();var graph=PlayableGraph.Create("Cast release sampling");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(playable);graph.Play();float best=float.NegativeInfinity,result=.45f;
 try{for(int i=10;i<=48;i++){float phase=i/60f;playable.SetTime(phase*clip.length);graph.Evaluate(0);float reach=Vector3.Dot(animator.GetBoneTransform(hand).position-animator.GetBoneTransform(HumanBodyBones.Chest).position,sample.transform.forward);if(reach>best){best=reach;result=phase;}}return result;}finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(sample);}
 }
 public static string Run(){
 controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"HumanPlayer.controller");
 foreach(var role in new[]{("Sword",3,"Swordsman","Swordsman"),("Axe",1,"Brute","Brute"),("Hammer",3,"Hammer","Hammer"),("Spear",3,"Spearman","Spearman"),("Katana",2,"2 Handed","2Handed"),("Bow",2,"Archer","Archer"),("Staff",2,"Mage","Mage"),("Dagger",1,"Ninja","Ninja"),("Shield",2,"Knight","Knight"),("Musket",3,"Crossbow","Crossbow")}){
 foreach(var action in new[]{"Idle","Walk","Run"}){string state=role.Item1+action;State(state,Clip(state,Bundle(role.Item2,role.Item3,role.Item4,action),true),false,action!="Idle");}
 }
 foreach(var spec in new[]{("SwordCombo1","Combat/1H/HumanM@Attack1H01_R.fbx"),("Thrust","Combat/Polearm/HumanM@AttackPolearm01.fbx"),("KatanaSlashA","Combat/2H/HumanM@Attack2H01.fbx"),("SwordIdle","Combat/1H/HumanM@CombatIdle1H01.fbx"),("KatanaIdle","Combat/2H/HumanM@CombatIdle2H01.fbx"),("SpearIdle","Combat/Polearm/HumanM@CombatIdlePolearm01.fbx"),("BowIdle","Combat/Bow/HumanM@BowIdle01.fbx"),("StaffIdle","Combat/Spellcasting/HumanM@CastingIdle01.fbx"),("Hit","Combat/HumanM@CombatDamage01.fbx"),("StaffHit","Combat/Spellcasting/HumanM@CastingDamage01.fbx"),("StaffCast","Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx"),("Conjure","Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx")})State(spec.Item1,Clip(spec.Item1,Human+spec.Item2,spec.Item1.EndsWith("Idle")),!spec.Item1.EndsWith("Idle"));
 State("AxeAttack",Clip("AxeAttack",Bundle(1,"Brute","Brute","Attack1")),true);State("HeavyAttack",Clip("HeavyAttack",Bundle(3,"Hammer","Hammer","Attack1")),true);
 State("ShootBow",Clip("ShootBow",Bundle(2,"Archer","Archer","Attack1")),true);
 State("DualIdle",Clip("DualIdle","Assets/TransformAnimation SxB_V2/Animation/TwinSword_Idle.FBX",true));
 foreach(var name in new[]{"DualSlice","DualStab"})State(name,Clip(name,Human+"Combat/1H/HumanM@Attack1H01_R.fbx"),true);
 var layers=controller.layers;var dual=layers.First(l=>l.name=="Dual offhand");foreach(var child in dual.stateMachine.states){string name=child.state.name;child.state.motion=Clip("Offhand"+name,name=="DualIdle"?"Assets/TransformAnimation SxB_V2/Animation/TwinSword_Idle.FBX":Human+"Combat/1H/HumanM@Attack1H01_R.fbx",name=="DualIdle",name!="DualIdle");EditorUtility.SetDirty(child.state);}
 State("WeaponSwap",Clip("WeaponSwap","Assets/TransformAnimation SxB_V2/Animation/Sword2Blades_Transform/Inplace/Sword2Blades_Transform_01_Inplace.FBX"),true);
 // Sample the full clip's forward hand reach for the projectile release phase.
 string spell=Human+"Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_";
 float staff=ReleaseFraction(spell+"R.fbx",HumanBodyBones.RightHand),hand=ReleaseFraction(spell+"L.fbx",HumanBodyBones.LeftHand);
 System.IO.File.WriteAllText("Assets/Resources/Presentation/Human/CastTimings.json","{\"staff\":"+staff.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"hand\":"+hand.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
 EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();AssetDatabase.Refresh();return "PASS ten weapon roles, Human Melee/Spellcasting, Warrior 1/2/3, Archer, dual poses; staff release="+staff+" hand="+hand;
 }
}

