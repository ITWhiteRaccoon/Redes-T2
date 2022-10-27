namespace Simulador;

public class RouterTableEntry
{
    public string Destination { get; }
    public string NextHop { get; }
    public int Port { get; }

    public RouterTableEntry(string destination, string nextHop, int port)
    {
        Destination = destination;
        NextHop = nextHop;
        Port = port;
    }

    public override string ToString()
    {
        return $"(Destination: {Destination}, NextHop: {NextHop}, Port: {Port})";
    }
}