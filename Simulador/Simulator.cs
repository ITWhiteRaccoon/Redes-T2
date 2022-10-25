using Spectre.Console;

namespace Simulador;

public class Simulator
{
    private Dictionary<string, Node> _nodes;
    private Dictionary<string, Router> _routers;
    private List<RouterTableEntry> _routerTable;

    /*
    private string ArpRequest = $"Note over {srcName} : ARP Request<br/>Who has {dstIP}? Tell {srcIP}";
    private string ArpReply = $"{srcName} ->> {dstName} : ARP Reply<br/>{srcIP} is at {srcMAC}";
    private string ICMPEchoRequest = $"{srcName} ->> {dstName} : ICMP Echo Request<br/>src={srcIP} dst={dstIP} ttl={TTL}";
    private string ICMPEchoReply = $"{srcName} ->> {dstName} : ICMP Echo Reply<br/>src={srcIP} dst={dstIP} ttl={TTL}";
    private string ICMPTimeExceeded = $"{srcName} ->> {dstName} : ICMP Time Exceeded<br/>src={srcIP} dst={dstIP} ttl={TTL}";
    */

    public Simulator(string topologyFilePath)
    {
        _nodes = new Dictionary<string, Node>();
        _routers = new Dictionary<string, Router>();
        _routerTable = new List<RouterTableEntry>();

        var file = File.ReadAllLines(topologyFilePath);
        var currentMode = ReadingMode.node;

        for (var i = 0; i < file.Length; i++)
        {
            var line = file[i].Trim();
            if (line.StartsWith('#'))
            {
                currentMode = Enum.Parse<ReadingMode>(line[1..].ToLower());
                AnsiConsole.MarkupLine($"Reading [yellow]{currentMode}[/] configuration");
            }
            else
            {
                var content = line.Split(',');
                switch (currentMode)
                {
                    case ReadingMode.node:
                        _nodes[content[0].Trim()] = new Node(content[1].Trim(), content[2].Trim(), content[3].Trim());
                        AnsiConsole.MarkupLine($"Added node [yellow]{content[0]}[/] {_nodes[content[0].Trim()]}");
                        break;
                    case ReadingMode.router:
                        var numOfPorts = int.Parse(content[1].Trim());
                        var ports = new Port[numOfPorts];
                        for (var j = 0; j < numOfPorts; j++)
                        {
                            ports[j] = new Port(content[j + 2].Trim(), content[j + 3].Trim());
                        }

                        _routers[content[0].Trim()] = new Router(ports);
                        AnsiConsole.MarkupLine($"Added router [yellow]{content[0]}[/] {_routers[content[0].Trim()]}");
                        break;
                    case ReadingMode.routertable:
                        _routerTable.Add(new RouterTableEntry(content[0].Trim(), content[1].Trim(), content[2].Trim(),
                            content[3].Trim()));
                        AnsiConsole.MarkupLine($"Added to router table {_routerTable.Last()}");
                        break;
                }
            }
        }
    }
}