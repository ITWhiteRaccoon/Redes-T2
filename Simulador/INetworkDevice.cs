using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public interface INetworkDevice
{
    public string Name { get; }
    public IPAddress? Gateway { get; }
    public Dictionary<IPAddress, PhysicalAddress> ArpTable { get; }
    public Port GetPort(IPAddress requesterIp);
}