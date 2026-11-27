using UnityEngine;
namespace Codice.Characters
{
    public enum StatueBlessing { Blood, Mind, Iron }
    public sealed partial class CharacterState
    {
        [SerializeField] int blessings;
        public bool HasBlessing(StatueBlessing kind)=>(blessings&(1<<(int)kind))!=0;
        public float BlessingDamage=>HasBlessing(StatueBlessing.Blood)?1.15f:1;
        public bool GrantBlessing(StatueBlessing kind)
        {
            if(!IsAlive||(int)kind<0||(int)kind>2||HasBlessing(kind))return false;
            blessings|=1<<(int)kind;if(kind==StatueBlessing.Mind)mana=Mathf.Min(MaxMana,mana+25);if(kind==StatueBlessing.Iron)armor=Mathf.Min(MaxArmor,armor+20);
            Changed?.Invoke();return true;
        }
        public static string BlessingName(StatueBlessing kind)=>new[]{"Estatua de sangre","Estatua de la mente","Estatua de hierro"}[(int)kind];
        public static string BlessingDescription(StatueBlessing kind)=>new[]{"+15% daño de armas","+25 maná máximo","+20 armadura máxima"}[(int)kind];
    }
}
