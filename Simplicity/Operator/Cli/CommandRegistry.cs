using System.Reflection;

namespace Simplicity.Operator.Cli;

public class CommandRegistry(IServiceProvider serviceProvider)
{
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Type> All() => _commands.Values.Distinct();

    public void RegisterCommand(Type commandType)
    {
        if (!typeof(IOperatorCommand).IsAssignableFrom(commandType))
        {
            throw new ArgumentException($"Command type must implement {nameof(IOperatorCommand)}", nameof(commandType));
        }

        var attribute = commandType.GetCustomAttribute<OperatorCommandAttribute>()
            ?? throw new ArgumentException($"Command type must have {nameof(OperatorCommandAttribute)}", nameof(commandType));

        _commands[attribute.Command] = commandType;

        foreach (var alias in attribute.Aliases)
        {
            _commands[alias] = commandType;
        }
    }

    public IOperatorCommand? GetCommand(string name)
    {
        return _commands.TryGetValue(name, out var commandType) ?
            ActivatorUtilities.CreateInstance(serviceProvider, commandType) as IOperatorCommand
            : null;
    }
}
