using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Port
{
    public PhysicalAddress Mac { get; }
    public IPAddress Ip { get; }

    public Port(PhysicalAddress mac, IPAddress ip)
    {
        Mac = mac;
        Ip = ip;
    }
}