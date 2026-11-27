using Codice.Characters;
using UnityEngine;
namespace Codice.Presentation
{
    /// Coop impact feel. Time.timeScale can't freeze in a shared world, so the attacker's own animation stops for a few
    /// hundredths of a second, a spark appears at the hit and the camera gets a small punch.
    public static class CoopHitFeel
    {
        public static void Local(PlayerCharacter player, Vector3 point, float strength)
        {
            strength = Mathf.Clamp01(strength);
            if (player)
            {
                var pose = player.GetComponent<ActorPresentation>();
                if (pose) pose.FreezeUntil = Time.unscaledTime + Mathf.Lerp(.045f, .09f, strength);
                if (player.view) { var punch = player.view.gameObject.GetComponent<CameraPunch>() ?? player.view.gameObject.AddComponent<CameraPunch>(); punch.Kick(Mathf.Lerp(.5f, 1.4f, strength)); }
            }
            var cam = Camera.main; Vector3 face = cam ? cam.transform.position - point : Vector3.up;
            ImpactVfx.Spawn("VFX_ImpactClassic01", point, face, Mathf.Lerp(.25f, .4f, strength));
            Codice.Mechanics.GameInput.Rumble(.25f + .4f * strength, .35f + .4f * strength, .08f);
        }
    }
    /// Small rotational kick on the camera, applied after the camera rig so it never accumulates.
    public sealed class CameraPunch : MonoBehaviour
    {
        float amount, phase;
        public void Kick(float degrees) { amount = Mathf.Max(amount, degrees); phase = 0; }
        void LateUpdate()
        {
            if (amount <= .01f) return;
            phase += Time.unscaledDeltaTime * 40f;
            float a = amount * Mathf.Sin(phase) * Mathf.Exp(-phase * .35f);
            transform.localRotation *= Quaternion.Euler(a * .6f, a * .3f, 0);
            if (phase > 14) amount = 0;
        }
    }
}
