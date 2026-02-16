namespace k8s.Operator.Generation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PatternAttribute(string pattern) : Attribute
{
    public string Pattern { get; } = pattern;
}

