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

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ResyncIntervalAttribute() : PatternAttribute("^([0-9]+(\\.[0-9]+)?(ms|s|m|h))+$")
{
}
