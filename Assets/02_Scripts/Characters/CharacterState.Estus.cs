using UnityEngine;
namespace Codice.Characters
{
    public sealed partial class CharacterState
    {
        public const int EstusCapacity=3;
        public const float EstusHealFraction=.4f;
        [SerializeField] int estusCharges=EstusCapacity;
        public int EstusCharges=>estusCharges;
        public bool AddEstusCharge(){if(!IsAlive||estusCharges>=EstusCapacity)return false;estusCharges++;Changed?.Invoke();return true;}
        public bool DrinkEstus()
        {
            if(!IsAlive||health>=MaxHealth||estusCharges<=0)return false;
            estusCharges--;Heal(MaxHealth*EstusHealFraction*BoonFlask);return true;
        }
        public bool RefillEstus()
        {
            if(!IsAlive||estusCharges>=EstusCapacity)return false;
            estusCharges=EstusCapacity;Changed?.Invoke();return true;
        }
    }
}
