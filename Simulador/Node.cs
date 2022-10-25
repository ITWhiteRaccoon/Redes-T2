namespace Simulador;

public class Node
{
    public Port Port { get; }
    public string Gateway { get; }

    public Node(string mac, string ip, string gateway)
    {
        Port = new Port(mac.ToLower(), ip);
        Gateway = gateway;
    }

    public override string ToString()
    {
        return $"(Port: {Port}, Gateway: {Gateway})";
    }
}