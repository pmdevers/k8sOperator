using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace k8s.Operator.Cli;

public class CommandRegistry(IServiceProvider serviceProvider)
{
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Type> All() => _commands.Values.Distinct();

    public CommandRegistry Add<T>()
        where T : IOperatorCommand
    {
        var commandType = typeof(T);
        var attribute = commandType.GetCustomAttribute<OperatorCommandAttribute>()
            ?? throw new ArgumentException($"Command type must have {nameof(OperatorCommandAttribute)}", nameof(commandType));

        _commands[attribute.Command] = commandType;

        foreach (var alias in attribute.Aliases)
        {
            _commands[alias] = commandType;
        }
        return this;
    }

    public IOperatorCommand? GetCommand(string name)
    {
        return _commands.TryGetValue(name, out var commandType) ?
            ActivatorUtilities.CreateInstance(serviceProvider, commandType) as IOperatorCommand
            : null;
    }
}
