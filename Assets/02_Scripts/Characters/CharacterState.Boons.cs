using System;
using UnityEngine;
namespace Codice.Characters
{
    /// Bendiciones de ceniza: potenciadores de la partida, acumulables (hasta 3 veces cada uno).
    public enum AshBoon { Temper, Vigor, ShadowStep, EmberBlood, GraveSkin, ExecutionerRhythm, AppraiserGreed, DeepFlask, CriticalEye, ArcaneEcho, LastBreath, VengefulEmber, CoupDeGrace, IronPosture, SoulHunger }
    public static class BoonCatalog
    {
        public const int MaxStacks = 3;
        public static readonly string[] Names = { "Filo templado", "Pulmones de fragua", "Paso de sombra", "Sangre de brasa", "Piel de sepulcro", "Ritmo del verdugo", "Codicia del tasador", "Frasco hondo", "Ojo del cazador", "Eco arcano", "Último aliento", "Brasa vengativa", "Golpe de gracia", "Postura férrea", "Hambre de almas" };
        public static readonly string[] Effects = {
            "+10 % de daño", "+12 % de vida máxima", "+8 % de velocidad de movimiento", "Robas el 3 % del daño que infliges",
            "−8 % de daño recibido", "+8 % de velocidad de ataque", "+20 % de oro obtenido", "El frasco cura un 25 % más",
            "+6 % de probabilidad de crítico", "+15 % de maná máximo y +25 % de regeneración", "Una vez por piso sobrevives a un golpe mortal con 1 de vida",
            "Al recibir un golpe cuerpo a cuerpo: 25 % de quemar al atacante (20 de daño)", "+30 % de daño a enemigos bajo el 30 % de vida", "+25 % de daño a la postura enemiga", "Cada baja devuelve 3 de maná y 2 % de vida" };
        public static readonly Color[] Colors = { new Color(1f,.5f,.2f), new Color(.9f,.2f,.2f), new Color(.5f,.5f,.9f), new Color(.85f,.1f,.25f), new Color(.6f,.6f,.55f), new Color(1f,.75f,.3f), new Color(1f,.85f,.2f), new Color(.3f,.85f,.7f), new Color(.95f,.95f,.6f), new Color(.45f,.6f,1f), new Color(1f,1f,1f), new Color(1f,.35f,.05f), new Color(.7f,.05f,.1f), new Color(.65f,.5f,.35f), new Color(.6f,.3f,.9f) };
        public static int Count => Names.Length;
    }
    public sealed partial class CharacterState
    {
        [SerializeField] int[] boons = new int[16];
        [SerializeField] bool lastBreathSpent;
        public int Boon(AshBoon b) { if (boons == null || boons.Length < BoonCatalog.Count) Array.Resize(ref boons, 16); return boons[(int)b]; }
        public bool CanTakeBoon(AshBoon b) => Boon(b) < BoonCatalog.MaxStacks;
        public bool GrantBoon(AshBoon b)
        {
            if (!IsAlive || !CanTakeBoon(b)) return false;
            float before = MaxHealth; boons[(int)b]++;
            if (b == AshBoon.Vigor) health = Mathf.Min(MaxHealth, health + (MaxHealth - before));
            Changed?.Invoke(); return true;
        }
        public int BoonTotal { get { int n = 0; for (int i = 0; i < BoonCatalog.Count; i++) n += Boon((AshBoon)i); return n; } }
        public string BoonSummary { get { var s = ""; for (int i = 0; i < BoonCatalog.Count; i++) if (Boon((AshBoon)i) > 0) s += (s.Length > 0 ? " · " : "") + BoonCatalog.Names[i] + (Boon((AshBoon)i) > 1 ? " ×" + Boon((AshBoon)i) : ""); return s; } }
        float BoonHealthFactor => (1 + .12f * Boon(AshBoon.Vigor)) * TalismanFactor(TalismanStat.MaxHealth);
        float BoonManaFactor => (1 + .15f * Boon(AshBoon.ArcaneEcho)) * TalismanFactor(TalismanStat.MaxMana);
        public float BoonManaRegen => (1 + .25f * Boon(AshBoon.ArcaneEcho)) * TalismanFactor(TalismanStat.ManaRegen);
        float BoonSpeedFactor => (1 + .08f * Boon(AshBoon.ShadowStep)) * TalismanFactor(TalismanStat.MoveSpeed) * SpellSpeed;
        /// Set every frame by RunSpellbook from active personal buffs (Piel de hierro / Paso del viento).
        [NonSerialized] public float SpellGuard = 1, SpellSpeed = 1;
        float BoonAttackSpeed => (1 + .08f * Boon(AshBoon.ExecutionerRhythm)) * TalismanFactor(TalismanStat.AttackSpeed) * SpellSpeed;
        public float BoonDamage => (1 + .10f * Boon(AshBoon.Temper)) * TalismanFactor(TalismanStat.Damage);
        public float BoonCrit => .06f * Boon(AshBoon.CriticalEye);
        public float BoonLifesteal => .03f * Boon(AshBoon.EmberBlood);
        public float BoonPosture => 1 + .25f * Boon(AshBoon.IronPosture);
        public float BoonFlask => (1 + .25f * Boon(AshBoon.DeepFlask)) * TalismanFactor(TalismanStat.FlaskHeal);
        public float BoonExecute(float targetHealthFraction) => targetHealthFraction < .3f ? 1 + .3f * Boon(AshBoon.CoupDeGrace) : 1;
        float BoonGuard => Mathf.Pow(.92f, Boon(AshBoon.GraveSkin)) * TalismanFactor(TalismanStat.DamageTaken) * SpellGuard;
        int BoonGold(int amount) => Mathf.RoundToInt(amount * (1 + .2f * Boon(AshBoon.AppraiserGreed)));
        /// Called on each floor start: the Last Breath charge refreshes.
        public void RefreshFloorBoons() { lastBreathSpent = false; }
        public void BoonKill() { int n = Boon(AshBoon.SoulHunger); if (n <= 0 || !IsAlive) return; RestoreMana(3 * n); Heal(MaxHealth * .02f * n); }
        bool SaveFromDeath(float incoming)
        {
            if (Boon(AshBoon.LastBreath) <= 0 || lastBreathSpent || health - incoming > 0) return false;
            lastBreathSpent = true; return true;
        }
    }
}
