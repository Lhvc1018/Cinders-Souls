using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using Codice.Presentation;
public static class GameAudioFinaleSetup {
 [MenuItem("Codice/Audio/Upgrade combat and ending")]
 public static void Upgrade(){var library=Resources.Load<GameAudioLibrary>("GameAudioLibrary");if(!library)throw new Exception("Missing library");var paths=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/05_Music","Assets/Angel_Knight_Anims"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
 AudioClip Clip(string name,bool loop=false){var path=paths.FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p)==name);if(path==null)throw new Exception("Missing audio "+name);var importer=AssetImporter.GetAtPath(path) as AudioImporter;var settings=importer.defaultSampleSettings;settings.loadType=loop?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.forceToMono=!loop;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<AudioClip>(path);}
 var entries=(library.effects??Array.Empty<SoundEntry>()).ToList();void Add(SoundCue cue,float volume,float cooldown,params string[] names){entries.RemoveAll(e=>e.cue==cue);entries.Add(new SoundEntry{cue=cue,volume=volume,cooldown=cooldown,clips=names.Select(n=>Clip(n)).ToArray()});}
 Add(SoundCue.BossWindup,.28f,.25f,"Medieval Board Game - Sword Slash 3","Medieval Board Game - Sword Slash 4");
 Add(SoundCue.BossImpact,.55f,.16f,"harsh_thud","sword_clash_2");Add(SoundCue.BossCast,.38f,.25f,"synth_bass_mystery");
 Add(SoundCue.BossPhase,.42f,2,"synth_warning");Add(SoundCue.BossLow,.48f,2,"freesound_community-dragon-shout-roar-98277");Add(SoundCue.BossDeath,.5f,.5f,"die1");
 Add(SoundCue.Portal,.4f,.5f,"vibraphone_chime_positive");Add(SoundCue.Seal,.4f,.3f,"stone_push_medium");Add(SoundCue.Heal,.35f,.3f,"synth_confirmation");Add(SoundCue.Orb,.45f,.5f,"vibraphone_level_complete");Add(SoundCue.Finale,.55f,2,"vibraphone_level_complete");
 library.bossTension=Clip("Power_Up_Loop",true);library.ending=Clip("nakaradaalexander-winning-loop-228639",true);library.effects=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Debug.Log("[Audio] Combat and ending upgrade: "+entries.Count+" cues");}
}
