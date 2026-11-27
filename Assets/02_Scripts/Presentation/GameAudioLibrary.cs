using UnityEngine;
namespace Codice.Presentation
{
    public enum SoundCue { Click, Swing, Bow, Gun, Magic, Heavy, Hit, Block, Hurt, Death, Footstep, Coin, Pickup, Chest, Purchase, Error, Reload, Power, Dash, Explosion, Victory, Defeat, Boss, BossWindup, BossImpact, BossCast, BossPhase, BossLow, BossDeath, Portal, Seal, Heal, Orb, Finale, Parry, SpellArcane, SpellFrost, SpellLightning, SpellFire, SpellPoison }
    [System.Serializable] public sealed class SoundEntry
    {
        public SoundCue cue;
        public AudioClip[] clips;
        [Range(0,1)] public float volume=.5f;
        [Min(0)] public float cooldown=.06f;
    }
    public sealed class GameAudioLibrary : ScriptableObject
    {
        public AudioClip menu, dungeon, arena;
        public AudioClip bossTension, ending;
        [Tooltip("Boss fights (falls back to arena).")] public AudioClip boss;
        [Tooltip("Looping ambient bed under expedition music (wind, drips, chains).")] public AudioClip ambience;
        [Range(0,1)] public float ambienceVolume = .22f;
        [Tooltip("Gain for all background music; does not change effects or the saved master volume.")]
        [Range(0,1)] public float musicVolume = .595f;
        public SoundEntry[] effects;
    }
}
