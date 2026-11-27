using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
namespace Codice.Multiplayer
{
    // Host-authoritative gameplay snapshots use Fusion's fragmented data stream.
    // The existing motor predicts the local player; remote actors interpolate snapshots.
    public sealed class FusionCoopTransport:MonoBehaviour,INetworkRunnerCallbacks
    {
        public NetworkRunner Runner {get;private set;}
        public bool IsHost {get;private set;}
        public event Action<LanWire> Connected;
        public event Action<string> Closed;
        readonly Dictionary<PlayerRef,LanWire> wires=new Dictionary<PlayerRef,LanWire>();
        LanWire server;int sequence;bool closing;
        public async Task<LanWire> StartSession(bool host,string room)
        {
            IsHost=host;Runner=gameObject.AddComponent<NetworkRunner>();Runner.AddCallbacks(this);Runner.ProvideInput=false;
            if(!host)server=new LanWire(s=>Send(PlayerRef.None,s));
            var result=await Runner.StartGame(new StartGameArgs{GameMode=host?GameMode.Host:GameMode.Client,SessionName=room,PlayerCount=4,IsVisible=false,EnableClientSessionCreation=false});
            if(!result.Ok){await Stop();throw new InvalidOperationException("Photon Fusion: "+result.ShutdownReason);}
            Debug.Log("[Fusion coop] Connected topology="+(host?"Host":"Client")+" session="+room);return server;
        }
        public async Task Stop(){if(closing)return;closing=true;foreach(var w in wires.Values)w.Dispose();server?.Dispose();if(Runner)await Runner.Shutdown();if(this)Destroy(gameObject);}
        void OnDestroy(){foreach(var w in wires.Values)w.Dispose();server?.Dispose();}
        void Send(PlayerRef player,string value)
        {
            if(!Runner||!Runner.IsRunning||closing)return;
            var raw=Encoding.UTF8.GetBytes(value);if(raw.Length>LanWire.MaxFrame)return;
            using var stream=new MemoryStream();using(var zip=new DeflateStream(stream,System.IO.Compression.CompressionLevel.Fastest,true))zip.Write(raw,0,raw.Length);
            var bytes=stream.ToArray();var key=ReliableKey.FromInts(++sequence,0,0,0);
            if(IsHost)Runner.SendReliableDataToPlayer(player,key,bytes);else Runner.SendReliableDataToServer(key,bytes);
        }
        public void OnReliableDataReceived(NetworkRunner runner,PlayerRef player,ReliableKey key,ReadOnlySpan<byte> data)
        {
            try
            {
                if(data.Length>LanWire.MaxFrame)throw new InvalidDataException();
                using var input=new MemoryStream(data.ToArray());using var zip=new DeflateStream(input,CompressionMode.Decompress);using var output=new MemoryStream();var buffer=new byte[4096];int read;
                while((read=zip.Read(buffer,0,buffer.Length))>0){if(output.Length+read>LanWire.MaxFrame)throw new InvalidDataException();output.Write(buffer,0,read);}
                var text=Encoding.UTF8.GetString(output.ToArray());if(IsHost){if(wires.TryGetValue(player,out var wire))wire.Receive(text);}else server?.Receive(text);
            }
            catch(Exception){if(IsHost)runner.Disconnect(player);else Closed?.Invoke("Datos de red incompatibles.");}
        }
        public void OnPlayerJoined(NetworkRunner runner,PlayerRef player)
        {
            if(!IsHost||player==runner.LocalPlayer)return;
            var wire=new LanWire(text=>Send(player,text));wires[player]=wire;Connected?.Invoke(wire);
        }
        public void OnPlayerLeft(NetworkRunner runner,PlayerRef player){if(wires.TryGetValue(player,out var wire)){wire.Dispose();wires.Remove(player);}}
        public void OnShutdown(NetworkRunner runner,ShutdownReason reason){foreach(var wire in wires.Values)wire.Dispose();server?.Dispose();if(!closing)Closed?.Invoke("ConexiÃ³n cerrada: "+reason);}
        public void OnDisconnectedFromServer(NetworkRunner runner,NetDisconnectReason reason){server?.Dispose();if(!closing)Closed?.Invoke("AnfitriÃ³n desconectado: "+reason);}
        public void OnConnectRequest(NetworkRunner runner,NetworkRunnerCallbackArgs.ConnectRequest request,byte[] token){if(wires.Count<3)request.Accept();else request.Refuse();}
        public void OnConnectFailed(NetworkRunner runner,NetAddress address,NetConnectFailedReason reason){}
        public void OnInput(NetworkRunner runner,NetworkInput input){}
        public void OnInputMissing(NetworkRunner runner,PlayerRef player,NetworkInput input){}
        public void OnConnectedToServer(NetworkRunner runner){}
        public void OnSessionListUpdated(NetworkRunner runner,List<SessionInfo> sessions){}
        public void OnCustomAuthenticationResponse(NetworkRunner runner,Dictionary<string,object> data){}
        public void OnHostMigration(NetworkRunner runner,HostMigrationToken token){Closed?.Invoke("El anfitriÃ³n saliÃ³. Crea otra partida.");}
        public void OnSceneLoadDone(NetworkRunner runner){}
        public void OnSceneLoadStart(NetworkRunner runner){}
        public void OnReliableDataProgress(NetworkRunner runner,PlayerRef player,ReliableKey key,float progress){}
        public void OnObjectExitAOI(NetworkRunner runner,NetworkObject obj,PlayerRef player){}
        public void OnObjectEnterAOI(NetworkRunner runner,NetworkObject obj,PlayerRef player){}
        public void OnUserSimulationMessage(NetworkRunner runner,SimulationMessagePtr message){}
    }
}
