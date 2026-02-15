namespace Simplicity.Operator.Generation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DefaultAttribute(string defaultValue) : Attribute
{
    public string Default { get; } = defaultValue;
}

