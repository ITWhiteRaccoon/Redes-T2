using Spectre.Console.Cli;
namespace Simulador;

public class Program
{
    
    public static void Main(string[] args)
    {
        var app = new CommandApp<Simulator>();
        app.Run(args);
    }
}