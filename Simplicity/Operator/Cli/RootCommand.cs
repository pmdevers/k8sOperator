namespace Simplicity.Operator.Cli;

public class RootCommand(IHost host, CommandRegistry registry) : IOperatorCommand
{
    public Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            var help = registry.GetCommand("help");
            return help?.ExecuteAsync(args) ?? Task.CompletedTask;
        }

        var commandName = args[0];
        var command = registry.GetCommand(commandName);

        if (command == null)
        {
            // Handle unknown command scenario
            return Task.CompletedTask;
        }
        return command.ExecuteAsync(args.Skip(1).ToArray());
    }
}
