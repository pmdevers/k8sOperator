using k8s.Operator.Configuration;
using System.Data;
using System.Reflection;
using System.Text;
using static k8s.Operator.Cli.Helpers.ConsoleHelpers;

namespace k8s.Operator.Cli.Commands;

[OperatorCommand("help", "Display help information", -1, "-h", "--help")]
public class HelpCommand(OperatorConfiguration config, CommandRegistry registry) : IOperatorCommand
{
    [Argument(Name = "command", Description = "Command to get help for", Position = 0, Required = false)]
    public string? CommandName { get; set; }

    public Task<int> ExecuteAsync(string[] args)
    {
        if (!string.IsNullOrEmpty(CommandName))
        {
            ShowCommandHelp(CommandName);
        }
        else
        {
            ShowGeneralHelp();
        }

        return Task.FromResult(0);
    }

    private void ShowGeneralHelp()
    {
        var operatorName = config.Name;
        var strBuilder = new StringBuilder();

        strBuilder.AppendLine($"Welcome to the help for {GREEN}{operatorName}{NORMAL}.");
        strBuilder.AppendLine();
        strBuilder.AppendLine($"{BOLD}USAGE:{NORMAL}");
        strBuilder.AppendLine($"  {GREY}{operatorName.ToLowerInvariant()} {YELLOW}<command> {WHITE} {BOLD}[options] {NORMAL}");
        strBuilder.AppendLine();
        strBuilder.AppendLine($"{BOLD}COMMANDS:{NORMAL}");

        var commands = registry.All()
            .Select(t => t.GetCustomAttribute<OperatorCommandAttribute>())
            .Where(a => a != null)
            .OrderBy(a => a!.Order)
            .ThenBy(a => a!.Command);

        foreach (var cmd in commands)
        {
            var commandName = cmd!.Command;
            commandName = commandName.PadRight(25);
            strBuilder.AppendLine($"  {YELLOW}{commandName}{BOLD}{NORMAL}{cmd.Description}{NORMAL}");
        }

        strBuilder.AppendLine();
        strBuilder.AppendLine($"Use '{operatorName.ToLowerInvariant()} help {YELLOW}<command>{NORMAL}' for more information about a command.");
        strBuilder.AppendLine();

        Console.WriteLine(strBuilder.ToString());
    }

    private void ShowCommandHelp(string commandName)
    {
        var command = registry.Get(commandName);
        if (command == null)
        {
            var strBuilder = new StringBuilder();

            strBuilder.AppendLine($"Unknown command: {commandName}");
            strBuilder.AppendLine();
            strBuilder.AppendLine($"Use '{config.Name.ToLowerInvariant()} help' to see available commands.");

            Console.WriteLine(strBuilder.ToString());
            return;
        }

        var helpGen = HelpGenerator.Create(command, config);
        Console.WriteLine(helpGen.Generate());
    }
}

public class HelpGenerator
{
    private readonly StringBuilder _writer = new();

    private readonly OperatorConfiguration _config;
    private readonly OperatorCommandAttribute _commandAttr;
    private readonly List<PropertyOption> _properties;

    private record struct PropertyOption(PropertyInfo Property, OptionAttribute? Option, ArgumentAttribute? Argument);

    public static HelpGenerator Create(IOperatorCommand? command, OperatorConfiguration config)
    {
        var commandType = command?.GetType();
        var attribute = commandType?.GetCustomAttribute<OperatorCommandAttribute>();
        var properties = commandType?.GetProperties()?.Select(p => new PropertyOption(
            p, p.GetCustomAttribute<OptionAttribute>(), p.GetCustomAttribute<ArgumentAttribute>()
        )).ToList() ?? [];

        if (attribute == null)
        {
            throw new InvalidOperationException("Command does not have an OperatorCommandAttribute");
        }

        return new HelpGenerator(attribute, properties, config);
    }

