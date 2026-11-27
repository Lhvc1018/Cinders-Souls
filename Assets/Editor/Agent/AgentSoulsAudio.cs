using System.Linq;using UnityEditor;using UnityEngine;using Codice.Presentation;
// Installs the procedurally synthesized Soulsborne score + SFX (Assets/05_Music/Souls) into GameAudioLibrary.
public static class AgentSoulsAudio{
 const string Root="Assets/05_Music/Souls";
 [MenuItem("Codice/Audio/Install Souls score")]
 public static string Install(){
  foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Root})){
   var path=AssetDatabase.GUIDToAssetPath(guid);var imp=(AudioImporter)AssetImporter.GetAtPath(path);bool music=!path.Contains("/SFX/");
   var s=imp.defaultSampleSettings;s.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=music?.55f:.8f;
   imp.defaultSampleSettings=s;imp.loadInBackground=music;imp.SaveAndReimport();}
  var lib=Resources.Load<GameAudioLibrary>("GameAudioLibrary");
  AudioClip M(string n)=>AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Souls_{n}.ogg");
  lib.menu=M("Menu");lib.dungeon=M("Dungeon");lib.arena=M("Arena");lib.boss=M("Boss");lib.bossTension=M("Tension");lib.ending=M("Ending");lib.ambience=M("Ambience");
  var list=lib.effects.ToList();var sb=new System.Text.StringBuilder();
  void Set(SoundCue cue,string n,float vol){var clips=AssetDatabase.FindAssets("SFX_"+n+"_",new[]{Root+"/SFX"}).Select(g=>AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).Where(c=>c).ToArray();
   var e=list.Find(x=>x.cue==cue);if(e==null){e=new SoundEntry{cue=cue,cooldown=.08f};list.Add(e);}e.clips=clips;e.volume=vol;sb.Append($"{cue}:{clips.Length} ");}
  Set(SoundCue.Hit,"Hit",.6f);Set(SoundCue.Block,"Block",.62f);Set(SoundCue.Parry,"Parry",.8f);Set(SoundCue.Swing,"Swing",.38f);Set(SoundCue.Heavy,"Heavy",.5f);
  Set(SoundCue.Footstep,"Footstep",.22f);Set(SoundCue.BossImpact,"BossImpact",.8f);Set(SoundCue.Seal,"Seal",.6f);Set(SoundCue.Hurt,"Hurt",.5f);
  Set(SoundCue.SpellArcane,"SpellArcane",.6f);Set(SoundCue.SpellFrost,"SpellFrost",.6f);Set(SoundCue.SpellLightning,"SpellLightning",.6f);Set(SoundCue.SpellFire,"SpellFire",.6f);Set(SoundCue.SpellPoison,"SpellPoison",.55f);
  lib.effects=list.ToArray();EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
  return $"music menu={lib.menu?.length:0} dungeon={lib.dungeon?.length:0} arena={lib.arena?.length:0} boss={lib.boss?.length:0} tension={lib.bossTension?.length:0} ending={lib.ending?.length:0} ambience={lib.ambience?.length:0} | {sb}";}
 public static string Status(){var a=GameAudio.Instance;if(!a)return "no audio";var bed=a.transform.Find("Ambient bed")?.GetComponent<AudioSource>();
  return $"music={(a.CurrentMusic?a.CurrentMusic.name:"-")} ambience={(bed&&bed.isPlaying?bed.clip.name+" vol="+bed.volume.ToString("0.00"):"off")} played={a.PlayedCount} last={a.LastCue} parry={a.Count(SoundCue.Parry)} hit={a.Count(SoundCue.Hit)} block={a.Count(SoundCue.Block)} seal={a.Count(SoundCue.Seal)}";}
 public static async System.Threading.Tasks.Task<string> Fight(){var r=Codice.Mechanics.RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new System.Text.StringBuilder();
  int first=l.Links.First(e=>e.x==0&&e.y!=l.SecretRoom&&e.y!=l.VaultRoom).y;UnityEngine.AI.NavMesh.SamplePosition(f.Center(first),out var h,4,UnityEngine.AI.NavMesh.AllAreas);
  var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=h.position+Vector3.up*.1f;cc.enabled=true;await System.Threading.Tasks.Task.Delay(2500);sb.AppendLine("combat: "+Status());
  for(int i=0;i<f.Cleared.Length;i++)if(i!=l.VaultRoom){f.Cleared[i]=true;f.Visited[i]=true;}
  foreach(var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))e.ReceiveDamage(new Codice.Characters.DamageInfo{amount=1e6f,source=p.gameObject,direction=Vector3.forward});await System.Threading.Tasks.Task.Delay(1500);UnityEngine.AI.NavMesh.SamplePosition(f.Center(l.Boss),out var hb,6,UnityEngine.AI.NavMesh.AllAreas);cc.enabled=false;p.transform.position=hb.position+Vector3.up*.1f;cc.enabled=true;await System.Threading.Tasks.Task.Delay(3000);sb.AppendLine("boss room: "+Status()+" boss="+(f.context.arena.ActiveBoss?f.context.arena.ActiveBoss.name:"none"));
  return sb.ToString();}
}
