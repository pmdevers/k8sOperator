namespace k8s.Operator.Configuration;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class ContainerAttribute(string? registry, string? organization, string image, string? tag, string? digest) : Attribute
{
    public string? Registry { get; set; } = registry;
    public string? Organization { get; set; } = organization;
    public string Image { get; set; } = image;
    public string? Tag { get; set; } = tag;
    public string? Digest { get; set; } = digest;
}
