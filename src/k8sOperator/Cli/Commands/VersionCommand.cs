using k8s.Operator.Configuration;

namespace k8s.Operator.Cli.Commands;

[OperatorCommand("version", "Display version information", 0, "-v", "--version")]
public class VersionCommand(OperatorConfiguration config) : IOperatorCommand
{
    public Task<int> ExecuteAsync(string[] args)
    {
        Console.WriteLine($"{config.Name} v{config.Version}");
        Console.WriteLine($"{config.Container.FullImage()}");
        return Task.FromResult(0);
    }
}

