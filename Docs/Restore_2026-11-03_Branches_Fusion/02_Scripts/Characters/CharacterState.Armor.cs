using UnityEngine;
namespace Codice.Characters
{
    public sealed partial class CharacterState
    {
        [SerializeField] float armor,armorDelay;
        public float Armor=>armor;
        public float MaxArmor=>playerClass==PlayerClass.Tank?55:playerClass==PlayerClass.Mage?15:playerClass==PlayerClass.Archer?20:playerClass==PlayerClass.Shinobi?25:30;
        public const float ArmorDelay=6,ArmorRegen=5;
        void TickArmor(float dt)
        {
            float recharge=Mathf.Max(0,dt-armorDelay);armorDelay=Mathf.Max(0,armorDelay-dt);
            armor=Mathf.Min(MaxArmor,armor+ArmorRegen*recharge);
        }
        public bool RestoreMana(float amount)
        {
            if(!IsAlive||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount)||mana>=MaxMana)return false;
            mana=Mathf.Min(MaxMana,mana+amount);Changed?.Invoke();return true;
        }
    }
}
