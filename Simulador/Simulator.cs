using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using NetTools;
using Spectre.Console;

namespace Simulador;

public class Simulator
{
    private static PhysicalAddress BroadcastMac = PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF");
    private Dictionary<PhysicalAddress, Node> _nodes;
    private Dictionary<string, Router> _routers;
    private List<RouterTableEntry> _routerTable;

    public Simulator(string topologyFilePath)
    {
        _nodes = new Dictionary<PhysicalAddress, Node>();
        //_routers = new Dictionary<string, Router>();
        //_routerTable = new List<RouterTableEntry>();

        var file = File.ReadAllLines(topologyFilePath);
        var currentMode = ReadingMode.Node;

        for (var i = 0; i < file.Length; i++)
        {
            var line = file[i].Trim();
            if (line.StartsWith('#'))
            {
                currentMode = Enum.Parse<ReadingMode>(line[1..], true);

                AnsiConsole.MarkupLine($"Reading [yellow]{currentMode}[/] configuration");
            }
            else
            {
                var content = line.Split(',');
                switch (currentMode)
                {
                    case ReadingMode.Node:
                        _nodes[PhysicalAddress.Parse(content[1].Trim())] = new Node(
                            content[0].Trim(),
                            PhysicalAddress.Parse(content[1].Trim()),
                            IPAddressRange.Parse(content[2].Trim()),
                            IPAddress.Parse(content[3].Trim()));
                        break;
                    /*case ReadingMode.router:
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
                        break;*/
                    default:
                        AnsiConsole.MarkupLine("How did you get here? This topology file must be [red]invalid[/].");
                        break;
                }
            }
        }

        AnsiConsole.MarkupLine("Finished configuration:");
        PrintNodes();
        // PrintRouters();
        // PrintRouterTable();
    }

    private void SendPackage(Package p)
    {
    }

    public void Ping(string srcName, string dstName)
    {
        Node srcNode = null, dstNode = null;


        //AnsiConsole.MarkupLine(string.Format(Messages.ArpRequest, srcName, dstNode.Port.Ip, srcNode.Port.Ip));
    }

    public void Traceroute(string srcName, string dstName)
    {
        //var srcNode = _nodes[srcName];
        //var dstNode = _nodes[dstName];

        //AnsiConsole.MarkupLine($"");
    }

    public Node[] GetNodes(string srcName, string dstName)
    {
        Node[] nodes = new Node[2];
        foreach (var node in _nodes)
        {
            if (node.Value.Name == srcName)
            {
                nodes[0] = node.Value;
            }
            else if (node.Value.Name == dstName)
            {
                nodes[1] = node.Value;
            }

            if (nodes[0] != null && nodes[1] != null)
            {
                break;
            }
        }

        return nodes;
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
            table.AddRow(
                entry.Value.Name,
                BitConverter.ToString(entry.Value.Mac.GetAddressBytes()),
                entry.Value.Ip.ToCidrString(),
                entry.Value.Gateway.ToString());
        }

        AnsiConsole.Write(table);
    }

    /*public void PrintRouters()
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
    }*/
}