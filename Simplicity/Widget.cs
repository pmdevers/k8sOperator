using k8s;
using k8s.Models;
using System.Text.Json.Serialization;

namespace Simplicity;

[KubernetesEntity(
    ApiVersion = V1Widget.KubeApiVersion,
    Kind = V1Widget.KubeKind,
    Group = V1Widget.KubeGroup,
    PluralName = V1Widget.KubePluralName)]
public class V1Widget : IKubernetesObject<V1ObjectMeta>,
    ISpec<V1WidgetSpec>, IStatus<V1WidgetStatus>
{
    public const string KubeApiVersion = "v1";
    public const string KubeKind = "Widget";
    public const string KubeGroup = "simplicity.io";
    public const string KubePluralName = "widgets";

    public string ApiVersion { get; set; } = $"{KubeGroup}/{KubeApiVersion}";
    public string Kind { get; set; } = KubeKind;

    /// <summary>
    /// Standard object&apos;s metadata. More info:
    /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; }

    /// <summary>
    /// Specification of the desired behavior of the pod. More info:
    /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#spec-and-status
    /// </summary>
    [JsonPropertyName("spec")]
    public V1WidgetSpec Spec { get; set; }

    /// <summary>
    /// Most recently observed status of the pod. This data may not be up to date.
    /// Populated by the system. Read-only. More info:
    /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#spec-and-status
    /// </summary>
    [JsonPropertyName("status")]
    public V1WidgetStatus Status { get; set; }
}

public class V1WidgetSpec
{
    public string Name { get; set; }
    public int Size { get; set; }
}

public class V1WidgetStatus
{
    public string State { get; set; }
    public string Message { get; set; }
}
