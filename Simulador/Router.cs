using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Router : INetComponent
{
    public string Name { get; }
    public List<Port> Ports { get; }
    public List<RouterTableEntry> RouterTable { get; }

    /// Maps known IP addresses to their MAC addresses
    public Dictionary<IPAddress, PhysicalAddress> ArpTable { get; }


    public Router(string name, int numberOfPorts, List<Port> ports)
    {
        Name = name;
        Ports = ports;
        RouterTable = new List<RouterTableEntry>(numberOfPorts);
        ArpTable = new Dictionary<IPAddress, PhysicalAddress>();
    }

    public override string ToString()
    {
        return $"";
    }
}