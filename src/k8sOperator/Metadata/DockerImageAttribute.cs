namespace k8s.Operator.Metadata;

[AttributeUsage(AttributeTargets.Assembly)]
public class DockerImageAttribute(string registry, string repository, string tag) : Attribute
{
    public static DockerImageAttribute Default => new("ghcr.io", "operator/operator", "latest");

    public string Registry { get; set; } = registry;
    public string Repository { get; set; } = repository;
    public string Tag { get; set; } = tag;
    public string GetImage() => $"{Registry}/{Repository}:{Tag}";

    public override string ToString()
        => DebuggerHelpers.GetDebugText("DockerImage", GetImage());
}
