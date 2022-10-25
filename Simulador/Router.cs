namespace Simulador;

public class Router
{
    public Port[] Ports { get; }

    public Router(Port[] ports)
    {
        Ports = ports;
    }

    public override string ToString()
    {
        return $"(Ports: {string.Join(',', (IEnumerable<Port>)Ports)})";
    }
}