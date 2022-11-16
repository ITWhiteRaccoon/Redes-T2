using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Node : INetworkDevice
{
    public string Name { get; }
    public Port Port { get; }
    public IPAddress Gateway { get; }
    public Dictionary<IPAddress, PhysicalAddress> ArpTable { get; }
    
    public Port GetPort(IPAddress requesterIp)
    {
        return Port;
    }

    public Node(string name, Port port, IPAddress gateway)
    {
        Name = name;
        Port = port;
        Gateway = gateway;
        ArpTable = new Dictionary<IPAddress, PhysicalAddress>();
    }

    public override string ToString()
    {
        return $"(MAC: {BitConverter.ToString(Port.Mac.GetAddressBytes())}, IP: {Port.Ip}, Gateway: {Gateway})";
    }
}