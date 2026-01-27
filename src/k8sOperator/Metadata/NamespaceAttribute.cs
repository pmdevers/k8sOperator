namespace k8s.Operator.Metadata;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class NamespaceAttribute(string ns) : Attribute
{
    public static NamespaceAttribute Default => new("default");

    public string Namespace { get; set; } = ns;

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(Namespace), Namespace);
}

