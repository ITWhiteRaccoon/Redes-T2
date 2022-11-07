using System.Net;
using NetTools;

namespace Simulador;

public class RouterTableEntry
{
    public IPAddressRange Destination { get; }
    public IPAddress NextHop { get; }
    public int Port { get; }

    public RouterTableEntry(IPAddressRange destination, IPAddress nextHop, int port)
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