using Microsoft.Extensions.Hosting;

namespace k8s.Operator.Cli.Commands;

[OperatorCommand("operator", "Starts the operator.", -2)]
public class OperatorCommand(IHost app) : IOperatorCommand
{
    public async Task<int> ExecuteAsync(string[] args)
    {
        await app.RunAsync();
        return 0;
    }
}
