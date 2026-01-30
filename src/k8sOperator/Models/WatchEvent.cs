namespace k8s.Operator.Models;

public record WatchEvent<TResource>
    where TResource : CustomResource
{
    public WatchEventType Type { get; set; }
    public TResource Object { get; set; }
}
