namespace k8s.Operator.Cli.Commands;

/// <summary>
/// Defines a command-line option that can be specified by users.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OptionAttribute : Attribute
{
    /// <summary>
    /// The name of the option (e.g., "--output", "-o")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Alternative names/aliases for the option
    /// </summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>
    /// Description shown in help text
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this option is required
    /// </summary>
    public bool Required { get; init; }

    /// <summary>
    /// Default value if not specified
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Value name shown in help (e.g., --output &lt;file&gt;)
    /// </summary>
    public string? ValueName { get; init; }
}

/// <summary>
/// Defines a positional argument for a command.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ArgumentAttribute : Attribute
{
    /// <summary>
    /// Name of the argument (used in help text)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description shown in help text
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this argument is required
    /// </summary>
    public bool Required { get; init; } = true;

    /// <summary>
    /// Default value if not specified
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Position of the argument (0-based)
    /// </summary>
    public int Position { get; init; }
}
