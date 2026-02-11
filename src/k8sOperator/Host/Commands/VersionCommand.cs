using k8s.Operator.Configuration;

namespace k8s.Operator.Host.Commands;

[OperatorArgument(
    Command = "version",
    Description = "Display version information",
    Aliases = ["-v", "--version"],
    Order = 0)]
public class VersionCommand(OperatorConfiguration config) : IOperatorCommand
{
    public Task RunAsync(string[] args)
    {
        Console.WriteLine($"{config.Name} v{config.Version}");
        return Task.CompletedTask;
    }
}
