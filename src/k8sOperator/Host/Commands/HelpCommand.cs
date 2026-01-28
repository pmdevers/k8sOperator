using k8s.Operator;
using k8s.Operator.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using static k8s.Operator.Helpers.ConsoleHelpers;

namespace k8s.Operator.Host.Commands;

[OperatorArgument(
    Command = "help",
    Description = "Display help information",
    Aliases = ["-h", "--help"],
    Order = -1)]
public class HelpCommand(IHost host, IEnumerable<Type> commandTypes) : IOperatorCommand
{
    private readonly IEnumerable<Type> _commandTypes = commandTypes;

    public Task RunAsync(string[] args)
    {
        var config = host.Services.GetRequiredService<OperatorConfiguration>();

        var operatorName = config.OperatorName;

        Console.WriteLine($"Welcome to the help for {operatorName}.");
        Console.WriteLine();
        Console.WriteLine($"{BOLD}USAGE:{NORMAL}");
        Console.WriteLine($"  {GREY}{operatorName.ToLowerInvariant()} {WHITE} {BOLD}[options] {YELLOW}<command> {NORMAL}");
        Console.WriteLine();
        Console.WriteLine($"{BOLD}COMMANDS:{NORMAL}");

        var commands = _commandTypes
            .Select(t => t.GetCustomAttribute<OperatorArgumentAttribute>())
            .Where(a => a != null)
            .OrderBy(a => a!.Order)
            .ThenBy(a => a!.Command);

        foreach (var cmd in commands)
        {
            var commandName = cmd!.Command;
            commandName = commandName.PadRight(25);
            Console.WriteLine($"  {YELLOW}{commandName}{BOLD}{NORMAL}{cmd.Description}{NORMAL}");
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }
}
