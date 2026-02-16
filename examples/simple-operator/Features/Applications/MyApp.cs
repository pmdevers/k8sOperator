using k8s;
using k8s.Models;

namespace simple_operator.Features.ManageApplication;
/// <summary>
/// Example custom resource
/// </summary>
[KubernetesEntity(Group = KubeGroup, ApiVersion = KubeApiVersion, Kind = KubeKind, PluralName = KubePluralName)]
public class V1MyApp : IKubernetesObject<V1ObjectMeta>, ISpec<V1MyAppSpec>, IStatus<V1MyAppStatus>
{
    public const string KubeApiVersion = "v1";
    public const string KubeKind = "TodoItem";
    public const string KubeGroup = "simplicity.io";
    public const string KubePluralName = "todos";

    public string ApiVersion { get; set; } = $"{KubeGroup}/{KubeApiVersion}";
    public string Kind { get; set; } = KubeKind;
    public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
    public V1MyAppSpec Spec { get; set; } = new V1MyAppSpec();
    public V1MyAppStatus Status { get; set; } = new V1MyAppStatus();

}

public class V1MyAppSpec
{
    public int Replicas { get; set; }
    public string? Image { get; set; }
}

public class V1MyAppStatus
{
    public string? Phase { get; set; }
    public int ReadyReplicas { get; set; }
}
