namespace Simulador;

public static class Messages
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