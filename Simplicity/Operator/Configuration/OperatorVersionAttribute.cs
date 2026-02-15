namespace Simplicity.Operator.Configuration;

[AttributeUsage(AttributeTargets.Assembly)]
public class OperatorVersionAttribute(string version) : Attribute
{
    public string Version { get; } = version;
}
