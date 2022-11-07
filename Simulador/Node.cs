using System.Net;
using System.Net.NetworkInformation;
using NetTools;

namespace Simulador;

public class Node
{
    public string Name { get; set; }
    public PhysicalAddress Mac { get; }
    public IPAddress Ip { get; }
    public int Mask { get; }
    public IPAddress Gateway { get; }
    public Dictionary<IPAddress, PhysicalAddress> ArpTable { get; }

    public Node(string name, PhysicalAddress mac, IPAddress ip, int mask, IPAddress gateway)
    {
        Name = name;
        Mac = mac;
        Ip = ip;
        Mask = mask;
        Gateway = gateway;
        ArpTable = new Dictionary<IPAddress, PhysicalAddress>();
        ArpTable[IPAddress.Parse("192.168.1.0")] = PhysicalAddress.Parse("00:00:00:00:00:01");
    }

    public override string ToString()
    {
        return $"(MAC: {BitConverter.ToString(Mac.GetAddressBytes())}, IP: {Ip}, Gateway: {Gateway})";
    }
}