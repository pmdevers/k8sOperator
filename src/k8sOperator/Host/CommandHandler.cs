using k8s.Operator.Host.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace k8s.Operator.Host;

public class CommandHandler
{
    private readonly IHost _host;
    private readonly CommandRegistry _registry;

    public CommandHandler(IHost host, CommandRegistry registry)
    {
        _host = host;
        _registry = registry;
    }

    public async Task<int> HandleAsync(string[] args)
    {
        var command = args.Length > 0 ? args[0] : "help";
        var commandType = _registry.GetCommandType(command);

        if (commandType == null)
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine();

            // Show help
            var helpType = _registry.GetCommandType("help");
            if (helpType != null)
            {
                var helpCommand = CreateCommand(helpType);
                await helpCommand.RunAsync(args);
            }

            return 1;
        }

        try
        {
            var operatorCommand = CreateCommand(commandType);
            await operatorCommand.RunAsync(args);
            return 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error executing command '{command}': {ex.Message}");
            return 1;
        }
    }

    private IOperatorCommand CreateCommand(Type commandType)
    {
        // Special handling for HelpCommand to inject command types
        if (commandType == typeof(HelpCommand))
        {
            return new HelpCommand(_host, _registry.GetAllCommandTypes());
        }

        // Use DI to create the command
        try
        {
            return (IOperatorCommand)ActivatorUtilities.CreateInstance(_host.Services, commandType);
        }
        catch
        {
            // If that fails, try with IHost as parameter (for OperatorCommand)
            return (IOperatorCommand)ActivatorUtilities.CreateInstance(_host.Services, commandType, _host);
        }
    }
}
