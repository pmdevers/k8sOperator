using Simplicity.Operator.Configuration;

namespace Simplicity.Operator.Cli.Commands;

[OperatorCommand("version", "Display version information", 0, "-v", "--version")]
public class VersionCommand(OperatorConfiguration config) : IOperatorCommand
{
    public Task ExecuteAsync(string[] args)
    {
        Console.WriteLine($"{config.Name} v{config.Version}");
        return Task.CompletedTask;
    }
}

