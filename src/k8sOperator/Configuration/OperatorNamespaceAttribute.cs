namespace k8s.Operator.Configuration;

[AttributeUsage(AttributeTargets.Assembly)]
public class OperatorNamespaceAttribute(string @namespace) : Attribute
{
    public string Namespace { get; } = @namespace;
}
