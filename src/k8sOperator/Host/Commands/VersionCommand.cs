using k8s.Operator;
using System.Reflection;

namespace k8s.Operator.Host.Commands;

[OperatorArgument(
    Command = "version",
    Description = "Display version information",
    Aliases = ["-v", "--version"],
    Order = 0)]
public class VersionCommand : IOperatorCommand
{
    public Task RunAsync(string[] args)
    {
        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? assembly?.GetName().Version?.ToString() ?? "1.0.0";

        var operatorName = assembly?.GetName().Name ?? "Operator";

        Console.WriteLine($"{operatorName} v{version}");
        return Task.CompletedTask;
    }
}
