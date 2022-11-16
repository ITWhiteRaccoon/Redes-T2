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
    private Node _commandSrc;
    private Node _commandDst;

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
                    {
                        var ip = content[2].Trim().Split("/");
                        var node = new Node(
                            content[0].Trim(),
                            new Port(PhysicalAddress.Parse(content[1].Trim()), IPAddress.Parse(ip[0]),
                                int.Parse(ip[1])),
                            IPAddress.Parse(content[3].Trim())
                        );
                        _nodesByMac[node.Port.Mac] = node;
                        _nodesByIp[node.Port.Ip] = node;
                        _nodesByName[node.Name] = node;
                        break;
                    }
                    case ReadingMode.Router:
                    {
                        var numOfPorts = int.Parse(content[1].Trim());
                        var ports = new List<Port>(numOfPorts);
                        for (var j = 0; j < numOfPorts; j++)
                        {
                            var ip = content[j * 2 + 3].Trim().Split('/');
                            ports.Add(new Port(
                                PhysicalAddress.Parse(content[j * 2 + 2].Trim()),
                                IPAddress.Parse(ip[0]),
                                int.Parse(ip[1])
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
                    }
                    case ReadingMode.RouterTable:
                    {
                        _routersByName[content[0].Trim()].RouterTable.Add(new RouterTableEntry(
                            IPAddressRange.Parse(content[1].Trim()),
                            IPAddress.Parse(content[2].Trim()),
                            int.Parse(content[3].Trim())
                        ));
                        break;
                    }
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

    private INetworkDevice GetDevice(IPAddress ip)
    {
        return _nodesByIp.ContainsKey(ip) ? _nodesByIp[ip] : _routersByIp[ip];
    }

    private INetworkDevice GetDevice(PhysicalAddress mac)
    {
        return _nodesByMac.ContainsKey(mac) ? _nodesByMac[mac] : _routersByMac[mac];
    }

    private List<Node> GetAllNodes()
    {
        return _nodesByName.Values.ToList();
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

    private List<Router> GetAllRouters()
    {
        return _routersByName.Values.ToList();
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
        _commandSrc = GetNode(srcName);
        _commandDst = GetNode(dstName);

        //Source IP and MAC will always start with the source node
        //Destination IP will be the destination node's IP if it's in the same subnet, otherwise it will be the gateway
        var p = new Packet
        {
            SrcIp = _commandSrc.Port.Ip, SrcMac = _commandSrc.Port.Mac,
            DstIp = new IPAddressRange(_commandSrc.Port.Ip, _commandSrc.Port.Mask).Contains(_commandDst.Port.Ip)
                ? _commandDst.Port.Ip
                : _commandSrc.Gateway
        };

        if (!_commandSrc.ArpTable.ContainsKey(p.DstIp))
        {
            //If the source node doesn't have the destination node's IP in its ARP table, it will send an ARP request
            p.RequestType = RequestType.ArpRequest;
            AnsiConsole.MarkupLine(string.Format(Messages.ArpRequest, _commandSrc.Name, p.DstIp, p.SrcIp));
        }
        else
        {
            //If the source node has the destination node's IP in its ARP table, it will send an ICMP request
            p.RequestType = RequestType.IcmpEchoRequest;
            p.DstMac = _commandSrc.ArpTable[_commandDst.Port.Ip];
            AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoRequest, _commandSrc.Name, _commandDst.Name,
                _commandSrc.Port.Ip, _commandDst.Port.Ip, p.TTL));
        }

        var unfinished = true;

        //After setting the initial packet, it will be forwarded to the destination and be treated accordingly
        while (p.TTL > 0 && unfinished)
        {
            p = ProcessPackage(p, out unfinished);
        }
    }

    private Packet NewIcmpEchoRequest(Packet p)
    {
        var srcDevice = GetDevice(p.SrcIp);
        var srcPort = srcDevice.GetPort(p.SrcIp);

        var newP = new Packet
        {
            SrcIp = srcPort.Ip, SrcMac = srcPort.Mac,
            DstIp = (new IPAddressRange(srcPort.Ip, srcPort.Mask).Contains(p.DstIp)
                ? srcPort.Ip
                : srcDevice.Gateway) ?? throw new InvalidOperationException()
        };

        if (srcDevice.ArpTable.ContainsKey(newP.DstIp))
        {
            newP.DstMac = srcDevice.ArpTable[newP.DstIp];
        }
    }

    private Packet ProcessPackage(Packet p, out bool unfinished)
    {
        unfinished = true;
        switch (p.RequestType)
        {
            case RequestType.ArpRequest:
            {
                p = TreatArpRequest(p);
                break;
            }
            case RequestType.ArpReply:
            {
                p = TreatArpReply(p);
                break;
            }
            case RequestType.IcmpEchoRequest:
            {
                p = TreatIcmpEchoRequest(p);
                break;
            }
            case RequestType.IcmpEchoReply:
            {
                if (p.DstMac.Equals(_commandSrc.Port.Mac))
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

    private Packet TreatArpRequest(Packet p)
    {
        var netDevice = GetDevice(p.DstIp);
        var port = netDevice.GetPort(p.DstIp);

        netDevice.ArpTable[p.SrcIp] = p.SrcMac;

        var newPacket = new Packet
        {
            RequestType = RequestType.ArpReply, SrcIp = port.Ip, DstIp = p.SrcIp, SrcMac = port.Mac,
            DstMac = p.SrcMac, TTL = p.TTL
        };

        AnsiConsole.MarkupLine(string.Format(Messages.ArpReply, netDevice.Name, _commandSrc.Name, port.Ip,
            BitConverter.ToString(port.Mac.GetAddressBytes()).Replace('-', ':')));
        p = newPacket;
        return p;
    }

    private Packet TreatArpReply(Packet p)
    {
        var packetSrc = GetDevice(p.SrcMac);
        var packetDst = GetDevice(p.DstMac);
        var srcPort = packetSrc.GetPort(p.SrcIp);
        var dstPort = packetDst.GetPort(p.DstIp);

        packetDst.ArpTable[p.SrcIp] = p.SrcMac;
        var newP = new Packet
        {
            RequestType = RequestType.IcmpEchoRequest, SrcIp = dstPort.Ip, DstIp = srcPort.Ip, SrcMac = dstPort.Mac,
            DstMac = srcPort.Mac
        };
        p = newP;
        AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoRequest, packetDst.Name, packetSrc.Name, dstPort.Ip,
            srcPort.Ip, p.TTL));
        return p;
    }

    private Packet TreatIcmpEchoRequest(Packet p)
    {
        var packetSrc = GetDevice(p.SrcMac);
        var packetDst = GetDevice(p.DstMac);
        var srcPort = packetSrc.GetPort(p.SrcIp);
        var dstPort = packetDst.GetPort(p.DstIp);

        packetDst.ArpTable[p.SrcIp] = p.SrcMac;
        var newP = new Packet
        {
            RequestType = RequestType.IcmpEchoRequest, SrcIp = dstPort.Ip, DstIp = srcPort.Ip, SrcMac = dstPort.Mac,
            DstMac = srcPort.Mac
        };
        p = newP;
        AnsiConsole.MarkupLine(string.Format(Messages.IcmpEchoReply, packetDst.Name, packetSrc.Name, dstPort.Ip,
            srcPort.Ip, p.TTL));
        return p;
    }

    public void Traceroute(string srcName, string dstName)
    {
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

        foreach (var node in GetAllNodes())
        {
            table.AddRow(
                node.Name,
                BitConverter.ToString(node.Port.Mac.GetAddressBytes()).Replace('-', ':'),
                $"{node.Port.Ip}/{node.Mask}",
                node.Gateway.ToString());
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