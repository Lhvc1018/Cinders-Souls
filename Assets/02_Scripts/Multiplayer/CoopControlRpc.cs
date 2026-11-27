using Fusion;
namespace Codice.Multiplayer
{
    /* Small control messages bypass the bulk world stream. Damage remains authoritative. */
    public sealed class CoopControlRpc : SimulationBehaviour
    {
        [Rpc(RpcSources.All,RpcTargets.All,Channel=RpcChannel.Unreliable,TickAligned=false,InvokeLocal=false)]
        public static void RPC_PoseChunk(NetworkRunner runner,[RpcTarget] PlayerRef target,int frame,int part,int count,byte[] payload,RpcInfo info=default)
        {
            var transport=runner.GetComponent<FusionCoopTransport>();
            if(transport)transport.ReceivePoseChunk(info.Source,frame,part,count,payload);
        }
        [Rpc(RpcSources.All,RpcTargets.All,Channel=RpcChannel.Reliable,TickAligned=false,InvokeLocal=false)]
        public static void RPC_Control(NetworkRunner runner,[RpcTarget] PlayerRef target,byte[] payload,RpcInfo info=default)
        {
            var transport=runner.GetComponent<FusionCoopTransport>();
            if(transport)transport.ReceiveControl(info.Source,payload);
        }
    }
}
