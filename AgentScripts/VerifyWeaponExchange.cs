using System;
using System.Reflection;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
using UnityEngine;

public static class VerifyWeaponExchange
{
    public static async Task<string> Run()
    {
        var room=UnityEngine.Object.FindAnyObjectByType<CombatTestRoom>();
        var player=RunDirector.Instance.Player;room.ChangeClass(PlayerClass.Warrior);
        var mode=player.GetComponent<HumanPreviewCamera>();if(!mode.ThirdPerson)mode.Toggle();
        typeof(CombatTestRoom).GetMethod("SetPanel",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(room,new object[]{false});
        await Task.Delay(200);
        player.GetComponent<PlayerCombat>().TryAttack();await Task.Delay(350);
        player.State.EquipIntoSlot(new WeaponInstance(WeaponKind.Dagger),0);player.State.SelectWeapon(0);
        await Task.Delay(200);
        var grip=mode.HeldWeapon;var guard=grip.GetComponent<WeaponGroundGuard>();
        if(!guard||!mode.OffhandWeapon)throw new Exception("Dagger or shield missing");
        var late=typeof(WeaponGroundGuard).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
        late.Invoke(guard,null);var rotation=grip.localRotation;var scale=grip.localScale;
        for(int i=0;i<120;i++)late.Invoke(guard,null);
        if(Quaternion.Angle(rotation,grip.localRotation)>.01f||Vector3.Distance(scale,grip.localScale)>.0001f)throw new Exception("Weapon grip accumulates rotation or scale");
        room.ChangeClass(PlayerClass.Warrior);
        return "PASS attack -> dagger + shield; 120 stationary ground corrections preserve orientation and scale";
    }
}
