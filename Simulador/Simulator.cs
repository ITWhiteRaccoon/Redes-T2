using Spectre.Console;

namespace Simulador;

public class Simulator
{
    private Dictionary<string, Node> _nodes;
    private Dictionary<string, Router> _routers;
    private List<RouterTableEntry> _routerTable;


    private static class Messages
    {
        public static string ArpRequest =
            "[yellow]Note[/] over [yellow]{0}[/] : [blue]ARP Request[/]<br/>Who has [green]{1}[/]? Tell [green]{2}[/]";

        public static string ArpReply =
            "[yellow]{0}[/] ->> [yellow]{1}[/] : [blue]ARP Reply[/]<br/>[green]{2}[/] is at [green]{3}[/]";

        public static string ICMPEchoRequest =
            "[yellow]{0}[/] ->> [yellow]{1}[/] : [blue]ICMP Echo Request[/]<br/>src=[green]{2}[/] dst=[green]{3}[/] ttl=[green]{4}[/]";

        public static string ICMPEchoReply =
            "[yellow]{0}[/] ->> [yellow]{1}[/] : [blue]ICMP Echo Reply[/]<br/>src=[green]{2}[/] dst=[green]{3}[/] ttl=[green]{4}[/]";

        public static string ICMPTimeExceeded =
            "[yellow]{0}[/] ->> [yellow]{1}[/] : [blue]ICMP Time Exceeded[/]<br/>src=[green]{2}[/] dst=[green]{3}[/] ttl=[green]{4}[/]";
    }

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

#if DEBUG
                AnsiConsole.MarkupLine($"Reading [yellow]{currentMode}[/] configuration");
#endif
            }
            else
            {
                var content = line.Split(',');
                switch (currentMode)
                {
                    case ReadingMode.node:
                        _nodes[content[0].Trim()] = new Node(content[1].Trim(), content[2].Trim(), content[3].Trim());
                        break;
                    case ReadingMode.router:
                        var numOfPorts = int.Parse(content[1].Trim());
                        var ports = new Port[numOfPorts];
                        for (var j = 0; j < numOfPorts; j++)
                        {
                            ports[j] = new Port(content[j * 2 + 2].Trim(), content[j * 2 + 3].Trim());
                        }

                        _routers[content[0].Trim()] = new Router(ports);
                        break;
                    case ReadingMode.routertable:
                        _routerTable.Add(new RouterTableEntry(content[0].Trim(), content[1].Trim(), content[2].Trim(),
                            content[3].Trim()));
                        break;
                    default:
                        AnsiConsole.MarkupLine("How did you get here? This topology file must be [red]invalid[/].");
                        break;
                }
            }
        }

#if DEBUG
        AnsiConsole.MarkupLine("Finished configuration:");
        PrintNodes();
        PrintRouters();
        PrintRouterTable();
#endif
    }

    public void Ping(string srcName, string dstName)
    {
        var srcNode = _nodes[srcName];
        var dstNode = _nodes[dstName];

        AnsiConsole.MarkupLine(string.Format(Messages.ArpRequest, srcName, dstNode.Port.Ip, srcNode.Port.Ip));
    }

    public void Traceroute(string srcName, string dstName)
    {
        var srcNode = _nodes[srcName];
        var dstNode = _nodes[dstName];

        AnsiConsole.MarkupLine($"");
    }

    public void PrintNodes()
    {
        var table = new Table
        {
            Title = new TableTitle("Nodes", new Style(decoration: Decoration.Bold | Decoration.Underline)),
            Border = TableBorder.Rounded
        };

        table.AddColumn("Node");
        table.AddColumn("MAC");
        table.AddColumn("IP");
        table.AddColumn("Gateway");

        foreach (var entry in _nodes)
        {
            table.AddRow(entry.Key, entry.Value.Port.Mac, entry.Value.Port.Ip, entry.Value.Gateway);
        }

        AnsiConsole.Write(table);
    }

    public void PrintRouters()
    {
        var table = new Table
        {
            Title = new TableTitle("Routers", new Style(decoration: Decoration.Bold | Decoration.Underline)),
            Border = TableBorder.Rounded
        };

        var maxNumberOfPorts = _routers.Max(x => x.Value.Ports.Length);

        table.AddColumn("Router");
        table.AddColumn("N. of ports");
        for (var i = 0; i < maxNumberOfPorts; i++)
        {
            table.AddColumn($"MAC{i}");
            table.AddColumn($"IP{i}");
        }

        foreach (var entry in _routers)
        {
            var row = new string[entry.Value.NumberOfPorts * 2 + 2];
            row[0] = entry.Key;
            row[1] = entry.Value.NumberOfPorts.ToString();
            for (var i = 0; i < entry.Value.NumberOfPorts; i++)
            {
                row[i * 2 + 2] = entry.Value.Ports[i].Mac;
                row[i * 2 + 3] = entry.Value.Ports[i].Ip;
            }

            table.AddRow(row);
        }

        AnsiConsole.Write(table);
    }

    public void PrintRouterTable()
    {
        var table = new Table
        {
            Title = new TableTitle("Router Table", new Style(decoration: Decoration.Bold | Decoration.Underline)),
            Border = TableBorder.Rounded
        };

        table.AddColumn("Router");
        table.AddColumn("Destination");
        table.AddColumn("Next Hop");
        table.AddColumn("Port");

        foreach (var entry in _routerTable)
        {
            table.AddRow(entry.RouterName, entry.Destination, entry.NextHop, entry.Port);
        }

        AnsiConsole.Write(table);
    }
}