namespace Simplicity.Operator.Cli;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OperatorCommandAttribute(string command, string description, int order, params string[] aliases) : Attribute
{
    public string Command { get; } = command;
    public string? Description { get; } = description;
    public string[] Aliases { get; } = aliases;
    public int Order { get; } = order;
}
