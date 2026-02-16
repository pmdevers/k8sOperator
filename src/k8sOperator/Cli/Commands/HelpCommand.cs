using k8s.Operator.Configuration;
using System.Reflection;
using static k8s.Operator.Cli.Helpers.ConsoleHelpers;

namespace k8s.Operator.Cli.Commands;

[OperatorCommand("help", "Display help information", -1, "-h", "--help")]
public class HelpCommand(OperatorConfiguration config, CommandRegistry registry) : IOperatorCommand
{
    public Task<int> ExecuteAsync(string[] args)
    {
        var operatorName = config.Name;

        Console.WriteLine($"Welcome to the help for {operatorName}.");
        Console.WriteLine();
        Console.WriteLine($"{BOLD}USAGE:{NORMAL}");
        Console.WriteLine($"  {GREY}{operatorName.ToLowerInvariant()} {WHITE} {BOLD}[options] {YELLOW}<command> {NORMAL}");
        Console.WriteLine();
        Console.WriteLine($"{BOLD}COMMANDS:{NORMAL}");

        var commands = registry.All()
            .Select(t => t.GetCustomAttribute<OperatorCommandAttribute>())
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
        return Task.FromResult(0);
    }
}
