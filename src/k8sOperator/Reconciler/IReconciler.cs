namespace k8s.Operator.Reconciler;

public interface IReconciler
{
    Task StartAsync(CancellationToken token);
    Task StopAsync(CancellationToken token);
}
