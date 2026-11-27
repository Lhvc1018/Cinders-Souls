using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.Animations;using UnityEngine;
public static class AgentShinobiCuts {
 const string Root="Assets/Resources/Presentation/Human/";
 // Cut B becomes the fast Thymesia horizontal cut; cut A keeps the readable overhead diagonal.
 public static string Apply(){
  if(EditorApplication.isPlaying)return "stop play mode first";
  var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"Shinobi.controller");
  var b=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Presentation/Thymesia/Thymesia_KatanaSlashB.anim");
  var state=c.layers[0].stateMachine.states.First(s=>s.state.name=="KatanaSlashB").state;
  var old=state.motion?state.motion.name:"none";state.motion=b;state.speedParameterActive=true;state.speedParameter="ActionSpeed";state.iKOnFeet=false;
  EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
  return "KatanaSlashB: "+old+" -> "+b.name+"\n"+Export();
 }
 // Runtime cannot read AnimatorController; export state->clip so hit windows follow the real motion.
 public static string Export(){
  var sb=new StringBuilder();int n=0;
  foreach(var path in Directory.GetFiles(Root,"*.controller")){
   var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(!c)continue;
   var file=Path.GetFileNameWithoutExtension(path);if(c.name!=file){c.name=file;EditorUtility.SetDirty(c);}
   foreach(var s in c.layers[0].stateMachine.states)if(s.state.motion is AnimationClip clip){sb.Append(c.name).Append('|').Append(s.state.name).Append('|').Append(clip.name).Append('\n');n++;}
  }
  AssetDatabase.SaveAssets();File.WriteAllText(Root+"StateClips.txt",sb.ToString());AssetDatabase.ImportAsset(Root+"StateClips.txt");
  return "exported "+n+" state clips";
 }
}
