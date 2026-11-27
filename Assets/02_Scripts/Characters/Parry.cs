using Codice.Mechanics;
using Codice.Presentation;
using UnityEngine;
namespace Codice.Characters
{
    /// One place for parry rules and feedback, shared by every class that can guard.
    ///  - Melee: ShinobiGuard (katana deflect) and ClassIdentity (shield / Caballero riposte window) resolve the hit.
    ///  - Ranged: an enemy bolt that meets a perfect guard is sent back at its shooter (EnemyProjectile.Reflect).
    public static class Parry
    {
        /// True and consumes the window if the player is in a perfect guard facing the incoming direction.
        public static bool TryDeflect(PlayerCharacter p, Vector3 incoming, Vector3 point)
        {
            if (!p || !p.State.IsAlive || incoming.sqrMagnitude < .0001f) return false;
            if (Vector3.Dot(Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized, -Vector3.ProjectOnPlane(incoming, Vector3.up).normalized) < .3f) return false;
            var shinobi = p.GetComponent<ShinobiGuard>(); bool ok = shinobi && shinobi.ConsumePerfectForProjectile();
            if (!ok) { var id = p.GetComponent<ClassIdentity>(); ok = id && id.ConsumeParryWindow(); }
            if (!ok) return false;
            Feedback(p, point, incoming, true); return true;
        }
        /// Sound, hit-stop, rumble, sparks and a golden ring. `ranged` adds a brighter flash (the bolt turns around).
        public static void Feedback(PlayerCharacter p, Vector3 point, Vector3 incoming, bool ranged)
        {
            GameAudio.Play(SoundCue.Parry); HitStop.Do(ranged ? .12f : .1f); GameInput.Rumble(.5f, 1f, .15f);
            PresentationVfx.PerfectDeflect(point, incoming);
            RelicPulse.Spawn(point, ranged ? .9f : .6f, new Color(1f, .82f, .42f));
            if (Multiplayer.LanAvatar.IsRemote(p)) return;
            Codice.UI.RunHUD.Instance?.Announce(ranged ? "PARRY · PROYECTIL DEVUELTO" : "PARRY PERFECTO");
        }
    }
}
