using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace k8s.Operator.Host;

public class CommandRegistry
{
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Type> _allCommandTypes = [];
    private bool _builtInCommandsRegistered = false;

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

        var attribute = commandType.GetCustomAttribute<OperatorArgumentAttribute>()
            ?? throw new ArgumentException($"Command type must have {nameof(OperatorArgumentAttribute)}", nameof(commandType));

        // Prevent duplicate registration
        if (_allCommandTypes.Contains(commandType))
        {
            return;
        }

        _allCommandTypes.Add(commandType);
        _commands[attribute.Command] = commandType;

        foreach (var alias in attribute.Aliases)
        {
            _commands[alias] = commandType;
        }
    }

    /// <summary>
    /// Discovers commands from the specified assembly.
    /// In single-file/trimmed builds, reflection may fail - built-in commands are registered explicitly.
    /// </summary>
    public void DiscoverCommands(Assembly assembly)
    {
        // Always register built-in commands first for single-file compatibility
        EnsureBuiltInCommandsRegistered();

        try
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
        catch (Exception)
        {
            // Reflection may fail in single-file/trimmed builds
            // Built-in commands are already registered above
        }
    }

    private void EnsureBuiltInCommandsRegistered()
    {
        if (_builtInCommandsRegistered)
        {
            return;
        }

        _builtInCommandsRegistered = true;

        // Register built-in commands explicitly for single-file/trimmed support
        RegisterBuiltInCommandsInternal();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Built-in command types are preserved")]
    private void RegisterBuiltInCommandsInternal()
    {
        // Hard-coded list of built-in commands that should always be available
        var builtInCommandTypes = new[]
        {
            typeof(Commands.OperatorCommand),
            typeof(Commands.InstallCommand),
            typeof(Commands.VersionCommand),
            typeof(Commands.HelpCommand),
        };

        foreach (var type in builtInCommandTypes)
        {
            try
            {
                RegisterCommand(type);
            }
            catch
            {
                // Skip commands that don't exist (optional commands)
            }
        }
    }

    public Type? GetCommandType(string command)
    {
        return _commands.TryGetValue(command, out var type) ? type : null;
    }

    public IEnumerable<Type> GetAllCommandTypes() => _allCommandTypes;
}
