using System;
using System.Threading.Tasks;
using Fusion;
using Codice.Multiplayer;
using UnityEngine;
public static class ProbeFusion
{
    public static async Task<string> Run()
    {
        var config=NetworkProjectConfig.Global;var previous=config.PeerMode;config.PeerMode=NetworkProjectConfig.PeerModes.Multiple;
        var host=new GameObject("Fusion verification host").AddComponent<FusionCoopTransport>();var client=new GameObject("Fusion verification client").AddComponent<FusionCoopTransport>();
        string room="CindersProbe-"+Guid.NewGuid().ToString("N").Substring(0,10);LanWire hostWire=null;
        host.Connected+=wire=>hostWire=wire;
        try
        {
            await host.StartSession(true,room);var clientWire=await client.StartSession(false,room);
            for(int i=0;i<100&&hostWire==null;i++)await Task.Delay(50);
            if(hostWire==null)throw new Exception("No Fusion peer callback");
            var start=DateTime.UtcNow;clientWire.Send("Cinders authority request");string request=null;
            for(int i=0;i<100;i++){if(hostWire.TryRead(out request))break;await Task.Delay(20);}
            if(request!="Cinders authority request")throw new Exception("Client -> Host failed");
            string payload=new string('x',100000);hostWire.Send(payload);string reply=null;
            for(int i=0;i<150;i++){if(clientWire.TryRead(out reply))break;await Task.Delay(20);}
            if(reply!=payload)throw new Exception("Host -> Client 100KB payload mismatch");
            string result="Two live Photon Fusion runners connected; bidirectional delivery and compressed 100KB payload passed. Round trip including polling: "+(DateTime.UtcNow-start).TotalMilliseconds.ToString("0")+" ms. This is a transport check, not a full-game latency guarantee.";
            System.IO.File.WriteAllText("Docs/Fusion_TransportValidation.txt",result);return result;
        }
        finally{await client.Stop();await host.Stop();config.PeerMode=previous;}
    }
}
