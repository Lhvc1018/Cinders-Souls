using UnityEngine;
namespace Codice.Characters
{
    public sealed partial class CharacterState
    {
        [SerializeField] float armor,armorDelay;
        public float Armor=>armor;
        public float MaxArmor=>((playerClass==PlayerClass.Tank?55:playerClass==PlayerClass.Mage?15:playerClass==PlayerClass.Archer?20:playerClass==PlayerClass.Shinobi?25:30)+(HasBlessing(StatueBlessing.Iron)?20:0))*TalismanFactor(TalismanStat.MaxArmor);
        public const float ArmorDelay=6,ArmorRegen=0;
        void TickArmor(float dt)
        {
            armorDelay=Mathf.Max(0,armorDelay-dt);
        }
        public bool RestoreArmor(float amount)
        {
            if(!IsAlive||!float.IsFinite(amount)||amount<=0||armor>=MaxArmor)return false;
            armor=Mathf.Min(MaxArmor,armor+amount);Changed?.Invoke();return true;
        }
        public bool RestoreMana(float amount)
        {
            if(!IsAlive||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount)||mana>=MaxMana)return false;
            mana=Mathf.Min(MaxMana,mana+amount);Changed?.Invoke();return true;
        }
    }
}
