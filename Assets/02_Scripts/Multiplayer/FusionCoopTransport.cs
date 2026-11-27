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
    // Reliable gameplay events and disposable pose chunks use separate Fusion channels.
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
            if(!host)server=new LanWire(s=>Send(PlayerRef.None,s,false),s=>Send(PlayerRef.None,s,true));
            // Everyone must land on the SAME Photon region or a guest never finds the host's room
            // (each PC otherwise picks its own "best" region). South America first: the group plays from Bolivia.
            var settings=Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();settings.FixedRegion=Region;
            var config=NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.Simulation.TickRateSelection=new TickRate.Selection{Client=60,ClientSendInterval=1,ServerTickInterval=1,ServerSendInterval=1};
            var result=await Runner.StartGame(new StartGameArgs{Scene=new NetworkSceneInfo(),Config=config,GameMode=host?GameMode.Host:GameMode.Client,SessionName=room,PlayerCount=4,IsVisible=false,EnableClientSessionCreation=false,CustomPhotonAppSettings=settings});
            if(!result.Ok){await Stop();throw new InvalidOperationException(Explain(result.ShutdownReason,host));}
            /* This project streams gameplay through the reliable data API, not native Networked properties.
               Reserve reliable bandwidth for authoritative events and resynchronization. */
            int previousDataRate=Runner.ReliableDataSendRate;Runner.ReliableDataSendRate=60;
            Debug.Log("[Fusion coop] Reliable data cadence "+previousDataRate+" -> "+Runner.ReliableDataSendRate+" Hz");
            Debug.Log("[Fusion coop] Connected topology="+(host?"Host":"Client")+" session="+room);return server;
        }
        public const string Region="sa";
        static string Explain(ShutdownReason reason,bool host)
        {
            switch(reason)
            {
                case ShutdownReason.GameNotFound:return "No existe una sala con ese código. Revisa el código y que el anfitrión siga en el lobby.";
                case ShutdownReason.GameIsFull:return "La sala está llena (máximo 4).";
                case ShutdownReason.GameClosed:return "Esa partida ya empezó; pide al anfitrión que cree otra.";
                case ShutdownReason.GameIdAlreadyExists:return "Ese código ya está en uso. Vuelve a crear la partida.";
                case ShutdownReason.InvalidAuthentication:case ShutdownReason.CustomAuthenticationFailed:return "Photon rechazó la conexión (App ID). Avisa al desarrollador.";
                case ShutdownReason.PhotonCloudTimeout:case ShutdownReason.ConnectionTimeout:case ShutdownReason.ServerInRoom:return "No hay respuesta de Photon. Revisa tu internet o el firewall y prueba otra vez.";
                case ShutdownReason.MaxCcuReached:return "Se alcanzó el límite de jugadores del servidor gratuito. Prueba en unos minutos.";
                default:return "No se pudo conectar ("+reason+"). Revisa internet y que ambos tengan la misma versión.";
            }
        }
        public async Task Stop(){if(closing)return;closing=true;foreach(var w in wires.Values)w.Dispose();server?.Dispose();if(Runner)await Runner.Shutdown();if(this)Destroy(gameObject);}
        void OnDestroy(){foreach(var w in wires.Values)w.Dispose();server?.Dispose();}
        public static int LastCompressed;
        /// Host only: "45 ms directo" / "120 ms relé" per guest (Photon punches a direct link when the routers allow it).
        float linkRefresh;string cachedLink;
        public string LinkInfo()
        {
            if(!Runner||!Runner.IsRunning||!IsHost)return null;if(Time.unscaledTime<linkRefresh)return cachedLink;linkRefresh=Time.unscaledTime+.5f;var parts=new List<string>();
            foreach(var p in wires.Keys){try{var type=Runner.GetPlayerConnectionType(p);parts.Add((Runner.GetPlayerRtt(p)*1000).ToString("0")+" ms "+(type==ConnectionType.Direct?"directo":type==ConnectionType.Relayed?"relé":type.ToString()));}catch{}}
            cachedLink=parts.Count>0?string.Join(" · ",parts):null;return cachedLink;
        }
        public void Drop(LanWire wire)
        {
            if(!IsHost||!Runner||!Runner.IsRunning)return;
            PlayerRef target=PlayerRef.None;
            foreach(var pair in wires)if(pair.Value==wire){target=pair.Key;break;}
            if(target!=PlayerRef.None)
            {
                wire.Dispose();
                if(Runner.GetPlayerConnectionType(target)!=ConnectionType.None)Runner.Disconnect(target);
            }
        }
        public float GetWireRttMs(LanWire wire)
        {
            if(!Runner||!Runner.IsRunning||!IsHost||wire==null)return -1;
            foreach(var entry in wires)if(entry.Value==wire)return (float)(Runner.GetPlayerRtt(entry.Key)*1000);
            return -1;
        }
        readonly Dictionary<PlayerRef,CoopPoseFrames> fastFrames=new Dictionary<PlayerRef,CoopPoseFrames>();
        int poseSequence;
        void Send(PlayerRef player,string value,bool disposable)
        {
            if(!Runner||!Runner.IsRunning||closing)return;
            /* Fusion removes its simulation connection before OnPlayerLeft reaches gameplay. */
            if(IsHost)
            {
                if(!wires.TryGetValue(player,out var wire)||wire.Closed)return;
                if(Runner.GetPlayerConnectionType(player)==ConnectionType.None)return;
            }
            else if(server==null||server.Closed||!Runner.HasAnyActiveConnections())return;
            var raw=Encoding.UTF8.GetBytes(value);if(raw.Length>LanWire.MaxFrame)return;
            using var stream=new MemoryStream();using(var zip=new DeflateStream(stream,System.IO.Compression.CompressionLevel.Fastest,true))zip.Write(raw,0,raw.Length);
            var bytes=stream.ToArray();LastCompressed=bytes.Length;
            var target=IsHost?player:PlayerRef.None;
            if(disposable)
            {
                if(bytes.Length>LanWire.MaxFrame)return;
                int frame=++poseSequence;
                int count=(bytes.Length+CoopPoseFrames.ChunkSize-1)/CoopPoseFrames.ChunkSize;
                for(int part=0;part<count;part++)
                {
                    int offset=part*CoopPoseFrames.ChunkSize;
                    var chunk=count==1?bytes:new byte[Math.Min(CoopPoseFrames.ChunkSize,bytes.Length-offset)];
                    if(count>1)Buffer.BlockCopy(bytes,offset,chunk,0,chunk.Length);
                    CoopControlRpc.RPC_PoseChunk(Runner,target,frame,part,count,chunk);
                }
                return;
            }
            if(bytes.Length<=400){CoopControlRpc.RPC_Control(Runner,target,bytes);return;}
            var key=ReliableKey.FromInts(++sequence,0,0,0);
            if(IsHost)Runner.SendReliableDataToPlayer(player,key,bytes);else Runner.SendReliableDataToServer(key,bytes);
        }
        public void ReceiveControl(PlayerRef source,byte[] data)
        {
            if(!Runner||closing||data==null||data.Length>400)return;
            if(IsHost&&!wires.ContainsKey(source)||!IsHost&&source!=PlayerRef.None)return;
            OnReliableDataReceived(Runner,source,default,data);
        }
        public void ReceivePoseChunk(PlayerRef source,int frame,int part,int count,byte[] payload)
        {
            if(!Runner||closing||(IsHost?!wires.ContainsKey(source):source!=PlayerRef.None))return;
            if(!fastFrames.TryGetValue(source,out var frames))fastFrames[source]=frames=new CoopPoseFrames();
            var complete=frames.Push(frame,part,count,payload,Time.unscaledTime);
            if(complete!=null)ReceiveFrame(Runner,source,complete,true);
        }
        public void OnReliableDataReceived(NetworkRunner runner,PlayerRef player,ReliableKey key,ReadOnlySpan<byte> data)
        {
            if(closing||(IsHost&&!wires.ContainsKey(player)))return;
            ReceiveFrame(runner,player,data,false);
        }
        void ReceiveFrame(NetworkRunner runner,PlayerRef player,ReadOnlySpan<byte> data,bool disposable)
        {
            try
            {
                if(data.Length>LanWire.MaxFrame)throw new InvalidDataException();
                using var input=new MemoryStream(data.ToArray());using var zip=new DeflateStream(input,CompressionMode.Decompress);using var output=new MemoryStream();var buffer=new byte[4096];int read;
                while((read=zip.Read(buffer,0,buffer.Length))>0){if(output.Length+read>LanWire.MaxFrame)throw new InvalidDataException();output.Write(buffer,0,read);}
                var text=Encoding.UTF8.GetString(output.ToArray());
                if(disposable&&!text.StartsWith(IsHost?"{\"type\":\"motion\"":"{\"type\":\"poses\"",StringComparison.Ordinal))return;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if(CoopNetworkSimulation.Enqueue(text,()=>Deliver(player,text)))return;
#endif
                Deliver(player,text);
            }
            catch(Exception){if(disposable)return;if(IsHost)runner.Disconnect(player);else Closed?.Invoke("Datos de red incompatibles.");}
        }
        void Deliver(PlayerRef player,string text){if(closing)return;if(IsHost){if(wires.TryGetValue(player,out var wire))wire.Receive(text);}else server?.Receive(text);}
        public void OnPlayerJoined(NetworkRunner runner,PlayerRef player)
        {
            if(!IsHost||player==runner.LocalPlayer)return;
            var wire=new LanWire(text=>Send(player,text,false),text=>Send(player,text,true));wires[player]=wire;Connected?.Invoke(wire);
        }
        public void OnPlayerLeft(NetworkRunner runner,PlayerRef player){if(wires.TryGetValue(player,out var wire)){wire.Dispose();wires.Remove(player);fastFrames.Remove(player);}}
        public void OnShutdown(NetworkRunner runner,ShutdownReason reason){foreach(var wire in wires.Values)wire.Dispose();server?.Dispose();if(!closing)Closed?.Invoke("Conexión cerrada: "+reason);}
        public void OnDisconnectedFromServer(NetworkRunner runner,NetDisconnectReason reason){server?.Dispose();if(!closing)Closed?.Invoke("Anfitrión desconectado: "+reason);}
        public void OnConnectRequest(NetworkRunner runner,NetworkRunnerCallbackArgs.ConnectRequest request,byte[] token){if(wires.Count<3)request.Accept();else request.Refuse();}
        public void OnConnectFailed(NetworkRunner runner,NetAddress address,NetConnectFailedReason reason){}
        public void OnInput(NetworkRunner runner,NetworkInput input){}
        public void OnInputMissing(NetworkRunner runner,PlayerRef player,NetworkInput input){}
        public void OnConnectedToServer(NetworkRunner runner){}
        public void OnSessionListUpdated(NetworkRunner runner,List<SessionInfo> sessions){}
        public void OnCustomAuthenticationResponse(NetworkRunner runner,Dictionary<string,object> data){}
        public void OnHostMigration(NetworkRunner runner,HostMigrationToken token){Closed?.Invoke("El anfitrión salió. Crea otra partida.");}
        public void OnSceneLoadDone(NetworkRunner runner){}
        public void OnSceneLoadStart(NetworkRunner runner){}
        public void OnReliableDataProgress(NetworkRunner runner,PlayerRef player,ReliableKey key,float progress){}
        public void OnObjectExitAOI(NetworkRunner runner,NetworkObject obj,PlayerRef player){}
        public void OnObjectEnterAOI(NetworkRunner runner,NetworkObject obj,PlayerRef player){}
        public void OnUserSimulationMessage(NetworkRunner runner,SimulationMessagePtr message){}
    }

    /* Per sender. Incomplete frames expire independently; no lost chunk can block a newer frame. */
    internal sealed class CoopPoseFrames
    {
        public const int ChunkSize=400;
        const int Capacity=4;
        const int MaxChunks=(LanWire.MaxFrame+ChunkSize-1)/ChunkSize;
        sealed class Frame
        {
            public int id,received;
            public float created;
            public byte[][] chunks;
        }
        readonly Frame[] pending=new Frame[Capacity];
        int lastDelivered;
        public byte[] Push(int id,int part,int count,byte[] bytes,float now)
        {
            if(id<=lastDelivered||count<1||count>MaxChunks||part<0||part>=count||bytes==null||
                bytes.Length<1||bytes.Length>ChunkSize||(part<count-1&&bytes.Length!=ChunkSize)||
                (part==count-1&&part*ChunkSize+bytes.Length>LanWire.MaxFrame))return null;
            int slot=-1,empty=-1,oldest=0;
            for(int i=0;i<pending.Length;i++)
            {
                var candidate=pending[i];
                if(candidate==null||candidate.id<=lastDelivered||now-candidate.created>.5f)
                {
                    pending[i]=null;empty=i;continue;
                }
                if(candidate.id==id)slot=i;
                if(pending[oldest]==null||candidate.id<pending[oldest].id)oldest=i;
            }
            if(slot<0)
            {
                if(empty<0&&id<pending[oldest].id)return null;
                slot=empty>=0?empty:oldest;
                pending[slot]=new Frame{id=id,created=now,chunks=new byte[count][]};
            }
            var frame=pending[slot];
            if(frame.chunks.Length!=count||frame.chunks[part]!=null)return null;
            frame.chunks[part]=bytes;
            if(++frame.received<count)return null;
            var result=count==1?bytes:new byte[(count-1)*ChunkSize+frame.chunks[count-1].Length];
            if(count>1)for(int i=0;i<count;i++)Buffer.BlockCopy(frame.chunks[i],0,result,i*ChunkSize,frame.chunks[i].Length);
            lastDelivered=id;
            for(int i=0;i<pending.Length;i++)if(pending[i]!=null&&pending[i].id<=id)pending[i]=null;
            return result;
        }
    }

}
