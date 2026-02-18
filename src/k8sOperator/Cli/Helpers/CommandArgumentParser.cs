using k8s.Operator;
using k8s.Operator.Cli.Commands;
using System.Reflection;

namespace k8s.Operator.Cli.Helpers;

/// <summary>
/// Parses command-line arguments and binds them to command properties.
/// </summary>
public class CommandArgumentParser(IOperatorCommand command, string[] args)
{
    private readonly Dictionary<string, PropertyOption> options = [];
    private List<PropertyArgument> arguments = [];
    private readonly List<string> positionalArgs = [];
    public static void Parse(IOperatorCommand command, string[] args)
        => new CommandArgumentParser(command, args).Parse();

    public void Parse()
    {
        CollectMetadata();
        SortArguments();
        ParseArguments();
        BindPositionalArguments();
        SetDefaultValues();
        ValidateRequiredProperties();
    }

    private void ValidateRequiredProperties()
    {
        foreach (var propOption in options.Values.Distinct())
        {
            if (propOption.Attribute.Required)
            {
                var value = propOption.Property.GetValue(command);
                if (value == null || value.Equals(GetDefaultValue(propOption.Property.PropertyType)))
                {
                    throw new InvalidOperationException($"Required option '{propOption.Attribute.Name}' is missing.");
                }
            }
        }

        foreach (var propArg in arguments)
        {
            if (propArg.Attribute.Required)
            {
                var value = propArg.Property.GetValue(command);
                if (value == null || value.Equals(GetDefaultValue(propArg.Property.PropertyType)))
                {
                    throw new InvalidOperationException($"Required argument '{propArg.Attribute.Name}' is missing.");
                }
            }
        }
    }

    private void SetDefaultValues()
    {
        // Set default values for unset options
        foreach (var propOption in options.Values.Distinct())
        {
            if (propOption.Attribute.DefaultValue != null)
            {
                var currentValue = propOption.Property.GetValue(command);
                if (currentValue == null || currentValue.Equals(GetDefaultValue(propOption.Property.PropertyType)))
                {
                    propOption.Property.SetValue(command, propOption.Attribute.DefaultValue);
                }
            }
        }

        // Set default values for unset arguments
        foreach (var propArg in arguments)
        {
            if (propArg.Attribute.DefaultValue != null)
            {
                var currentValue = propArg.Property.GetValue(command);
                if (currentValue == null || currentValue.Equals(GetDefaultValue(propArg.Property.PropertyType)))
                {
                    propArg.Property.SetValue(command, propArg.Attribute.DefaultValue);
                }
            }
        }
    }

    private void BindPositionalArguments()
    {
        for (int i = 0; i < Math.Min(positionalArgs.Count, arguments.Count); i++)
        {
            SetPropertyValue(arguments[i].Property, command, positionalArgs[i]);
        }
    }

    private void SortArguments()
    {
        arguments = [.. arguments.OrderBy(a => a.Attribute.Position)];
    }

    private void ParseArguments()
    {
        int i = 0;
        while (i < args.Length)
        {
            var arg = args[i];

            if (arg.StartsWith("--") || arg.StartsWith('-'))
            {
                // Option
                var optionName = arg;
                object value = true; // Flag by default

                // Check if option has value (--option=value or --option value)
                if (arg.Contains('='))
                {
                    var parts = arg.Split('=', 2);
                    optionName = parts[0];
                    value = parts[1];
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    // Next arg is the value
                    value = args[i + 1];
                    i++; // Move to next argument for value
                }

                if (options.TryGetValue(optionName, out var propOption))
                {
                    SetPropertyValue(propOption.Property, command, value);
                }
            }
            else
            {
                // Positional argument
                positionalArgs.Add(arg);
            }
            i++;
        }
    }

    private void CollectMetadata()
    {
        var properties = command.GetType()
        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var optionAttr = prop.GetCustomAttribute<OptionAttribute>();
            if (optionAttr != null)
            {
                var propOption = new PropertyOption
                {
                    Property = prop,
                    Attribute = optionAttr
                };

                options[optionAttr.Name] = propOption;
                foreach (var alias in optionAttr.Aliases)
                {
                    options[alias] = propOption;
                }
            }

            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            if (argAttr != null)
            {
                arguments.Add(new PropertyArgument
                {
                    Property = prop,
                    Attribute = argAttr
                });
            }
        }
    }

    private static void SetPropertyValue(PropertyInfo property, object target, object value)
    {
        var targetType = property.PropertyType;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        object? convertedValue = value;

        if (value is string stringValue)
        {
            if (underlyingType == typeof(bool))
            {
                convertedValue = stringValue == "true" || stringValue == "1" || stringValue == "";
            }
            else if (underlyingType == typeof(int))
            {
                convertedValue = int.Parse(stringValue);
            }
            else if (underlyingType == typeof(long))
            {
                convertedValue = long.Parse(stringValue);
            }
            else if (underlyingType == typeof(double))
            {
                convertedValue = double.Parse(stringValue);
            }
            else if (underlyingType.IsEnum)
            {
                convertedValue = Enum.Parse(underlyingType, stringValue, ignoreCase: true);
            }
        }
        else if (value is bool && underlyingType != typeof(bool))
        {
            convertedValue = true;
        }

        property.SetValue(target, convertedValue);
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private sealed class PropertyOption
    {
        public required PropertyInfo Property { get; init; }
        public required OptionAttribute Attribute { get; init; }
    }

    private sealed class PropertyArgument
    {
        public required PropertyInfo Property { get; init; }
        public required ArgumentAttribute Attribute { get; init; }
    }
}
