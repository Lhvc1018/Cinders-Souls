using UnityEngine;
namespace Codice.Multiplayer
{
    // Difficulty follows the party size whenever a cooperative session is active. Single player = 1 everywhere.
    public static class CoopScaling
    {
        public static int Party => LanSession.Active ? Mathf.Clamp(LanSession.Instance.PartySize, 1, 4) : 1;
        /// Enemy and boss health: +60 % per extra player.
        public static float EnemyHealth => 1 + .6f * (Party - 1);
        /// Enemy damage dealt to players: +12 % per extra player.
        public static float EnemyDamage => 1 + .12f * (Party - 1);
        /// Extra residents per combat room (density instead of larger rooms, so every peer keeps the same layout).
        public static int ExtraEnemies => Mathf.Min(3, Party - 1);
        /// Chest tier bonus: +1 from three players. Gold: +25 % per extra player.
        public static int TierBonus => Party >= 3 ? 1 : 0;
        public static float Gold => 1 + .25f * (Party - 1);
        public static string Summary => Party <= 1 ? "" : $"x{Party} · Vida enemiga +{(EnemyHealth - 1) * 100:F0}% · Daño +{(EnemyDamage - 1) * 100:F0}% · +{ExtraEnemies} por sala";
    }
}
