using System;
using System.Reflection;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
using UnityEngine;

public static class VerifyBladeContact
{
    public static async Task<string> Run()
    {
        var room = UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();
        var player = RunDirector.Instance.Player;
        var before = player.State.Class;
        var camera = player.GetComponent<HumanPreviewCamera>();
        bool switchedCamera = !camera.ThirdPerson;
        var panel = typeof(CombatTestRoom).GetMethod("SetPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            if (!room.ChangeClass(PlayerClass.Shinobi)) throw new Exception("Shinobi unavailable");
            if (switchedCamera) camera.Toggle();
            panel.Invoke(room, new object[] { false });
            await Task.Delay(300);
            var combat = player.GetComponent<PlayerCombat>();
            if (!camera.HeldWeapon || !combat.TryAttack()) throw new Exception("Cannot start katana attack");
            float deadline = Time.realtimeSinceStartup + 4;
            TrailRenderer blade = null;
            while (!blade && Time.realtimeSinceStartup < deadline)
            {
                blade = camera.HeldWeapon.GetComponentInChildren<BladeContactTrail>()?.GetComponent<TrailRenderer>();
                if (!blade || !blade.emitting) { blade = null; await Task.Delay(10); }
            }
            if (!blade || !blade.sharedMaterial.shader.isSupported || !blade.sharedMaterial.mainTexture)
                throw new Exception("Missing rendered blade mask at contact");
            float reach = Vector3.Distance(camera.HeldWeapon.position, blade.transform.position);
            if (reach < .1f || reach > 2f) throw new Exception("Trail outside blade: " + reach);
            if (!player.HandleDodgePress(player.transform.forward) || blade.emitting)
                throw new Exception("Shift did not stop blade trail");
            return "PASS contact on held katana at " + reach.ToString("F2") + " m; supported textured material; Shift stopped emission";
        }
        finally
        {
            if (switchedCamera && camera.ThirdPerson) camera.Toggle();
            room.ChangeClass(before);
            panel.Invoke(room, new object[] { true });
        }
    }
}
