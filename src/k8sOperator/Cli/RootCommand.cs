namespace k8s.Operator.Cli;

public class RootCommand(CommandRegistry registry) : IOperatorCommand
{
    public async Task<int> ExecuteAsync(string[] args)
    {
        var commandName = args.Length > 0 ? args[0] : "help";
        var command = registry.GetCommand(commandName);

        if (command == null)
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine();

            return 1;
        }
        return await command.ExecuteAsync([.. args.Skip(1)]);
    }
}