    private HelpGenerator(OperatorCommandAttribute attribute, List<PropertyOption> properties, OperatorConfiguration config)
    {
        _config = config;
        _commandAttr = attribute;
        _properties = properties;
    }

    public string Generate()
    {
        PrintUsage();
        PrintOptions();
        PrintArguments();
        PrintAliasses();

        return _writer.ToString();
    }

    private void PrintAliasses()
    {
        // Show aliases if any
        if (_commandAttr.Aliases.Length > 0)
        {
            _writer.AppendLine($"{BOLD}ALIASES:{NORMAL}");
            _writer.AppendLine($"  {YELLOW}{string.Join(", ", _commandAttr.Aliases)}{NORMAL}");
            _writer.AppendLine();
        }
    }

    public void PrintUsage()
    {
        // Build usage line
        _writer.AppendLine($"{BOLD}USAGE:{NORMAL} ");
        _writer.AppendLine($"  {GREY}{_config.Name.ToLowerInvariant()} {YELLOW}{_commandAttr.Command}{NORMAL}");
        _writer.AppendLine();
        _writer.AppendLine($"{BOLD}{_commandAttr.Description}{NORMAL}");
        _writer.AppendLine();
    }

    private void PrintOptions()
    {
        var options = _properties.Where(x => x.Option is not null);

        if (options.Any())
        {
            _writer.AppendLine($"{BOLD}OPTIONS:{NORMAL}");
            foreach (var opt in options)
            {
                var optionName = opt.Option!.Name;
                var aliases = opt.Option.Aliases ?? [];

                // Build plain text version for padding calculation
                var plainText = optionName;
                if (aliases.Length > 0)
                {
                    plainText += $", {string.Join(", ", aliases)}";
                }

                var valueName = string.Empty;
                if (opt.Property.PropertyType != typeof(bool) && !string.IsNullOrEmpty(opt.Option.ValueName))
                {
                    valueName = $" <{opt.Option.ValueName}>";
                    plainText += valueName;
                }

                // Apply padding to plain text, then build colored version
                var paddedText = plainText.PadRight(40);
                var padding = paddedText.Length - plainText.Length;

                // Build colored display with same padding
                var coloredDisplay = optionName;
                if (aliases.Length > 0)
                {
                    coloredDisplay += $", {string.Join(", ", aliases)}";
                }
                if (!string.IsNullOrEmpty(valueName))
                {
                    coloredDisplay += $" {WHITE}<{opt.Option.ValueName}>{NORMAL}";
                }
                coloredDisplay += new string(' ', padding);

                var description = opt.Option.Description ?? string.Empty;

                if (opt.Option.DefaultValue != null)
                {
                    description += $" {GREY}(default: {opt.Option.DefaultValue}){NORMAL}";
                }
                else if (opt.Option.Required)
                {
                    description += $" {GREY}(required){NORMAL}";
                }

                _writer.AppendLine($"  {YELLOW}{coloredDisplay}{NORMAL}{description}");
            }
            _writer.AppendLine();
        }
    }

    public void PrintArguments()
    {
        var arguments = _properties.Where(x => x.Argument is not null);

        // Show arguments if any
        if (arguments.Any())
        {
            _writer.AppendLine($"{BOLD}ARGUMENTS:{NORMAL}");
            foreach (var arg in arguments)
            {
                var argName = (arg.Argument!.Name ?? arg.Property.Name.ToLowerInvariant()).PadRight(25);
                var description = arg.Argument.Description ?? string.Empty;

                if (arg.Argument.DefaultValue != null)
                {
                    description += $" {GREY}(default: {arg.Argument.DefaultValue}){NORMAL}";
                }
                else if (!arg.Argument.Required)
                {
                    description += $" {GREY}(optional){NORMAL}";
                }

                _writer.AppendLine($"  {WHITE}{argName}{NORMAL}{description}");
            }
            _writer.AppendLine();
        }
    }
}
