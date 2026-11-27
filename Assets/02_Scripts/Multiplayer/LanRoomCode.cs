using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
namespace Codice.Multiplayer
{
    public static class LanRoomCode
    {
        const string Letters="ABCDEFGHJKLMNPQRSTUVWXYZ";
        public static string Encode(IPAddress ip)
        {
            var b=ip.GetAddressBytes();if(b.Length!=4)throw new ArgumentException("IPv4 requerida");
            if(b[0]==192 && b[1]==168 && b[2]<=2)
                return new[]{"SOL","LUNA","MAR"}[b[2]]+b[3];
            if(b[0]==10 && b[1]==0 && b[2]==0)return "NUBE"+b[3];
            uint n=((uint)b[0]<<24)|((uint)b[1]<<16)|((uint)b[2]<<8)|b[3];
            var chars=new char[8];for(int i=7;i>=0;i--){chars[i]=Letters[(int)(n%(uint)Letters.Length)];n/=(uint)Letters.Length;}
            return new string(chars,1,7);
        }
        public static string Decode(string code)
        {
            if(IPAddress.TryParse(code,out var direct)&&direct.AddressFamily==AddressFamily.InterNetwork)return direct.ToString();
            var clean=(code??"").Trim().ToUpperInvariant();if(clean.StartsWith("LAN-"))clean=clean.Substring(4);clean=clean.Replace("-","").Replace(" ","");
            foreach(var prefix in new[]{"SOL","LUNA","MAR","NUBE"})
                if(clean.StartsWith(prefix) && byte.TryParse(clean.Substring(prefix.Length),out var last))
                    return (prefix=="NUBE"?"10.0.0.":prefix=="SOL"?"192.168.0.":prefix=="LUNA"?"192.168.1.":"192.168.2.")+last;
            if(clean.Length!=8 && clean.Length!=7)throw new ArgumentException("Escribe el código LAN que muestra el anfitrión.");
            ulong n=0;foreach(char c in clean){int digit=Letters.IndexOf(c);if(digit<0)throw new ArgumentException("Código LAN inválido.");n=n*(uint)Letters.Length+(uint)digit;}
            if(n>uint.MaxValue)throw new ArgumentException("Código LAN inválido.");
            return new IPAddress(new[]{(byte)(n>>24),(byte)(n>>16),(byte)(n>>8),(byte)n}).ToString();
        }
        public static string LocalCodes()
        {
            var addresses=Dns.GetHostAddresses(Dns.GetHostName())
                .Where(a=>a.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)).Distinct().ToList();
            if(addresses.Count>1){
                try{
                    var routed=System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                        .Where(n=>n.OperationalStatus==System.Net.NetworkInformation.OperationalStatus.Up)
                        .Select(n=>n.GetIPProperties())
                        .Where(p=>p.GatewayAddresses.Any(g=>g.Address.AddressFamily==AddressFamily.InterNetwork&&!g.Address.Equals(IPAddress.Any)))
                        .SelectMany(p=>p.UnicastAddresses.Select(u=>u.Address)).ToArray();
                    addresses=addresses.OrderByDescending(a=>routed.Contains(a)).ToList();
                }catch(System.Net.NetworkInformation.NetworkInformationException){ /* DNS addresses remain usable. */ }
                 catch(PlatformNotSupportedException){ /* Some players cannot enumerate gateways. */ }
            }
            return string.Join("\n",addresses.Select(Encode));
        }
    }
}
