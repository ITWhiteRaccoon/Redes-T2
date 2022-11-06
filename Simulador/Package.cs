using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Package
{
    public RequestType RequestType { get; set; }
    public PhysicalAddress SrcMac { get; set; }
    public PhysicalAddress DstMac { get; set; }
    public IPAddress SrcIp { get; set; }
    public IPAddress DstIp { get; set; }
    public int TTL { get; set; }

    public Package(RequestType requestType, PhysicalAddress srcMac, PhysicalAddress dstMac, IPAddress srcIp, IPAddress dstIp, int ttl = 8)
    {
        RequestType = requestType;
        SrcMac = srcMac;
        DstMac = dstMac;
        SrcIp = srcIp;
        DstIp = dstIp;
        TTL = ttl;
    }
}