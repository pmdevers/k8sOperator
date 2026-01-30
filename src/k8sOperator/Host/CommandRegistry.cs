using System.Reflection;

namespace k8s.Operator.Host;

public class CommandRegistry
{
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Type> _allCommandTypes = [];

    public void RegisterCommand<TCommand>() where TCommand : IOperatorCommand
    {
        RegisterCommand(typeof(TCommand));
    }

    public void RegisterCommand(Type commandType)
    {
        if (!typeof(IOperatorCommand).IsAssignableFrom(commandType))
        {
            throw new ArgumentException($"Command type must implement {nameof(IOperatorCommand)}", nameof(commandType));
        }

        var attribute = commandType.GetCustomAttribute<OperatorArgumentAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException($"Command type must have {nameof(OperatorArgumentAttribute)}", nameof(commandType));
        }

        _allCommandTypes.Add(commandType);
        _commands[attribute.Command] = commandType;

        foreach (var alias in attribute.Aliases)
        {
            _commands[alias] = commandType;
        }
    }

    public void DiscoverCommands(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(t => typeof(IOperatorCommand).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && !t.IsInterface
                     && t.GetCustomAttribute<OperatorArgumentAttribute>() != null);

        foreach (var type in commandTypes)
        {
            RegisterCommand(type);
        }
    }

    public Type? GetCommandType(string command)
    {
        return _commands.TryGetValue(command, out var type) ? type : null;
    }

    public IEnumerable<Type> GetAllCommandTypes() => _allCommandTypes;
}
