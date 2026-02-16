using k8s.Models;
using k8s.Operator.Generation;


namespace k8s.Frontman.Features.Releases;

[KubernetesEntity(Group = KubeGroup, ApiVersion = KubeApiVersion, Kind = KubeKind, PluralName = KubePluralName)]
[AdditionalPrinterColumn("Url", "string", "", ".spec.url")]
[AdditionalPrinterColumn("Version", "string", "", ".spec.version")]
[AdditionalPrinterColumn("Previous", "string", "", ".status.previousVersion")]
[AdditionalPrinterColumn("Message", "string", "", ".status.message")]
public class V1Release : IKubernetesObject<V1ObjectMeta>, ISpec<V1ReleaseSpec>, IStatus<V1ReleaseStatus>
{
    public const string KubeApiVersion = "v1";
    public const string KubeKind = "Release";
    public const string KubeGroup = "frontman.io";
    public const string KubePluralName = "releases";

    public string ApiVersion { get; set; } = KubeApiVersion;
    public string Kind { get; set; } = KubeKind;
    public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
    public V1ReleaseSpec Spec { get; set; } = new V1ReleaseSpec();
    public V1ReleaseStatus Status { get; set; } = new V1ReleaseStatus();
}

public class V1ReleaseSpec
{
    public string Provider { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class V1ReleaseStatus
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string PreviousVersion { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
