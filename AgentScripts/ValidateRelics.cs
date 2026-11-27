using System;
using System.Linq;
using Codice.Characters;
using UnityEditor;
using UnityEngine;
public static class ValidateRelics
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    public static string Run()
    {
        checks=0;
        foreach(PlayerClass kind in Enum.GetValues(typeof(PlayerClass)))
        {
            var s=new CharacterState();s.Initialize(kind);float speed=s.AttackSpeedMultiplier;
            Check(s.EquipPassive(PassiveKind.Frenzy),"Equip mutation");Check(s.AttackSpeedMultiplier>speed,"Passive must work without activation");
            s.TickPassives(100);Check(s.HasPassive(PassiveKind.Frenzy),"Passive must survive timers");Check(!s.EquipPassive(PassiveKind.Frenzy),"Duplicate mutation rejected");
            Check(s.EquipPassive(PassiveKind.Overload)&&s.EquipPassive(PassiveKind.MercuryBoots),"Three slots");
            Check(!s.EquipPassive(PassiveKind.ThornArmor),"Full requires selection");Check(s.EquipPassive(PassiveKind.ThornArmor,2)&&!s.HasPassive(PassiveKind.MercuryBoots),"Replacement removes old effect");
            Check(s.ConsumeRelic(0)&&!s.ConsumeRelic(0),"Cooldown denies spam");float wait=s.RelicWait(0);s.TickPassives(0);Check(s.RelicWait(0)==wait,"Paused cooldown");
            s.RelicKill();Check(s.RelicWait(0)==wait-1,"Overload reduces cooldown on kill");
            Check(!s.EquipRelic(s.GetRelic(1),0),"Duplicate active rejected");Check(!s.EquipRelic(ActiveRelic.AshenWard,-1),"Invalid slot rejected");
            Check(s.EquipRelic(ActiveRelic.AshenWard,0)&&s.RelicWait(0)>0,"Swap preserves cooldown");s.TickPassives(30);
            Check(s.ConsumeRelic(0)&&s.WardRemaining==4,"Ward duration");
            string snapshot=s.Capture();var restored=new CharacterState();restored.Initialize(kind);restored.Restore(snapshot);
            Check(restored.GetRelic(0)==ActiveRelic.AshenWard&&restored.PassiveCount==3&&restored.WardRemaining==4,"Run snapshot preserves loadout");
            s.Damage(s.MaxHealth);Check(!s.ConsumeRelic(1),"Dead cannot cast");s.Initialize(kind);Check(s.PassiveCount==0&&s.RelicWait(0)==0,"Restart clears run powers");
        }
        foreach(string kind in new[]{"AshenHound","HorrorInquisitor"})
        {
            string path="Assets/03_Models/Enemies/"+kind+"/"+kind+"_rigged.fbx";
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
            foreach(string state in new[]{"Idle","Chase","Melee","Slam","Hit","Stun","Death"})
            {
                var clip=clips.FirstOrDefault(c=>c.name.EndsWith(state));Check(clip&&clip.length>.1f,kind+" "+state);
                Check(AnimationUtility.GetCurveBindings(clip).Length>10,"Bone curves missing "+state);
            }
        }
        var report="[Cinders validation] "+checks+" checks passed: five classes, passive lifetime, duplicates, replacement, cooldowns, death/reset, snapshot and seven skeletal takes per enemy.";
        Debug.Log(report);System.IO.File.WriteAllText("Docs/RelicsValidation.txt",report);return report;
    }
}
