namespace Simulador;

public class Router
{
    public Port[] Ports { get; }
    public RouterTableEntry[] RoutingTable { get; }

    public int NumberOfPorts => Ports.Length;

    public Router(Port[] ports)
    {
        Ports = ports;
    }

    public override string ToString()
    {
        return $"(Ports: {string.Join(',', (IEnumerable<Port>)Ports)})";
    }
}