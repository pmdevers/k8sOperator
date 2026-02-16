using Microsoft.Extensions.Hosting;

namespace k8s.Operator.Cli.Commands;

[OperatorCommand("operator", "Starts the operator.", -2)]
public class OperatorCommand(IHost app) : IOperatorCommand
{
    public Task ExecuteAsync(string[] args)
    {
        return app.RunAsync();
    }
}
