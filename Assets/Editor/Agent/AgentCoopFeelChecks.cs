using System;
using System.Net;
using Codice.Characters;
using Codice.Multiplayer;
using UnityEngine;
public static class AgentCoopFeelChecks
{
    public static string Run()
    {
        foreach(var ip in new[]{"192.168.1.20","10.0.0.8","172.16.0.3","255.255.255.255","0.0.0.0"})
            if(LanRoomCode.Decode(LanRoomCode.Encode(IPAddress.Parse(ip)))!=ip)throw new Exception("LAN code roundtrip "+ip);
        if(LanRoomCode.Encode(IPAddress.Parse("192.168.1.20"))!="LUNA20"||
           LanRoomCode.Decode("luna20")!="192.168.1.20"||
           LanRoomCode.Decode("LAN-AAAA-AAAA")!="0.0.0.0")
            throw new Exception("Short or legacy room code failed");
        var state=new CharacterState();
        var fields=typeof(CharacterState).GetField("weapons",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var weapon=new WeaponInstance(WeaponKind.Sword,1);fields.SetValue(state,new[]{weapon,(WeaponInstance)null});
        var saved=state.Capture();state.Restore(saved);
        if(!ReferenceEquals(state.ActiveWeapon,weapon))throw new Exception("Snapshot replaced unchanged weapon");
        var original=new GameObject("Health bar clone test");
        GameObject clone=null;
        try
        {
            Codice.UI.WorldHealthBar.Create(original.transform);
            clone=UnityEngine.Object.Instantiate(original);
            var worldBar=Codice.UI.WorldHealthBar.Create(clone.transform);
            if(clone.GetComponentsInChildren<Codice.UI.WorldHealthBar>(true).Length!=1||
               !worldBar.Health.Ghost.transform.IsChildOf(clone.transform))throw new Exception("Cloned enemy bar lost its bindings");
            worldBar.Health.Set(30,100);
        }
        finally
        {
            if(clone)UnityEngine.Object.DestroyImmediate(clone);
            UnityEngine.Object.DestroyImmediate(original);
        }
        return "PASS cloned health bar + LAN code roundtrips + unchanged weapon reference survives snapshot";
    }
}
