using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simulador;

public class Program : Command<Program.Settings>
{
    public static void Main(string[] args)
    {
        var app = new CommandApp<Program>();
        app.Run(args);
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        AnsiConsole.MarkupLine(
            $"simulador <[yellow]{settings.TopologyFilePath}[/]> <[yellow]{settings.Command}[/]> <[yellow]{settings.Source}[/]> <[yellow]{settings.Destination}[/]>");
        var simulator = new Simulator(settings.TopologyFilePath);
        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<topologia>")] public string TopologyFilePath { get; set; }

        [CommandArgument(1, "<comando>")] public CommandType Command { get; set; }

        [CommandArgument(2, "<origem>")] public string Source { get; set; }

        [CommandArgument(3, "<destino>")] public string Destination { get; set; }
    }
}