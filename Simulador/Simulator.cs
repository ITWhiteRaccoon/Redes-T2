using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using NetTools;
using Spectre.Console;

namespace Simulador;

public class Simulator
{
    private static readonly PhysicalAddress BroadcastMac = PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF");
    private Dictionary<PhysicalAddress, Node> _nodesByMac;
    private Dictionary<IPAddress, Node> _nodesByIp;
    private Dictionary<string, Node> _nodesByName;
    private Dictionary<PhysicalAddress, Router> _routersByMac;
    private Dictionary<IPAddress, Router> _routersByIp;
    private Dictionary<string, Router> _routersByName;

    public Simulator(string topologyFilePath)
    {
        _nodesByMac = new Dictionary<PhysicalAddress, Node>();
        _nodesByIp = new Dictionary<IPAddress, Node>();
        _nodesByName = new Dictionary<string, Node>();
        _routersByMac = new Dictionary<PhysicalAddress, Router>();
        _routersByIp = new Dictionary<IPAddress, Router>();
        _routersByName = new Dictionary<string, Router>();

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
                        var node = new Node(
                            content[0].Trim(),
                            new Port(PhysicalAddress.Parse(content[1].Trim()), IPAddress.Parse(ip[0])),
                            int.Parse(ip[1]),
                            IPAddress.Parse(content[3].Trim())
                        );
                        _nodesByMac[node.Port.Mac] = node;
                        _nodesByIp[node.Port.Ip] = node;
                        _nodesByName[node.Name] = node;
                        break;
                    case ReadingMode.Router:
                        var numOfPorts = int.Parse(content[1].Trim());
                        var ports = new List<Port>(numOfPorts);
                        for (var j = 0; j < numOfPorts; j++)
                        {
                            ports.Add(new Port(
                                PhysicalAddress.Parse(content[j * 2 + 2].Trim()),
                                IPAddress.Parse(content[j * 2 + 3].Trim().Split('/')[0])
                            ));
                        }

                        var router = new Router(content[0].Trim(), numOfPorts, ports);
                        _routersByName[router.Name] = router;
                        foreach (var port in ports)
                        {
                            _routersByMac[port.Mac] = router;
                            _routersByIp[port.Ip] = router;
                        }

                        break;
                    case ReadingMode.RouterTable:
                        _routersByName[content[0].Trim()].RouterTable.Add(new RouterTableEntry(
                            IPAddressRange.Parse(content[1].Trim()),
                            IPAddress.Parse(content[2].Trim()),
                            int.Parse(content[3].Trim())
                        ));
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

    private Node? GetNode(PhysicalAddress mac)
    {
        return _nodesByMac.TryGetValue(mac, out var node) ? node : null;
    }

    private Node? GetNode(IPAddress ip)
    {
        return _nodesByIp.TryGetValue(ip, out var node) ? node : null;
    }

    private Node? GetNode(string name)
    {
        return _nodesByName.TryGetValue(name, out var node) ? node : null;
    }

    private Router? GetRouter(PhysicalAddress mac)
    {
        return _routersByMac.TryGetValue(mac, out var router) ? router : null;
    }

    private Router? GetRouter(IPAddress ip)
    {
        return _routersByIp.TryGetValue(ip, out var router) ? router : null;
    }

    private Router? GetRouter(string name)
    {
        return _routersByName.TryGetValue(name, out var router) ? router : null;
    }

    public void Ping(string srcName, string dstName)
    {
        var nodes = GetNodes(srcName, dstName);
        var commandSrc = nodes[0];
        var commandDst = nodes[1];

        //Source IP and MAC will always start with the source node
        //Destination IP will be the destination node's IP if it's in the same subnet, otherwise it will be the gateway
        var p = new Packet
        {
            SrcIp = commandSrc.Port.Ip, SrcMac = commandSrc.Port.Mac,
            DstIp = new IPAddressRange(commandSrc.Port.Ip, commandSrc.Mask).Contains(commandDst.Port.Ip)
                ? commandDst.Port.Ip
                : commandSrc.Gateway
        };

        if (!commandSrc.ArpTable.ContainsKey(p.DstIp))
        {
            //If the source node doesn't have the destination node's IP in its ARP table, it will send an ARP request
            p.RequestType = RequestType.ArpRequest;
            AnsiConsole.MarkupLine(string.Format(Messages.ArpRequest, commandSrc.Name, p.DstIp, p.SrcIp));
        }
        else
        {
            //If the source node has the destination node's IP in its ARP table, it will send an ICMP request
            p.RequestType = RequestType.IcmpEchoRequest;
            p.DstMac = commandSrc.ArpTable[commandDst.Port.Ip];
            AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoRequest, commandSrc.Name, commandDst.Name,
                commandSrc.Port.Ip, commandDst.Port.Ip, p.TTL));
        }

        var unfinished = true;

