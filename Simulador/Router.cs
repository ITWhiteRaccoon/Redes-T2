namespace Simulador;

public class Router
{
    public int NumberOfPorts { get; }
    public string[] PortMac { get; }
    public string[] PortIp { get; }
    public RouterTableEntry[] RoutingTable { get; }

    /// Maps known IP addresses to their MAC addresses
    public Dictionary<string, string> ArpTable { get; }


    public Router(int numberOfPorts, string[] portMac, string[] portIp, RouterTableEntry[] routingTable)
    {
        NumberOfPorts = numberOfPorts;
        PortMac = portMac;
        PortIp = portIp;
        RoutingTable = routingTable;
        ArpTable = new Dictionary<string, string>();
    }

    public override string ToString()
    {
        return $"";
    }
}