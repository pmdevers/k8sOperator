namespace k8s.Operator.Host;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class OperatorArgumentAttribute : Attribute
{
    public required string Command { get; set; }
    public string? Description { get; set; }

    public string[] Aliases { get; set; } = [];

    public int Order { get; set; }
}
