using UnityEngine;
namespace Codice.Characters
{
    // Compatibility facade for saved prefabs and LAN snapshots. Stamina no longer limits any class.
    public sealed class PlayerStamina : MonoBehaviour
    {
        public enum Action { Attack, HeavyAttack, Dodge, Charged, Block }
        public const float Regen=0, RegenDelay=0;
        public float Max=>100;
        public float Value=>Max;
        public bool Infinite=>true;
        public bool InCombat=>false;
        public static float Cost(Action action)=>0;
        public bool TrySpend(Action action)=>true;
        public bool TrySpendAmount(float amount)=>true;
        public void SetNetwork(float value) { }
    }
}
