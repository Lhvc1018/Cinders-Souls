using System;using System.Linq;using System.Text;using UnityEngine;using Codice.Characters;
// Class ↔ weapon arsenal verification: affinity matrix, equip restrictions and damage/speed scaling.
public static class AgentArsenal{public static string Run(){var sb=new StringBuilder();var kinds=(WeaponKind[])Enum.GetValues(typeof(WeaponKind));
 sb.AppendLine("clase      "+string.Join(" ",kinds.Select(k=>k.ToString().Substring(0,3))));
 foreach(PlayerClass c in Enum.GetValues(typeof(PlayerClass))){sb.Append(c.ToString().PadRight(11));foreach(var k in kinds){var a=ClassArsenal.Affinity(c,k);sb.Append((a==WeaponAffinity.Proscrita?"  X":a==WeaponAffinity.Maestra?"  S":a==WeaponAffinity.Diestra?"  A":a==WeaponAffinity.Competente?"  B":"  C")+" ");}sb.AppendLine();}
 foreach(PlayerClass c in Enum.GetValues(typeof(PlayerClass))){var s=new CharacterState();s.Initialize(c);var w=s.ActiveWeapon;
  var hammer=new WeaponInstance(WeaponKind.Hammer,2);var before=s.GetWeapon(1);var back=s.Equip(hammer);bool refused=back==hammer;
  sb.AppendLine($"{c}: start={Codice.UI.UIWidgets.WeaponName(w)} {ClassArsenal.Effective(s,w)} dmg x{ClassArsenal.DamageFactor(s,w):0.00} speed x{s.AttackSpeedMultiplier:0.00} | hammer {(refused?"RECHAZADO":"equipado")} | loot Hammer→{ClassArsenal.Usable(c,WeaponKind.Hammer)} | tecnica: {ClassArsenal.Technique(c,w)}");}
 return sb.ToString();} public static string Vigia(){var sb=new StringBuilder();foreach(PlayerClass c in Enum.GetValues(typeof(PlayerClass)))foreach(var l in Codice.Prologue.VigiaLessons.Affinity(c))sb.AppendLine(c+": "+l);sb.AppendLine("deepest="+Codice.Prologue.VigiaLessons.Deepest+" runs="+Codice.Prologue.VigiaLessons.Runs+" forge="+Codice.Prologue.VigiaLessons.ForgeRevealed+" | "+Codice.Prologue.VigiaLessons.Unlocks());return sb.ToString();}
}
