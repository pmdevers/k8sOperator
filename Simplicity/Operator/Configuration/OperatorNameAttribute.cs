namespace Simplicity.Operator.Configuration;

[AttributeUsage(AttributeTargets.Assembly)]
public class OperatorNameAttribute(string operatorName) : Attribute
{
    public string OperatorName { get; } = operatorName;
}
