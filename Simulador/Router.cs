using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Router
{
    public string Name { get; }
    public int NumberOfPorts { get; }
    public PhysicalAddress[] PortMac { get; }
    public IPAddress[] PortIp { get; }
    public List<RouterTableEntry> RouterTable { get; }

    /// Maps known IP addresses to their MAC addresses
    public Dictionary<string, string> ArpTable { get; }


    public Router(string name, int numberOfPorts, PhysicalAddress[] portMac, IPAddress[] portIp)
    {
        Name = name;
        NumberOfPorts = numberOfPorts;
        PortMac = portMac;
        PortIp = portIp;
        RouterTable = new List<RouterTableEntry>(numberOfPorts);
        ArpTable = new Dictionary<string, string>();
    }

    public override string ToString()
    {
        return $"";
    }
}