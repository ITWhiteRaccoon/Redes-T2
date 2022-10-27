namespace Simulador;

public enum RequestType
{
    ArpRequest,
    ArpReply,
    IcmpEchoRequest,
    IcmpEchoReply,
    IcmpTimeExceeded
}

public enum CommandType
{
    Ping,
    Traceroute
}

public enum ReadingMode
{
    Node,
    Router,
    Routertable
}