using System.Buffers;

namespace Simulador;

public class Node
{
    public string Name { get; }
    public string Ip { get; }
    public string Mask { get; }
    public string Gateway { get; }
    public Dictionary<string, string> ArpTable { get; }

    public Node(string name, string ip, string mask, string gateway)
    {
        Name = name;
        Ip = ip;
        Mask = mask;
        Gateway = gateway;
        ArpTable = new Dictionary<string, string>();
    }  
    private void ReceivePackage(Package p)
    {
        if (p.DstIp==Ip)
        {
            if (p.RequestType==RequestType.ArpRequest)
            {
                
            }
        }
    }

    public override string ToString()
    {
        return $"(Name: {Name}, Ip: {Ip}, Mask: {Mask}, Gateway: {Gateway})";
    }
}