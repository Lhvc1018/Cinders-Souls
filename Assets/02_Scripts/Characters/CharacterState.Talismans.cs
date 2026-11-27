using System;
using UnityEngine;
namespace Codice.Characters
{
    /// Stats a talisman can modify. Add a stat here and read it with TalismanFactor/TalismanBonus where it applies.
    public enum TalismanStat { MaxHealth, MaxMana, MaxArmor, Damage, DamageTaken, MoveSpeed, AttackSpeed, FlaskHeal, ManaRegen }
    public enum Talisman { CrimsonAmber, AzurTear, IronScarab, BlazingClaw, StoneSeal, WindFeather, VerdugoRing, AshCup, SageRing, BloodPact }

    /// Data table of talismans (rings / amulets). Each one is a list of (stat, multiplier delta). Adding a talisman = one row.
    public static class TalismanCatalog
    {
        public const int Slots = 4;
        public struct Entry { public string name, kind, lore; public (TalismanStat stat, float delta)[] mods; public Color tint; }
        public static readonly Entry[] All =
        {
            new Entry{name="Medallón de ámbar carmesí",kind="Amuleto",lore="Late como un segundo corazón.",mods=new[]{(TalismanStat.MaxHealth,.15f)},tint=new Color(.9f,.25f,.2f)},
            new Entry{name="Lágrima azur",kind="Amuleto",lore="Destila el maná de los que rezaron antes.",mods=new[]{(TalismanStat.MaxMana,.25f),(TalismanStat.ManaRegen,.2f)},tint=new Color(.35f,.55f,1f)},
            new Entry{name="Escarabajo de hierro",kind="Anillo",lore="Su caparazón se pega a la armadura.",mods=new[]{(TalismanStat.MaxArmor,.30f)},tint=new Color(.6f,.6f,.65f)},
            new Entry{name="Garra ardiente",kind="Anillo",lore="Arde contra la piel de quien la lleva.",mods=new[]{(TalismanStat.Damage,.12f)},tint=new Color(1f,.5f,.15f)},
            new Entry{name="Sello de piedra",kind="Amuleto",lore="Pesado. Los golpes resbalan.",mods=new[]{(TalismanStat.DamageTaken,-.12f),(TalismanStat.MoveSpeed,-.04f)},tint=new Color(.55f,.5f,.4f)},
            new Entry{name="Pluma del viento",kind="Anillo",lore="Tus pasos no hacen ruido.",mods=new[]{(TalismanStat.MoveSpeed,.08f)},tint=new Color(.75f,.95f,.9f)},
            new Entry{name="Anillo del verdugo",kind="Anillo",lore="Ritmo de hacha sobre la nuca.",mods=new[]{(TalismanStat.AttackSpeed,.10f)},tint=new Color(.85f,.7f,.3f)},
            new Entry{name="Copa de ceniza",kind="Amuleto",lore="Lo que bebes rinde más.",mods=new[]{(TalismanStat.FlaskHeal,.30f)},tint=new Color(.3f,.85f,.7f)},
            new Entry{name="Anillo del sabio",kind="Anillo",lore="Escucha a los muertos que leían.",mods=new[]{(TalismanStat.MaxMana,.15f),(TalismanStat.Damage,.05f)},tint=new Color(.6f,.4f,.95f)},
            new Entry{name="Pacto de sangre",kind="Amuleto",lore="Más fuerza, menos vida. Siempre hay precio.",mods=new[]{(TalismanStat.Damage,.25f),(TalismanStat.MaxHealth,-.15f)},tint=new Color(.7f,.05f,.15f)},
        };
        public static int Count => All.Length;
        static readonly string[] StatNames = { "vida máxima", "maná máximo", "armadura máxima", "daño", "daño recibido", "velocidad de movimiento", "velocidad de ataque", "curación del frasco", "regeneración de maná" };
        public static string Describe(Talisman t)
        {
            var e = All[(int)t]; string s = "";
            foreach (var m in e.mods) s += (m.delta >= 0 ? "+" : "−") + Mathf.RoundToInt(Mathf.Abs(m.delta) * 100) + " % de " + StatNames[(int)m.stat] + "\n";
            return s + "\n<i>" + e.lore + "</i>";
        }
    }

    public sealed partial class CharacterState
    {
        [SerializeField] int ownedTalismans;
        [SerializeField] int[] equippedTalismans = { -1, -1, -1, -1 };
        int[] Equipped { get { if (equippedTalismans == null || equippedTalismans.Length != TalismanCatalog.Slots) { equippedTalismans = new int[TalismanCatalog.Slots]; for (int i = 0; i < TalismanCatalog.Slots; i++) equippedTalismans[i] = -1; } return equippedTalismans; } }
        public bool OwnsTalisman(Talisman t) => (ownedTalismans & (1 << (int)t)) != 0;
        public int OwnedTalismanCount { get { int n = 0; for (int i = 0; i < TalismanCatalog.Count; i++) if ((ownedTalismans & (1 << i)) != 0) n++; return n; } }
        /// Talisman equipped in a slot, or -1.
        public int TalismanInSlot(int slot) => slot >= 0 && slot < TalismanCatalog.Slots ? Equipped[slot] : -1;
        public bool IsTalismanEquipped(Talisman t) => Array.IndexOf(Equipped, (int)t) >= 0;
        /// Adds a talisman to the inventory; it goes straight into a free slot if there is one.
        public bool GrantTalisman(Talisman t)
        {
            if (OwnsTalisman(t)) return false;
            ownedTalismans |= 1 << (int)t;
            int free = Array.IndexOf(Equipped, -1); if (free >= 0) EquipTalisman(free, (int)t); else Changed?.Invoke();
            return true;
        }
        /// Equips (or with -1, removes) a talisman. Current health/mana/armor keep their ratio so the new max values apply at once.
        public void EquipTalisman(int slot, int talisman)
        {
            if (slot < 0 || slot >= TalismanCatalog.Slots) return;
            if (talisman >= 0 && !OwnsTalisman((Talisman)talisman)) return;
            float hp = MaxHealth > 0 ? health / MaxHealth : 1, mp = MaxMana > 0 ? mana / MaxMana : 1, ar = MaxArmor > 0 ? armor / MaxArmor : 1;
            if (talisman >= 0) { int was = Array.IndexOf(Equipped, talisman); if (was >= 0) Equipped[was] = -1; }
            Equipped[slot] = talisman;
            if (health > 0) health = Mathf.Max(1, hp * MaxHealth); mana = mp * MaxMana; armor = ar * MaxArmor;
            Changed?.Invoke();
        }
        /// Multiplier from equipped talismans for one stat (1 = no change).
        public float TalismanFactor(TalismanStat stat)
        {
            float f = 1;
            foreach (int t in Equipped) if (t >= 0 && t < TalismanCatalog.Count) foreach (var m in TalismanCatalog.All[t].mods) if (m.stat == stat) f += m.delta;
            return Mathf.Max(.1f, f);
        }
        void ResetTalismans() { ownedTalismans = 0; equippedTalismans = new[] { -1, -1, -1, -1 }; }
    }
}
