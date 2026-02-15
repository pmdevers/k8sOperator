namespace Simplicity.Operator.Generation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AdditionalPrinterColumnAttribute(string name, string type, string desciption, string path, int priority = 0) : Attribute
{
    public string Name { get; } = name;
    public string Type { get; } = type;
    public string Description { get; } = desciption;
    public string Path { get; } = path;
    public int Priority { get; } = priority;
}
