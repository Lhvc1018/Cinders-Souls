using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codice.Multiplayer
{
    // Bounded, length-prefixed UTF-8 frames. Socket tasks never call Unity APIs.
    public sealed class LanWire : IDisposable
    {
        public const int Port = 26767, MaxFrame = 2 * 1024 * 1024;
        readonly TcpClient socket;
        readonly Action<string> fusionSend, fusionFast;
        public LanWire(Action<string> sender,Action<string> fastSender){fusionSend=sender;fusionFast=fastSender;}
        public void Receive(string text)
        {
            if(Closed||string.IsNullOrEmpty(text)||text.Length>MaxFrame)return;
            if(IsFast(text)){Interlocked.Exchange(ref latestPose,text);return;}
            if(Interlocked.Add(ref queuedChars,text.Length)>4*1024*1024){Dispose();return;}incoming.Enqueue(text);
        }
        readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
        readonly SemaphoreSlim wake = new SemaphoreSlim(0);
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        static bool IsFast(string text)=>text.StartsWith("{\"type\":\"poses\"",StringComparison.Ordinal)||text.StartsWith("{\"type\":\"motion\"",StringComparison.Ordinal);
        string latestPose;
        byte[] tcpPose;
        int disposed, queuedChars, queuedBytes;
        readonly SemaphoreSlim receiveSlots=new SemaphoreSlim(128,128);
        public bool Closed => Volatile.Read(ref disposed) != 0;
        public LanWire(TcpClient client)
        { socket=client;socket.NoDelay=true;_ = Read();_ = Write(); }
        public static async Task<LanWire> Connect(string address)
        {
            address=LanRoomCode.Decode(address);
            if(!IPAddress.TryParse(address,out var ip)||ip.AddressFamily!=AddressFamily.InterNetwork)throw new ArgumentException("Introduce una dirección IPv4 válida.");
            var client=new TcpClient();
            try { var connect=client.ConnectAsync(ip,Port);if(await Task.WhenAny(connect,Task.Delay(5000))!=connect)throw new TimeoutException("El anfitrión no responde.");await connect;return new LanWire(client); }
            catch { client.Close();throw; }
        }
        public bool TryRead(out string value){if(incoming.TryDequeue(out value)){Interlocked.Add(ref queuedChars,-value.Length);if(socket!=null)receiveSlots.Release();return true;}value=Interlocked.Exchange(ref latestPose,null);return value!=null;}
        public void SendFast(string value)
        {
            if(Closed)return;
            if(fusionFast!=null){fusionFast(value);return;}
            var bytes=Encoding.UTF8.GetBytes(value);
            if(bytes.Length==0||bytes.Length>MaxFrame)return;
            if(Interlocked.Exchange(ref tcpPose,bytes)==null)wake.Release();
        }
        public void Send(string value)
        {
            if(Closed)return;if(fusionSend!=null){fusionSend(value);return;}var bytes=Encoding.UTF8.GetBytes(value);
            if(bytes.Length==0||bytes.Length>MaxFrame||Interlocked.Add(ref queuedBytes,bytes.Length)>8*1024*1024){Dispose();return;}
            outgoing.Enqueue(bytes);wake.Release();
        }
        async Task ReadExact(NetworkStream stream,byte[] data)
        {int offset=0;while(offset<data.Length){int count=await stream.ReadAsync(data,offset,data.Length-offset,stop.Token).ConfigureAwait(false);if(count==0)throw new EndOfStreamException();offset+=count;}}
        async Task Read()
        {
            try {var stream=socket.GetStream();var header=new byte[4];while(!Closed){await ReadExact(stream,header).ConfigureAwait(false);int length=IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header,0));if(length<1||length>MaxFrame)throw new InvalidDataException();var bytes=new byte[length];await ReadExact(stream,bytes).ConfigureAwait(false);var text=Encoding.UTF8.GetString(bytes);if(!IsFast(text))await receiveSlots.WaitAsync(stop.Token).ConfigureAwait(false);Receive(text);}}
            catch { Dispose(); }
        }
        async Task Write()
        {
            try
            {
                var stream=socket.GetStream();
                while(!Closed)
                {
                    await wake.WaitAsync(stop.Token).ConfigureAwait(false);
                    if(outgoing.TryDequeue(out var bytes))Interlocked.Add(ref queuedBytes,-bytes.Length);
                    else bytes=Interlocked.Exchange(ref tcpPose,null);
                    if(bytes==null)continue;
                    var header=BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));
                    await stream.WriteAsync(header,0,4,stop.Token).ConfigureAwait(false);
                    await stream.WriteAsync(bytes,0,bytes.Length,stop.Token).ConfigureAwait(false);
                }
            }
            catch{Dispose();}
        }
        public void Dispose(){if(Interlocked.Exchange(ref disposed,1)!=0)return;stop.Cancel();socket?.Close();}
    }
}
