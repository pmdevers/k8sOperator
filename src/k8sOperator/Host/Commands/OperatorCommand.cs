using k8s.Operator;
using Microsoft.Extensions.Hosting;

namespace k8s.Operator.Host.Commands;

[OperatorArgument(Command = "operator", Description = "Starts the operator.", Order = -2)]
public class OperatorCommand(IHost app) : IOperatorCommand
{
    public Task RunAsync(string[] args)
    {
        return app.RunAsync();
    }
}
