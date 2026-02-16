using k8s;
using k8s.Models;
using k8s.Operator.Generation;

namespace simple_operator.Features.Todos;

[KubernetesEntity(
    Group = V1TodoItem.KubeGroup,
    ApiVersion = V1TodoItem.KubeApiVersion,
    Kind = V1TodoItem.KubeKind,
    PluralName = V1TodoItem.KubePluralName)]
[AdditionalPrinterColumn("Title", "string", "Todo Title", ".spec.title")]
[AdditionalPrinterColumn("Description", "string", "Todo Description", ".spec.description")]
[AdditionalPrinterColumn("State", "string", "Todo State", ".status.state")]
public class V1TodoItem :
    IKubernetesObject<V1ObjectMeta>,
    IStatus<V1TodoStatus>
{
    public const string KubeApiVersion = "v1";
    public const string KubeKind = "TodoItem";
    public const string KubeGroup = "simplicity.io";
    public const string KubePluralName = "todos";

    public string ApiVersion { get; set; } = $"{KubeGroup}/{KubeApiVersion}";
    public string Kind { get; set; } = KubeKind;
    public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
    public V1TodoSpec Spec { get; set; } = new V1TodoSpec();
    public V1TodoStatus Status { get; set; } = new V1TodoStatus();
}

public class V1TodoSpec
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium"; // low, medium, high
    public DateTime? DueDate { get; set; }
}

public class V1TodoStatus
{
    public string State { get; set; } = "pending"; // pending, in-progress, completed
    public DateTime? CompletedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ReconciliationCount { get; set; }
}

