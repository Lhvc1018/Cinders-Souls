using System;
using UnityEngine;

namespace Codice.Characters
{
    public enum ActiveRelic { Censer, FrostBell, BloodLance, AshenWard }
    public static class RelicCatalog
    {
        public static string ShortName(ActiveRelic kind)=>new[]{"Incensario","Campana","Lanza","Velo"}[(int)kind];
        public static string Name(ActiveRelic kind) => new[]{"Incensario de brasas","Campana del sepulcro","Lanza de sangre","Velo de ceniza"}[(int)kind];
        public static float Cooldown(ActiveRelic kind) => new[]{10f,14f,8f,18f}[(int)kind];
        public static float ManaCost(ActiveRelic kind)=>new[]{12f,18f,15f,20f}[(int)kind];
        public static string Description(ActiveRelic kind) => new[]{
            "70 de daño en un radio de 4 m. Rechaza a los enemigos cercanos. Recarga: 10 s.",
            "24 de daño en 5 m. Aturde 1.4 s; jefes: 0.35 s. Recarga: 14 s.",
            "85 de daño en una línea de 12 m. No atraviesa paredes. Recarga: 8 s.",
            "Reduce el daño recibido un 35% durante 4 s. Recarga: 18 s."}[(int)kind];
        public static string PassiveDescription(PassiveKind kind) => new[]{
            "+25% de velocidad de ataque mientras esté equipada.",
            "Recuperas un 4% de vida máxima al matar. Contra jefes, cada 10 impactos.",
            "+50% de carga del especial. Cada baja reduce 1 s las recargas de activas.",
            "+20% de movimiento y una segunda carga de dash.",
            "Recibes un 20% menos de daño y devuelves 35 de daño cuerpo a cuerpo."}[(int)kind];
    }
    public sealed partial class CharacterState
    {
        [SerializeField] ActiveRelic[] activeRelics={ActiveRelic.Censer,ActiveRelic.FrostBell};
        [SerializeField] float[] relicCooldowns=new float[2];
        [SerializeField] float wardRemaining;
        public ActiveRelic GetRelic(int slot)=>activeRelics[Mathf.Clamp(slot,0,1)];
        public float RelicWait(int slot)=>slot>=0&&slot<2?relicCooldowns[slot]:0;
        public float WardRemaining=>wardRemaining;
        void ResetRelics(){activeRelics=new[]{ActiveRelic.Censer,ActiveRelic.FrostBell};relicCooldowns=new float[2];wardRemaining=0;}
        void TickRelics(float dt){for(int i=0;i<2;i++)relicCooldowns[i]=Mathf.Max(0,relicCooldowns[i]-dt);wardRemaining=Mathf.Max(0,wardRemaining-dt);}
        public bool OwnsRelic(ActiveRelic kind)=>Array.IndexOf(activeRelics,kind)>=0;
        public bool EquipRelic(ActiveRelic kind,int slot)
        {
            if(!IsAlive||slot<0||slot>1||!Enum.IsDefined(typeof(ActiveRelic),kind)||OwnsRelic(kind))return false;
            activeRelics[slot]=kind;
            // Swapping cannot erase a running cooldown.
            relicCooldowns[slot]=Mathf.Max(relicCooldowns[slot],2f);Changed?.Invoke();return true;
        }
        public bool ConsumeRelic(int slot)
        {
            if(!IsAlive||slot<0||slot>1||relicCooldowns[slot]>0)return false;
            var kind=activeRelics[slot];float cost=RelicCatalog.ManaCost(kind);if(mana<cost)return false;
            mana-=cost;manaDelay=1.5f;relicCooldowns[slot]=RelicCatalog.Cooldown(kind);
            if(kind==ActiveRelic.AshenWard)wardRemaining=4;
            Changed?.Invoke();return true;
        }
        public void RelicKill(){if(HasPassive(PassiveKind.Overload))for(int i=0;i<2;i++)relicCooldowns[i]=Mathf.Max(0,relicCooldowns[i]-1);}
    }
}
