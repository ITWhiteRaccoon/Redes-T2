using System.Net;
using System.Net.NetworkInformation;

namespace Simulador;

public class Node : INetComponent
{
    public string Name { get; }
    public Port Port { get; }
    public int Mask { get; }
    public IPAddress Gateway { get; }
    public Dictionary<IPAddress, PhysicalAddress> ArpTable { get; }

    public Node(string name, Port port, int mask, IPAddress gateway)
    {
        Name = name;
        Port = port;
        Mask = mask;
        Gateway = gateway;
        ArpTable = new Dictionary<IPAddress, PhysicalAddress>();
    }

    public override string ToString()
    {
        return $"(MAC: {BitConverter.ToString(Port.Mac.GetAddressBytes())}, IP: {Port.Ip}, Gateway: {Gateway})";
    }
}