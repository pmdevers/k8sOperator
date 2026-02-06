namespace k8s.Operator.Generation.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PatternAttribute(string pattern) : Attribute
{
    public string Pattern { get; } = pattern;
}


[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DefaultAttribute(string defaultValue) : Attribute
{
    public string Default { get; } = defaultValue;
}
