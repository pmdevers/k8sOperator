namespace k8s.Operator.Controller;

/// <summary>
/// Marker interface for all informer-based controllers.
/// </summary>
public interface IController
{
    Task RunAsync(CancellationToken cancellationToken);
}
