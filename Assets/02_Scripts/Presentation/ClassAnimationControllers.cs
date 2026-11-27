using Codice.Characters;
using UnityEngine;

namespace Codice.Presentation
{
    public static class ClassAnimationControllers
    {
        public static RuntimeAnimatorController Load(PlayerClass role)
        {
            string asset = role == PlayerClass.Warrior || role == PlayerClass.Tank ? "ClassicHuman" :
                role == PlayerClass.Shinobi ? "Shinobi" :
                role == PlayerClass.Berserker ? "Berserker" : "HumanPlayer";
            return Resources.Load<RuntimeAnimatorController>("Presentation/Human/" + asset);
        }
    }
}
