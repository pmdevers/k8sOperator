using k8s.Operator.Cli.Helpers;

namespace k8s.Operator.Cli;

public class RootCommand(CommandRegistry registry) : IOperatorCommand
{
    public async Task<int> ExecuteAsync(string[] args)
    {
        var commandName = args.Length > 0 ? args[0] : "help";
        var command = registry.Get(commandName);

        if (command == null)
        {
            Console.WriteLine($"Unknown command: {commandName}");
            Console.WriteLine();

            return 1;
        }

        // Parse command-specific options and arguments
        var commandArgs = args.Skip(1).ToArray();
        CommandArgumentParser.Parse(command, commandArgs);

        return await command.ExecuteAsync(commandArgs);
    }
}
