using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Packet
{
    public RequestType RequestType { get; set; }
    public PhysicalAddress SrcMac { get; set; }
    public PhysicalAddress DstMac { get; set; }
    public IPAddress SrcIp { get; set; }
    public IPAddress DstIp { get; set; }
    public int TTL { get; set; }

    public Packet(int ttl = 8)
    {
        TTL = ttl;
    }
}