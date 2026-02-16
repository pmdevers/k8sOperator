namespace k8s.Operator.Configuration;

[AttributeUsage(AttributeTargets.Assembly)]
public class OperatorNameAttribute(string operatorName) : Attribute
{
    public string OperatorName { get; } = operatorName;
}
