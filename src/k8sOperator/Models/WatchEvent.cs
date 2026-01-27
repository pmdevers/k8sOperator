namespace k8s.Operator.Models;

public record WatchEvent<TResource>
{
    public WatchEventType Type { get; set; }
    public TResource Object { get; set; }
}
