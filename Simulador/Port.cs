using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Port
{
    public PhysicalAddress Mac { get; }
    public IPAddress Ip { get; }
    public int Mask { get; }

    public Port(PhysicalAddress mac, IPAddress ip,int mask)
    {
        Mac = mac;
        Ip = ip;
        Mask = mask;
    }

    public override string ToString()
    {
        return $"(MAC: {BitConverter.ToString(Mac.GetAddressBytes())}, IP: {Ip}/{Mask})";
    }
}