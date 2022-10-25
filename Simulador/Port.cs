namespace Simulador;

public class Port
{
    public string Mac { get; }
    public string Ip { get; }

    public Port(string mac, string ip)
    {
        Mac = mac.ToLower();
        Ip = ip;
    }

    public override string ToString()
    {
        return $"(MAC: {Mac}, IP: {Ip})";
    }
}