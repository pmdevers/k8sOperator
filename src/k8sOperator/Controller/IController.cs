namespace k8s.Operator.Controller;

/// <summary>
/// Marker interface for all informer-based controllers.
/// </summary>
public interface IController
{
    IReadOnlyList<object> Metadata { get; }
    Type ResourceType { get; }
    Task RunAsync(CancellationToken cancellationToken);
}