        //After setting the initial packet, it will be forwarded to the destination and be treated accordingly
        while (p.TTL > 0 && unfinished)
        {
            p = ProcessPackage(p, commandSrc, commandDst, out unfinished);
        }
    }

    private Packet ProcessPackage(Packet p, Node commandSrc, Node commandDst, out bool unfinished)
    {
        unfinished = true;
        switch (p.RequestType)
        {
            case RequestType.ArpRequest:
            {
                p = ArpRequest(p, commandSrc, commandDst);
                break;
            }
            case RequestType.ArpReply:
            {
                p = ArpReply(p);
                break;
            }
            case RequestType.IcmpEchoRequest:
            {
                p = IcmpEchoRequest(p);
                break;
            }
            case RequestType.IcmpEchoReply:
            {
                if (p.DstMac.Equals(commandSrc.Port.Mac))
                {
                    unfinished = false;
                    return p;
                }

                break;
            }
            case RequestType.IcmpTimeExceeded:
            {
                break;
            }
        }

        return p;
    }

    private Packet NewArpRequest(Packet p, Node commandSrc, Node commandDst)
    {
        var isNode = _nodes.FirstOrDefault(x => x.Value.Port.Ip.Equals(p.DstIp)).Equals(null);
        var nodePort = _nodes.First(x => x.Value.Port.Ip.Equals(p.DstIp)).Value.Port;


        var routerPort = _routers.First(x => x.Value.Ports.Any(y => y.Ip.Equals(p.DstIp)));
    }

    private Packet ArpRequest(Packet p, Node commandSrc, Node commandDst)
    {
        var n = _nodes.FirstOrDefault(x => x.Value.Port.Ip.Equals(p.DstIp));
        if (n.Key != null && n.Value != null)
        {
            var node = n.Value;
            node.ArpTable[p.SrcIp] = p.SrcMac;
            var newPacket = new Packet
            {
                RequestType = RequestType.ArpReply, SrcIp = node.Port.Ip, DstIp = p.SrcIp, SrcMac = node.Port.Mac,
                DstMac = p.SrcMac, TTL = p.TTL
            };
            AnsiConsole.MarkupLine(string.Format(Messages.ArpReply, node.Name, commandSrc.Name, node.Port.Ip,
                BitConverter.ToString(node.Port.Mac.GetAddressBytes()).Replace('-', ':')));
            p = newPacket;
        }
        else
        {
            var r = _routers.FirstOrDefault(x => x.Value.Ports.Any(y => y.Ip.Equals(p.DstIp)));
            if (r.Key != null && r.Value != null)
            {
                var router = r.Value;
                var routerPort = router.Ports.First(x => x.Ip.Equals(p.DstIp));
                router.ArpTable[p.SrcIp] = p.SrcMac;
                var newPacket = new Packet
                {
                    RequestType = RequestType.ArpReply, SrcIp = routerPort.Ip, DstIp = p.SrcIp,
                    SrcMac = routerPort.Mac, DstMac = p.SrcMac, TTL = p.TTL
                };
                AnsiConsole.MarkupLine(string.Format(Messages.ArpReply, router.Name, commandSrc.Name, routerPort.Ip,
                    BitConverter.ToString(routerPort.Mac.GetAddressBytes()).Replace('-', ':')));
                p = newPacket;
            }
        }

        return p;
    }

    private Packet ArpReply(Packet p)
    {
        var packetSrc = _nodes[p.SrcMac];
        var packetDst = _nodes[p.DstMac];
        packetDst.ArpTable[p.SrcIp] = p.SrcMac;
        var newP = new Packet
        {
            RequestType = RequestType.IcmpEchoRequest, SrcIp = packetDst.Port.Ip, DstIp = packetSrc.Port.Ip,
            SrcMac = packetDst.Port.Mac, DstMac = packetSrc.Port.Mac
        };
        p = newP;
        AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoRequest, packetDst.Name, packetSrc.Name,
            packetDst.Port.Ip, packetSrc.Port.Ip, p.TTL));
        return p;
    }

    private Packet IcmpEchoRequest(Packet p)
    {
        var packetSrc = _nodes[p.SrcMac];
        var packetDst = _nodes[p.DstMac];
        packetDst.ArpTable[p.SrcIp] = p.SrcMac;
        var newP = new Packet
        {
            RequestType = RequestType.IcmpEchoReply, SrcIp = packetDst.Port.Ip, DstIp = packetSrc.Port.Ip,
            SrcMac = packetDst.Port.Mac, DstMac = packetSrc.Port.Mac
        };
        p = newP;
        AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoReply, packetDst.Name, packetSrc.Name,
            packetDst.Port.Ip, packetSrc.Port.Ip, p.TTL));
        return p;
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
                BitConverter.ToString(entry.Value.Port.Mac.GetAddressBytes()).Replace('-', ':'),
                $"{entry.Value.Port.Ip}/{entry.Value.Mask}",
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