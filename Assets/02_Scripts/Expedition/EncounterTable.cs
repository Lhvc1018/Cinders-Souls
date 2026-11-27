using Codice.Enemies;
namespace Codice.Expedition
{
    /// Spawn table per world: how many enemies a combat room holds and how the basic kinds are weighted.
    /// (World rosters still swap the models; this decides headcount and the melee / ranged / caster / heavy mix.)
    public struct EncounterTable
    {
        public int min, maxEarly, maxLate, sideMin, sideMax;
        public float swordsman, crossbow, mage, heavy;
        static readonly EncounterTable[] Worlds =
        {
            new EncounterTable { min = 3, maxEarly = 5, maxLate = 7, sideMin = 3, sideMax = 4, swordsman = 5, crossbow = 3, mage = 1, heavy = 1 },  // 1 Mazmorra
            new EncounterTable { min = 3, maxEarly = 5, maxLate = 7, sideMin = 3, sideMax = 5, swordsman = 4, crossbow = 3, mage = 2, heavy = 1 },  // 2 Cloacas
            new EncounterTable { min = 4, maxEarly = 6, maxLate = 7, sideMin = 3, sideMax = 5, swordsman = 4, crossbow = 2, mage = 2, heavy = 2 },  // 3 Cementerio
            new EncounterTable { min = 4, maxEarly = 6, maxLate = 8, sideMin = 4, sideMax = 5, swordsman = 3, crossbow = 2, mage = 2, heavy = 3 },  // 4 Forja
            new EncounterTable { min = 4, maxEarly = 6, maxLate = 8, sideMin = 4, sideMax = 6, swordsman = 5, crossbow = 2, mage = 1, heavy = 2 },  // 5 Campamento
        };
        public static EncounterTable For(int floor) { var t = Worlds[System.Math.Clamp((floor - 1) / 3, 0, Worlds.Length - 1)]; if (floor == 3) { t.min = 5; t.maxEarly = t.maxLate = 7; } return t; }
        public EnemyKind Pick(System.Random rng, bool firstRoom)
        {
            float h = firstRoom ? 0 : heavy, m = firstRoom ? 0 : mage, total = swordsman + crossbow + m + h, r = (float)rng.NextDouble() * total;
            if ((r -= swordsman) < 0) return EnemyKind.Swordsman; if ((r -= crossbow) < 0) return EnemyKind.Crossbowman; if ((r -= m) < 0) return EnemyKind.Mage; return EnemyKind.Heavy;
        }
    }
}
