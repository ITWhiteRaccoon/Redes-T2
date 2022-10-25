namespace Simulador;

public class RouterTableEntry
{
    public string RouterName { get; }
    public string Destination { get; }
    public string NextHop { get; }
    public string Port { get; }

    public RouterTableEntry(string routerName,string destination, string nextHop, string port)
    {
        RouterName = routerName.ToLower();
        Destination = destination;
        NextHop = nextHop;
        Port = port;
    }

    public override string ToString()
    {
        return $"(Router: {RouterName}, Destination: {Destination}, NextHop: {NextHop}, Port: {Port})";
    }
}