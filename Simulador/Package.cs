namespace Simulador;

public class Package
{
    public RequestType RequestType { get; set; }
    public string SrcMac { get; set; }
    public string DstMac { get; set; }
    public string SrcIp { get; set; }
    public string DstIp { get; set; }
    public int TTL { get; set; }

    public Package(RequestType requestType, string srcMac, string dstMac, string srcIp, string dstIp, int ttl = 8)
    {
        RequestType = requestType;
        SrcMac = srcMac;
        DstMac = dstMac;
        SrcIp = srcIp;
        DstIp = dstIp;
        TTL = ttl;
    }
}