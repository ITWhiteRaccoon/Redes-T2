using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using NetTools;
using Spectre.Console;

namespace Simulador;

public class Simulator
{
    private static readonly PhysicalAddress BroadcastMac = PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF");
    private Dictionary<PhysicalAddress, Node> _nodes;
    private Dictionary<string, Router> _routers;

    public Simulator(string topologyFilePath)
    {
        _nodes = new Dictionary<PhysicalAddress, Node>();
        _routers = new Dictionary<string, Router>();

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
                        var ip = content[2].Trim().Split("/");
                        _nodes[PhysicalAddress.Parse(content[1].Trim())] = new Node(
                            content[0].Trim(),
                            PhysicalAddress.Parse(content[1].Trim()),
                            IPAddress.Parse(ip[0]),
                            int.Parse(ip[1]),
                            IPAddress.Parse(content[3].Trim()));
                        break;
                    case ReadingMode.Router:
                        var numOfPorts = int.Parse(content[1].Trim());
                        var portMac = new PhysicalAddress[numOfPorts];
                        var portIp = new IPAddress[numOfPorts];
                        for (var j = 0; j < numOfPorts; j++)
                        {
                            portMac[j] = PhysicalAddress.Parse(content[j * 2 + 2].Trim());
                            portIp[j] = IPAddress.Parse(content[j * 2 + 3].Trim().Split('/')[0]);
                        }

                        _routers[content[0].Trim()] = new Router(content[0].Trim(), numOfPorts, portMac, portIp);
                        break;
                    case ReadingMode.RouterTable:
                        _routers[content[0].Trim()].RouterTable.Add(new RouterTableEntry(
                            IPAddressRange.Parse(content[1].Trim()), IPAddress.Parse(content[2].Trim()),
                            int.Parse(content[3].Trim())));
                        break;
                    default:
                        AnsiConsole.MarkupLine("How did you get here? This topology file must be [red]invalid[/].");
                        break;
                }
            }
        }

        AnsiConsole.MarkupLine("Finished configuration:");
        PrintNodes();
        //PrintRouters();
        //PrintRouterTable();
    }

    public void Ping(string srcName, string dstName)
    {
        var nodes = GetNodes(srcName, dstName);
        var pingSrc = nodes[0];
        var pingDst = nodes[1];

        var p = new Packet { SrcIp = pingSrc.Ip, DstIp = pingDst.Ip, SrcMac = pingSrc.Mac };
        if (!pingSrc.ArpTable.ContainsKey(pingDst.Ip))
        {
            p.RequestType = RequestType.ArpRequest;
            AnsiConsole.MarkupLine(string.Format(Messages.ArpRequest, pingSrc.Name, pingDst.Ip, pingSrc.Ip));
        }
        else
        {
            p.RequestType = RequestType.IcmpEchoRequest;
            p.DstMac = pingSrc.ArpTable[pingDst.Ip];
            AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoRequest, pingSrc.Name, pingDst.Name, pingSrc.Ip,
                pingDst.Ip, p.TTL));
        }

        while (p.TTL > 0)
        {
            switch (p.RequestType)
            {
                case RequestType.ArpRequest:
                {
                    var node = _nodes.First(x => x.Value.Ip.Equals(p.DstIp)).Value;
                    node.ArpTable[p.SrcIp] = p.SrcMac;
                    var newPacket = new Packet
                    {
                        RequestType = RequestType.ArpReply, SrcIp = node.Ip, DstIp = p.SrcIp, SrcMac = node.Mac,
                        DstMac = p.SrcMac, TTL = p.TTL
                    };
                    AnsiConsole.MarkupLine(string.Format(Messages.ArpReply, node.Name, pingSrc.Name, node.Ip,
                        BitConverter.ToString(node.Mac.GetAddressBytes()).Replace('-', ':')));
                    p = newPacket;
                    break;
                }
                case RequestType.ArpReply:
                {
                    var packetSrc = _nodes[p.SrcMac];
                    var packetDst = _nodes[p.DstMac];
                    packetDst.ArpTable[p.SrcIp] = p.SrcMac;
                    var newP = new Packet
                    {
                        RequestType = RequestType.IcmpEchoRequest, SrcIp = packetDst.Ip, DstIp = packetSrc.Ip,
                        SrcMac = packetDst.Mac, DstMac = packetSrc.Mac
                    };
                    p = newP;
                    AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoRequest, packetDst.Name, packetSrc.Name,
                        packetDst.Ip, packetSrc.Ip, p.TTL));
                    break;
                }
                case RequestType.IcmpEchoRequest:
                {
                    var packetSrc = _nodes[p.SrcMac];
                    var packetDst = _nodes[p.DstMac];
                    packetDst.ArpTable[p.SrcIp] = p.SrcMac;
                    var newP = new Packet
                    {
                        RequestType = RequestType.IcmpEchoReply, SrcIp = packetDst.Ip, DstIp = packetSrc.Ip,
                        SrcMac = packetDst.Mac, DstMac = packetSrc.Mac
                    };
                    p = newP;
                    AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoReply, packetDst.Name, packetSrc.Name,
                        packetDst.Ip, packetSrc.Ip, p.TTL));
                    break;
                }
                case RequestType.IcmpEchoReply:
                {
                    if (p.DstMac.Equals(pingSrc.Mac))
                    {
                        return;
                    }

                    break;
                }
                case RequestType.IcmpTimeExceeded:
                {
                    break;
                }
            }
        }
    }

    public void Traceroute(string srcName, string dstName)
    {
        var nodes = GetNodes(srcName, dstName);
    }

    private Node[] GetNodes(string srcName, string dstName)
    {
        var nodes = new Node[2];
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
                BitConverter.ToString(entry.Value.Mac.GetAddressBytes()).Replace('-', ':'),
                $"{entry.Value.Ip}/{entry.Value.Mask}",
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